﻿using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Models.CLEM.Resources
{
    ///<summary>
    /// Parent model of finance models.
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ResourcesHolder))]
    [Description("This resource group holds all finance types (bank accounts) for the simulation.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Finance/Finance.htm")]
    public class Finance : ResourceBaseWithTransactions
    {
        /// <summary>
        /// Currency used
        /// </summary>
        [Description("Name of currency")]
        public string CurrencyName { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            foreach (var child in Children)
            {
                if (child is IResourceWithTransactionType)
                {
                    (child as IResourceWithTransactionType).TransactionOccurred += Resource_TransactionOccurred; ;
                }
            }
        }

        /// <summary>
        /// Overrides the base class method to allow for clean up
        /// </summary>
        [EventSubscribe("Completed")]
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
            foreach (IResourceWithTransactionType childModel in Apsim.Children(this, typeof(IResourceWithTransactionType)))
            {
                childModel.TransactionOccurred -= Resource_TransactionOccurred;
            }
        }

        #region Transactions

        // Must be included away from base class so that APSIM Event.Subscriber can find them 

        /// <summary>
        /// Override base event
        /// </summary>
        protected new void OnTransactionOccurred(EventArgs e)
        {
            TransactionOccurred?.Invoke(this, e);
        }

        /// <summary>
        /// Override base event
        /// </summary>
        public new event EventHandler TransactionOccurred;

        private void Resource_TransactionOccurred(object sender, EventArgs e)
        {
            LastTransaction = (e as TransactionEventArgs).Transaction;
            OnTransactionOccurred(e);
        }

        #endregion

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            if(CurrencyName!=null && CurrencyName!="")
            {
                html += "<div class=\"activityentry\">Currency is <span class=\"setvalue\">" + CurrencyName+"</span></div>";
            }
            else
            {
                html += "<div class=\"activityentry\">Currency is <span class=\"errorlink\">Not specified</span></div>";
            }
            return html;
        }

    }
}
