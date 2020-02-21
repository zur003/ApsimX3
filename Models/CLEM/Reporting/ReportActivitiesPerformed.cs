﻿using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models.Report;
using APSIM.Shared.Utilities;
using System.Data;
using System.IO;
using Models.CLEM.Resources;
using Models.Core.Attributes;
using Models.Core.Run;
using Models.Storage;

namespace Models.CLEM.Reporting
{
    /// <summary>
    /// A report class for writing output to the data store.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.ReportActivityLedgerView")]
    [PresenterName("UserInterface.Presenters.ReportActivityLedgerPresenter")]
    [ValidParent(ParentType = typeof(Zone))]
    [ValidParent(ParentType = typeof(Zones.CircularZone))]
    [ValidParent(ParentType = typeof(Zones.RectangularZone))]
    [ValidParent(ParentType = typeof(Simulation))]
    [ValidParent(ParentType = typeof(CLEMFolder))]
    [Description("This report automatically generates an activity performed ledger and provides a table of activity success.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Reporting/ActivitiesPerformed.htm")]
    public class ReportActivitiesPerformed: Models.Report.Report
    {
        /// <summary>The columns to write to the data store.</summary>
        [NonSerialized]
        private List<IReportColumn> columns = null;
        [NonSerialized]
        private ReportData dataToWriteToDb = null;

        /// <summary>Link to a simulation</summary>
        [Link]
        private Simulation simulation = null;

        /// <summary>Link to a clock model.</summary>
        [Link]
        private IClock clock = null;

        /// <summary>Link to a storage service.</summary>
        [Link]
        private IDataStore storage = null;

        /// <summary>Link to a locator service.</summary>
        [Link]
        private ILocator locator = null;

        /// <summary>Link to an event service.</summary>
        [Link]
        private IEvent events = null;

        /// <summary>An event handler to allow us to initialize ourselves.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        [EventSubscribe("Commencing")]
        private void OnCommencing(object sender, EventArgs e)
        {
            dataToWriteToDb = null;

            VariableNames = new string[]
            {
                "[Clock].Today as Date",
                "[Activities].LastActivityPerformed.Name as Name",
                "[Activities].LastActivityPerformed.Status as Status",
                "[Activities].LastActivityPerformed.UniqueID as UniqueID"
            };

            EventNames = new string[] { "[Activities].ActivityPerformed" };

            // Tidy up variable/event names.
            VariableNames = TidyUpVariableNames();
            EventNames = TidyUpEventNames();
            this.FindVariableMembers();

            // Subscribe to events.
            foreach (string eventName in EventNames)
            {
                if (eventName != string.Empty)
                {
                    events.Subscribe(eventName.Trim(), DoOutputEvent);
                }
            }
        }

        [EventSubscribe("Completed")]
        private void OnCompleted(object sender, EventArgs e)
        {
            if (dataToWriteToDb != null)
            {
                storage.Writer.WriteTable(dataToWriteToDb);
            }
            dataToWriteToDb = null;
        }
        /// <summary>A method that can be called by other models to perform a line of output.</summary>
        public new void DoOutput()
        {
            if (dataToWriteToDb == null)
            {
                dataToWriteToDb = new ReportData()
                {
                    SimulationName = simulation.Name,
                    TableName = Name,
                    ColumnNames = columns.Select(c => c.Name).ToList(),
                    ColumnUnits = columns.Select(c => c.Units).ToList()
                };
            }
            List<object> valuesToWrite = new List<object>();
            for (int i = 0; i < columns.Count; i++)
            {
                valuesToWrite.Add(columns[i].GetValue());
            }

            dataToWriteToDb.Rows.Add(valuesToWrite);

            if (dataToWriteToDb.Rows.Count == 100)
            {
                storage.Writer.WriteTable(dataToWriteToDb);
                dataToWriteToDb = null;
            }
        }

        /// <summary>Create a text report from tables in this data store.</summary>
        /// <param name="storage">The data store.</param>
        /// <param name="fileName">Name of the file.</param>
        public static new void WriteAllTables(IDataStore storage, string fileName)
        {
            // Write out each table for this simulation.
            foreach (string tableName in storage.Reader.TableNames)
            {
                DataTable data = storage.Reader.GetData(tableName);
                if (data != null && data.Rows.Count > 0)
                {
                    SortColumnsOfDataTable(data);
                    StreamWriter report = new StreamWriter(Path.ChangeExtension(fileName, "." + tableName + ".csv"));
                    DataTableUtilities.DataTableToText(data, 0, ",", true, report);
                    report.Close();
                }
            }
        }

        /// <summary>Sort the columns alphabetically</summary>
        /// <param name="table">The table to sort</param>
        private static void SortColumnsOfDataTable(DataTable table)
        {
            var columnArray = new DataColumn[table.Columns.Count];
            table.Columns.CopyTo(columnArray, 0);
            var ordinal = -1;
            foreach (var orderedColumn in columnArray.OrderBy(c => c.ColumnName))
            {
                orderedColumn.SetOrdinal(++ordinal);
            }

            ordinal = -1;
            int i = table.Columns.IndexOf("SimulationName");
            if (i != -1)
            {
                table.Columns[i].SetOrdinal(++ordinal);
            }

            i = table.Columns.IndexOf("SimulationID");
            if (i != -1)
            {
                table.Columns[i].SetOrdinal(++ordinal);
            }
        }


        /// <summary>Called when one of our 'EventNames' events are invoked</summary>
        public new void DoOutputEvent(object sender, EventArgs e)
        {
            DoOutput();
        }

        /// <summary>
        /// Fill the Members list with VariableMember objects for each variable.
        /// </summary>
        private void FindVariableMembers()
        {
            this.columns = new List<IReportColumn>();

            AddExperimentFactorLevels();

            foreach (string fullVariableName in this.VariableNames)
            {
                if (fullVariableName != string.Empty)
                {
                    this.columns.Add(ReportColumn.Create(fullVariableName, clock, storage.Writer, locator, events));
                }
            }
        }

        /// <summary>Add the experiment factor levels as columns.</summary>
        private void AddExperimentFactorLevels()
        {
            if (simulation.Descriptors != null)
            {
                foreach (var descriptor in simulation.Descriptors)
                {
                    if (descriptor.Name != "Zone" && descriptor.Name != "SimulationName")
                    {
                        this.columns.Add(new ReportColumnConstantValue(descriptor.Name, descriptor.Value));
                    }
                }
            }
        }

    }
}
