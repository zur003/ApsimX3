﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;
using APSIM.Shared.Utilities;

namespace Models.Functions
{
    /// <summary>
    /// A function that accumulates values from child functions
    /// </summary>
    [Serializable]
    [Description("Adds the value of all children functions to the previous day's accumulation between start and end phases")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class AccumulateByDate : Model, IFunction
    {
        //Class members
        /// <summary>The accumulated value</summary>
        private double AccumulatedValue = 0;

        /// <summary>The child functions</summary>
        private List<IModel> ChildFunctions;

        /// <summary>The Clock</summary>
        [Link]
        Clock clock = null;

        /// <summary>The start date</summary>
        [Description("Date to start accumulation dd-mmm")]
        public string StartDate { get; set; }

        /// <summary>The end date</summary>
        [Description("Date to stop accumulation dd-mmm")]
        public string EndDate { get; set; }

        /// <summary>The reset date</summary>
        [Description("(optional) Date to reset accumulation dd-mmm")]
        public string ResetDate { get; set; }

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            AccumulatedValue = 0;
        }

      /// <summary>Called at the start of each day</summary>
        /// <param name="sender">Plant.cs</param>
        /// <param name="e">Event arguments</param>
        [EventSubscribe("StartOfDay")]
        private void OnStartOfDay(object sender, EventArgs e)
        {
            if (ChildFunctions == null)
                ChildFunctions = Apsim.Children(this, typeof(IFunction));

            if (DateUtilities.WithinDates(StartDate, clock.Today, EndDate))
            {
                //Accumulate values at the start of each day
                double DailyIncrement = 0.0;
                foreach (IFunction function in ChildFunctions)
                {
                    DailyIncrement += function.Value();
                }

                AccumulatedValue += DailyIncrement;
            }

            //Zero value if today is reset date
         if (DateUtilities.WithinDates(ResetDate, clock.Today, ResetDate))
               AccumulatedValue = 0;
        }


        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            return AccumulatedValue;
        }


    }
}

