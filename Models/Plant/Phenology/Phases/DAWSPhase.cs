using System;
using Models.Core;
using Models.PMF.Organs;
using System.Xml.Serialization;
using Models.PMF.Struct;
using System.IO;
using Models.Functions;


namespace Models.PMF.Phen
{
    /// <summary>
    /// It proceeds until the last leaf on the main-stem has fully senessced.  Therefore its duration depends on the number of main-stem leaves that are produced and the rate at which they seness following final leaf appearance.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Phenology))]
    public class DAWSPhase : Model, IPhase
    {
        // 1. Links
        //----------------------------------------------------------------------------------------------------------------

        [Link]
        Weather met = null;

        //2. Private and protected fields
        //-----------------------------------------------------------------------------------------------------------------

        private int StartDAWS = 0;
        private bool First = true;

        //5. Public properties
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>The start</summary>
        [Description("Start")]
        public string Start { get; set; }

        /// <summary>The end</summary>
        [Description("End")]
        public string End { get; set; }

        /// <summary>Days after winter solstice to progress from phase</summary>
        [Description("DAWStoProgress")]
        public int DAWStoProgress { get; set; }

        /// <summary>Return a fraction of phase complete.</summary>
        [XmlIgnore]
        public double FractionComplete
        {
            get
            {
                return Math.Min(1, (met.DaysSinceWinterSolstice-StartDAWS) / (DAWStoProgress-StartDAWS));
            }
        }

        //6. Public methods
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>Do our timestep development</summary>
        public bool DoTimeStep(ref double propOfDayToUse)
        {
            bool proceedToNextPhase = false;
            if (First)
            {
                StartDAWS = met.DaysSinceWinterSolstice;
                First = false;
            }

            if ((met.DaysSinceWinterSolstice >= DAWStoProgress)||((DAWStoProgress >= 365) & (met.DaysSinceWinterSolstice == 0)))
            {
                proceedToNextPhase = true;
                propOfDayToUse = 0.00001;
            }
            return proceedToNextPhase;
        }

        /// <summary>Resets the phase.</summary>
        public void ResetPhase()
        {
            First = true;
            StartDAWS = 0;
        }

        /// <summary>Writes the summary.</summary>
        /// <param name="writer">The writer.</param>
        public void WriteSummary(TextWriter writer) { writer.WriteLine("      " + Name); }

        /// <summary>Called when [simulation commencing].</summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        { ResetPhase(); }
    }
}



