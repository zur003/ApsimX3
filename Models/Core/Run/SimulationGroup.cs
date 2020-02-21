﻿namespace Models.Core.Run
{
    using APSIM.Shared.JobRunning;
    using Models.Core.ApsimFile;
    using Models.Storage;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Encapsulates a collection of jobs that are to be run. A job can be a simulation run or 
    /// a class instance that implements IRunnable e.g. EXCEL input run.
    /// </summary>
    public class SimulationGroup : JobManager
    {
        /// <summary>The model to use to search for simulations to run.</summary>
        private IModel relativeTo;

        /// <summary>Top level model.</summary>
        private IModel rootModel;

        /// <summary>Run simulations?</summary>
        private bool runSimulations;

        /// <summary>Run post simulation tools?</summary>
        private bool runPostSimulationTools;

        /// <summary>Run tests?</summary>
        private bool runTests;

        /// <summary>Has this instance been initialised?</summary>
        private bool initialised = false;

        /// <summary>Specific simulation names to run.</summary>
        private IEnumerable<string> simulationNamesToRun;

        /// <summary>A pattern used to determine simulations to run.</summary>
        private Regex patternMatch;

        /// <summary>The related storage model.</summary>
        private IDataStore storage;

        /// <summary>Time when job collection first started.</summary>
        private DateTime startTime;

        /// <summary>Contstructor</summary>
        /// <param name="relativeTo">The model to use to search for simulations to run.</param>
        /// <param name="runSimulations">Run simulations?</param>
        /// <param name="runPostSimulationTools">Run post simulation tools?</param>
        /// <param name="runTests">Run tests?</param>
        /// <param name="simulationNamesToRun">Only run these simulations.</param>
        /// <param name="simulationNamePatternMatch">A regular expression used to match simulation names to run.</param>
        public SimulationGroup(IModel relativeTo,
                             bool runSimulations = true,
                             bool runPostSimulationTools = true,
                             bool runTests = true,
                             IEnumerable<string> simulationNamesToRun = null,
                             string simulationNamePatternMatch = null)
        {
            this.relativeTo = relativeTo;
            this.runSimulations = runSimulations;
            this.runPostSimulationTools = runPostSimulationTools;
            this.runTests = runTests;
            this.simulationNamesToRun = simulationNamesToRun;

            if (simulationNamePatternMatch != null)
                patternMatch = new Regex(simulationNamePatternMatch);

            Initialise();
        }

        /// <summary>Contstructor</summary>
        /// <param name="fileName">The name of the file to run.</param>
        /// <param name="runTests">Run tests?</param>
        /// <param name="simulationNamePatternMatch">A regular expression used to match simulation names to run.</param>
        public SimulationGroup(string fileName,
                             bool runTests = true,
                             string simulationNamePatternMatch = null)
        {
            this.FileName = fileName;
            this.runSimulations = true;
            this.runPostSimulationTools = true;
            this.runTests = runTests;

            if (simulationNamePatternMatch != null)
                patternMatch = new Regex(simulationNamePatternMatch);

            Task.Run(() => { Initialise(); });
        }

        /// <summary>Name of file where the jobs came from.</summary>
        public string FileName { get; set; }

        /// <summary>The number of simulations to run.</summary>
        public int TotalNumberOfSimulations { get; private set; }

        /// <summary>The number of simulations completed running.</summary>
        public int NumberOfSimulationsCompleted { get; private set; }

        /// <summary>A list of exceptions thrown before and after the simulation runs. Will be null when no exceptions found.</summary>
        public List<Exception> PrePostExceptionsThrown { get; private set; }

        /// <summary>Called once to do initialisation before any jobs are run. Should throw on error.</summary>
        protected override void PreRun()
        {
            if (!initialised)
                SpinWait.SpinUntil(() => initialised);
        }

        /// <summary>Called once when all jobs have completed running. Should throw on error.</summary>
        protected override void PostRun(JobCompleteArguments args)
        {
            lock (this)
            {
                if(!(args.Job is EmptyJob))
                    NumberOfSimulationsCompleted++;
            }
        }

        /// <summary>Called once when all jobs have completed running. Should throw on error.</summary>
        protected override void PostAllRuns()
        {
            storage?.Writer.Stop();
            storage?.Reader.Refresh();

            if (runPostSimulationTools)
                RunPostSimulationTools();

            if (runTests)
                RunTests();

            storage?.Writer.Stop();
            storage?.Reader.Refresh();
        }

        /// <summary>Initialise the instance.</summary>
        private void Initialise()
        {
            startTime = DateTime.Now;

            List<Exception> exceptions = null;
            try
            {
                if (relativeTo == null)
                {

                    if (!File.Exists(FileName))
                        throw new Exception("Cannot find file: " + FileName);
                    relativeTo = FileFormat.ReadFromFile<Simulations>(FileName, out exceptions);
                    if (exceptions.Count > 0)
                        throw exceptions[0];
                }

                if (relativeTo != null)
                {
                    // If this simulation was not created from deserialisation then we need
                    // to parent all child models correctly and call OnCreated for each model.
                    bool hasBeenDeserialised = relativeTo.Children.Count > 0 &&
                                               relativeTo.Children[0].Parent == relativeTo;
                    if (!hasBeenDeserialised)
                    {
                        // Parent all models.
                        Apsim.ParentAllChildren(relativeTo);

                        // Call OnCreated in all models.
                        Apsim.ChildrenRecursively(relativeTo).ForEach(m => m.OnCreated());
                    }

                    // Find the root model.
                    rootModel = relativeTo;
                    while (rootModel.Parent != null)
                        rootModel = rootModel.Parent;

                    if (rootModel is Simulations)
                        FileName = (rootModel as Simulations).FileName;
                    else if (rootModel is Simulation)
                        FileName = (rootModel as Simulation).FileName;


                    // Publish BeginRun event.
                    var e = new Events(rootModel);
                    e.Publish("BeginRun", new object[] { this, new EventArgs() });

                    // Find simulations to run.
                    if (runSimulations)
                        FindListOfSimulationsToRun(relativeTo, simulationNamesToRun);

                    
                    if (numJobsToRun == 0)
                       Add(new EmptyJob());

                    // Find a storage model.
                    storage = Apsim.Child(rootModel, typeof(IDataStore)) as IDataStore;
                }
            }
            catch (Exception readException)
            {
                Exception exceptionToStore = readException;
                if (FileName != null)
                    exceptionToStore = new Exception("Error in file:" + FileName, readException);
                AddException(exceptionToStore);
            }
            initialised = true;
        }

        /// <summary>Determine the list of jobs to run</summary>
        /// <param name="relativeTo">The model to use to search for simulations to run.</param>
        /// <param name="simulationNamesToRun">Only run these simulations.</param>
        private void FindListOfSimulationsToRun(IModel relativeTo, IEnumerable<string> simulationNamesToRun)
        {
            if (relativeTo is Simulation)
            {
                if (SimulationNameIsMatched(relativeTo.Name))
                {
                    Add(new SimulationDescription(relativeTo as Simulation));
                    TotalNumberOfSimulations++;
                }
            }
            else if (relativeTo is ISimulationDescriptionGenerator)
            {
                foreach (var description in (relativeTo as ISimulationDescriptionGenerator).GenerateSimulationDescriptions())
                    if (SimulationNameIsMatched(description.Name))
                    {
                        Add(description);
                        TotalNumberOfSimulations++;
                    }
            }
            else if (relativeTo is Folder || relativeTo is Simulations)
            {
                // Get a list of all models we're going to run.
                foreach (var child in relativeTo.Children)
                    FindListOfSimulationsToRun(child, simulationNamesToRun);
            }
        }

        /// <summary>Return true if simulation name is a match.</summary>
        /// <param name="simulationName">Simulation name to look for.</param>
        /// <returns>True if matched.</returns>
        private bool SimulationNameIsMatched(string simulationName)
        {
            if (patternMatch != null)
                return patternMatch.Match(simulationName).Success;
            else
                return simulationNamesToRun == null || simulationNamesToRun.Contains(simulationName);
        }

        /// <summary>Run all post simulation tools.</summary>
        private void RunPostSimulationTools()
        {
            // Call all post simulation tools.
            object[] args = new object[] { this, new EventArgs() };
            foreach (IPostSimulationTool tool in Apsim.ChildrenRecursively(rootModel, typeof(IPostSimulationTool)))
            {
                storage?.Writer.WaitForIdle();
                storage?.Reader.Refresh();

                DateTime startTime = DateTime.Now;
                Exception exception = null;
                try
                {
                    if (rootModel is Simulations)
                        (rootModel as Simulations).Links.Resolve(tool as IModel);
                    if ((tool as IModel).Enabled)
                        tool.Run();
                }
                catch (Exception err)
                {
                    exception = err;
                    AddException(err);
                }
            }
        }

        /// <summary>Run all tests.</summary>
        private void RunTests()
        {
            storage?.Writer.WaitForIdle();
            storage?.Reader.Refresh();

            List<object> services;
            if (relativeTo is Simulations)
                services = (relativeTo as Simulations).GetServices();
            else
            {
                Simulations sims = Apsim.Find(relativeTo, typeof(Simulations)) as Simulations;
                if (sims != null)
                    services = sims.GetServices();
                else if (relativeTo is Simulation)
                    services = (relativeTo as Simulation).Services;
                else
                {
                    services = new List<object>();
                    if (storage != null)
                        services.Add(storage);
                }
            }

            var links = new Links(services);
            foreach (ITest test in Apsim.ChildrenRecursively(rootModel, typeof(ITest)))
            {
                DateTime startTime = DateTime.Now;

                links.Resolve(test as IModel, true);
                
                // If we run into problems, we will want to include the name of the test in the 
                // exception's message. However, tests may be manager scripts, which always have
                // a name of 'Script'. Therefore, if the test's parent is a Manager, we use the
                // manager's name instead.
                string testName = test.Parent is Manager ? test.Parent.Name : test.Name;
                Exception exception = null;
                try
                {
                    test.Run();
                }
                catch (Exception err)
                {
                    exception = err;
                    AddException(new Exception("Encountered an error while running test " + testName, err));
                }
            }
        }

        /// <summary>
        /// Add an exception to our list of exceptions.
        /// </summary>
        /// <param name="err">The exception to add.</param>
        private void AddException(Exception err)
        {
            if (err != null)
            {
                if (PrePostExceptionsThrown == null)
                    PrePostExceptionsThrown = new List<Exception>();
                PrePostExceptionsThrown.Add(err);
            }
        }

        /// <summary>
        /// Tells us to Dispose (close) our datastore
        /// </summary>
        public void DisposeStorage()
        {
            storage?.Dispose();
        }
    }
}