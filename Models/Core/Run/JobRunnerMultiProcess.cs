﻿namespace Models.Core.Run
{
    using APSIM.Shared.JobRunning;
    using APSIM.Shared.Utilities;
    using Models.Storage;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Pipes;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>A class for managing asynchronous running of jobs transferred via a socket connection</summary>
    [Serializable]
    public class JobRunnerMultiProcess : JobRunner
    {
        private bool allStopped;
        private int numberOfProcessors;
        private ConcurrentQueue<MultiProcessJob> jobsToRun = new ConcurrentQueue<MultiProcessJob>();
        private Task jobQueueFillerTask;

        private List<Task> pipeServers = new List<Task>();

        /// <summary>A mapping of job IDs (Guid) to job  instances.</summary>
        private Dictionary<Guid, MultiProcessJob> runningJobs = new Dictionary<Guid, MultiProcessJob>();

        /// <summary>Constructor.</summary>
        /// <param name="numOfProcessors">The maximum number of cores to use.</param>
        public JobRunnerMultiProcess(int numOfProcessors = -1)
        {
            numberOfProcessors = numOfProcessors;
            // Determine number of threads to use
            if (numberOfProcessors == -1)
            {
                int number;
                string numOfProcessorsString = Environment.GetEnvironmentVariable("NUMBER_OF_PROCESSORS");
                if (numOfProcessorsString != null && Int32.TryParse(numOfProcessorsString, out number))
                    numberOfProcessors = System.Math.Max(number, 1);
                else
                    numberOfProcessors = System.Math.Max(Environment.ProcessorCount - 1, 1);
            }
        }

        /// <summary>
        /// Stop the APSIMRunners.
        /// </summary>
        public override void Stop()
        {
            DeleteRunners();
        }

        /// <summary>Main worker thread.</summary>
        protected override void WorkerThread()
        {
            // Start a task to fill our queue of jobs to run.
            jobQueueFillerTask = Task.Run(() => FillJobQueue());

            DeleteRunners();
            CreateRunners();

            AppDomain.CurrentDomain.AssemblyResolve += Manager.ResolveManagerAssembliesEventHandler;

            SpinWait.SpinUntil(() => allStopped);

            DeleteRunners();
            runningJobs.Clear();

            ElapsedTime = DateTime.Now - startTime;
            InvokeAllCompleted();

            completed = true;
        }

        /// <summary>Create one job runner process for each CPU</summary>
        private void CreateRunners()
        {
            int numRunners = Process.GetProcessesByName("APSIMRunner").Length;
            for (int i = numRunners; i < numberOfProcessors; i++)
                pipeServers.Add(Task.Run(() => PipeServerTaskThread()));
        }

         /// <summary>Delete any runners that may exist.</summary>
        private void DeleteRunners()
        {
            foreach (Process runner in Process.GetProcessesByName("APSIMRunner"))
                runner.Kill();
        }

        /// <summary>
        /// Main task thread for working with a single APSIMRunner.exe process using
        /// an anonymous pipe.
        /// </summary>
        private void PipeServerTaskThread()
        {
            // Create 2 anonymous pipes (read and write) for duplex communications
            // (each pipe is one-way)
            using (var pipeRead = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable))
            using (var pipeWrite = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable))
            {
                var pipeHandles = pipeRead.GetClientHandleAsString() + " " + pipeWrite.GetClientHandleAsString();

                // Run a APSIMRunner process passing the pipe read and write handles as arguments.
                var runnerProcess = new ProcessUtilities.ProcessWithRedirectedOutput();
                runnerProcess.Exited += OnExited;
                var runnerExecutable = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "APSIMRunner.exe");
                runnerProcess.Start(executable: runnerExecutable,
                                    arguments: pipeHandles,
                                    workingDirectory: Directory.GetCurrentDirectory(),
                                    redirectOutput: true,
                                    writeToConsole: false);

                // Release the local handles that were created with the above GetClientHandleAsString calls
                pipeRead.DisposeLocalCopyOfClientHandle();
                pipeWrite.DisposeLocalCopyOfClientHandle();

                try
                {
                    // Get the next job to run.
                    var job = GetJobToRun();

                    while (job != null)
                    {
                        var startTime = DateTime.Now;

                        // Send the job to APSIMRunner.exe - this will run the simulation.
                        PipeUtilities.SendObjectToPipe(pipeWrite, job.JobSentToClient);

                        pipeWrite.WaitForPipeDrain();

                        // Get the output data back from APSIMRunner.exe
                        var endJob = PipeUtilities.GetObjectFromPipe(pipeRead) as JobOutput;

                        // Send the output data to storage.
                        endJob?.WriteOutput(job.DataStore);

                        // Signal end of job.
                        InvokeJobCompleted(job.RunnableJob,
                                           job.JobManager,
                                           startTime,
                                           endJob?.ErrorMessage);

                        // Get the next job to run.
                        job = GetJobToRun();
                    }
                }
                catch (Exception err)
                {
                    AddException(err);
                }
            }
        }

        /// <summary>A runner process has exited. Check for errors</summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments</param>
        private void OnExited(object sender, EventArgs e)
        {
            ProcessUtilities.ProcessWithRedirectedOutput p = sender as ProcessUtilities.ProcessWithRedirectedOutput;
            if (p.ExitCode != 0)
            {
                var exception = new Exception(p.StdOut + Environment.NewLine + p.StdErr);
                AddException(exception);
            }

            if (Process.GetProcessesByName("APSIMRunner").Length == 0)
                allStopped = true;
        }

        /// <summary>Fill our job queue with jobs to run.</summary>
        private void FillJobQueue()
        {
            foreach (var jobManager in jobManagers)
            {
                foreach (var job in jobManager.GetJobs())
                {
                    jobsToRun.Enqueue(
                        new MultiProcessJob()
                        {
                            JobManager = jobManager,
                            RunnableJob = job
                        });
                }
            }
        }

        /// <summary>Get the next job to run. Needs to be thread safe.</summary>
        /// <returns>The job to run or NULL if no more jobs to be run.</returns>
        private MultiProcessJob GetJobToRun()
        {
            var jobKey = Guid.NewGuid();
            MultiProcessJob job = null;
            SpinWait.SpinUntil(() => jobsToRun.TryDequeue(out job) || jobQueueFillerTask.IsCompleted);
            if (job == null)
                return null;
            else if (job.RunnableJob is SimulationDescription)
            {
                job.DataStore = (job.RunnableJob as SimulationDescription).Storage;
                job.JobSentToClient = (job.RunnableJob as SimulationDescription).ToSimulation();
                (job.JobSentToClient as Simulation).Services = null;
            }
            else
                job.JobSentToClient = job.RunnableJob;

            return job;
        }

        /// <summary>
        /// Add an exception to our list of exceptions.
        /// </summary>
        /// <param name="err">The exception to add.</param>
        private void AddException(Exception err)
        {
            if (err != null)
            {
                if (ExceptionThrownByRunner == null)
                    ExceptionThrownByRunner = err;
            }
        }

        /// <summary>An class for encapsulating arguments to an EndJob command</summary>
        [Serializable]
        public class JobOutput
        {
            /// <summary>Error message</summary>
            public Exception ErrorMessage { get; set; }

            /// <summary>Report data that needs to be written to storage.</summary>
            public List<ReportData> ReportData { get; set; }

            /// <summary>Data tables that need to be written to storage.</summary>
            public List<DataTable> DataTables { get; set; }

            /// <summary>Data tables that need to be written to storage.</summary>
            /// <param name="storage">Write all output to storage.</param>
            public void WriteOutput(IDataStore storage)
            {
                DataTables.ForEach(table => storage.Writer.WriteTable(table));
                ReportData.ForEach(table => storage.Writer.WriteTable(table));
            }
        }
        
        private class MultiProcessJob
        {
            /// <summary>The owining job manager.</summary>
            public IJobManager JobManager { get; set; }

            /// <summary>The job to run.</summary>
            public IRunnable RunnableJob { get; set; }

            /// <summary>The job that was sent to the APSIM Runner client.</summary>
            public IRunnable JobSentToClient { get; set; }

            /// <summary>The data store relating to the job</summary>
            public DataStore DataStore { get; set; }
        }
    }
}