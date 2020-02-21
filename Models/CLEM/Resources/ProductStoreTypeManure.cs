﻿using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Models.CLEM.Resources
{
    ///<summary>
    /// Store for manure
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ProductStore))]
    [Description("This resource represents a manure store. This is a special type of Product Store Type and is needed for manure management and must be named \"Manure\".")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Products/ManureType.htm")]
    public class ProductStoreTypeManure: CLEMResourceTypeBase, IResourceWithTransactionType, IResourceType
    {
        /// <summary>
        /// Unit type
        /// </summary>
        [Description("Units (nominal)")]
        public string Units { get; set; }

        /// <summary>
        /// List of all uncollected manure stores
        /// These present manure in the field and yards
        /// </summary>
        [NonSerialized]
        public List<ManureStoreUncollected> UncollectedStores;

        /// <summary>
        /// Biomass decay rate each time step
        /// </summary>
        [Description("Biomass decay rate each time step")]
        [Required, Proportion]
        public double DecayRate { get; set; }

        /// <summary>
        /// Moisture decay rate each time step
        /// </summary>
        [Description("Moisture decay rate each time step")]
        [Required, Proportion]
        public double MoistureDecayRate { get; set; }

        /// <summary>
        /// Proportion moisture of fresh manure
        /// </summary>
        [Description("Proportion moisture of fresh manure")]
        [Required, Proportion]
        public double ProportionMoistureFresh { get; set; }

        /// <summary>
        /// Maximum age manure lasts
        /// </summary>
        [Description("Maximum age (time steps) manure lasts")]
        [Required]
        public int MaximumAge { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseResource")]
        private void OnCLEMInitialiseResource(object sender, EventArgs e)
        {
            UncollectedStores = new List<ManureStoreUncollected>();
            Initialise();
        }

        /// <summary>
        /// Overrides the base class method to allow for clean up
        /// </summary>
        [EventSubscribe("Completed")]
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
            if (UncollectedStores != null)
            {
                UncollectedStores.Clear();
            }
            UncollectedStores = null;
        }

        /// <summary>
        /// Method to add uncollected manure to stores
        /// </summary>
        /// <param name="storeName">Name of store to add manure to</param>
        /// <param name="amount">Amount (dry weight) of manure to add</param>
        public void AddUncollectedManure(string storeName, double amount)
        {
            ManureStoreUncollected store = UncollectedStores.Where(a => a.Name.ToLower() == storeName.ToLower()).FirstOrDefault();
            if(store == null)
            {
                store = new ManureStoreUncollected() { Name = storeName };
                UncollectedStores.Add(store);
            }
            ManurePool pool = store.Pools.Where(a => a.Age == 0).FirstOrDefault();
            if(pool == null)
            {
                pool = new ManurePool() { Age = 0, ProportionMoisture= ProportionMoistureFresh };
                store.Pools.Add(pool);
            }
            pool.Amount += amount;
        }

        /// <summary>
        /// Function to age manure pools
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAgeResources")]
        private void OnCLEMAgeResources(object sender, EventArgs e)
        {
            // decay Amount and Moisture of pools and age by 1 month
            foreach (ManureStoreUncollected store in UncollectedStores)
            {
                foreach (ManurePool pool in store.Pools)
                {
                    pool.Age++;
                    pool.Amount *= DecayRate;
                    pool.ProportionMoisture *= MoistureDecayRate;
                }
                store.Pools.RemoveAll(a => a.Age > MaximumAge);
            }
        }

        /// <summary>
        /// Method to collect manure from uncollected manure stores
        /// Manure is collected from freshest to oldest
        /// </summary>
        /// <param name="storeName">Name of store to add manure to</param>
        /// <param name="resourceLimiter">Reduction due to limited resources</param>
        /// <param name="activity">Name of activity performing collection</param>
        public void Collect(string storeName, double resourceLimiter, CLEMModel activity)
        {
            ManureStoreUncollected store = UncollectedStores.Where(a => a.Name.ToLower() == storeName.ToLower()).FirstOrDefault();
            if (store != null)
            {
                double limiter = Math.Max(Math.Min(resourceLimiter, 1.0), 0);
                double amountPossible = store.Pools.Sum(a => a.Amount) * limiter;
                double amountMoved = 0;

                while (store.Pools.Count > 0 && amountMoved<amountPossible)
                {
                    // take needed
                    double take = Math.Min(amountPossible - amountMoved, store.Pools[0].Amount);
                    amountMoved += take;
                    store.Pools[0].Amount -= take; 
                    // if 0 delete
                    store.Pools.RemoveAll(a => a.Amount == 0);
                }
                this.Add(amountMoved, activity, ((storeName=="")?"General":storeName));
            }
        }

        /// <summary>
        /// Current amount of this resource
        /// </summary>
        public double Amount { get { return amount; } }
        private double amount { get { return roundedAmount; } set { roundedAmount = Math.Round(value, 9); } }
        private double roundedAmount;

        /// <summary>
        /// Initialise resource type
        /// </summary>
        public void Initialise()
        {
            this.amount = 0;
        }

        #region transactions

        /// <summary>
        /// Back account transaction occured
        /// </summary>
        public event EventHandler TransactionOccurred;

        /// <summary>
        /// Transcation occurred 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnTransactionOccurred(EventArgs e)
        {
            TransactionOccurred?.Invoke(this, e);
        }

        /// <summary>
        /// Last transaction received
        /// </summary>
        [XmlIgnore]
        public ResourceTransaction LastTransaction { get; set; }

        /// <summary>
        /// Add money to account
        /// </summary>
        /// <param name="resourceAmount">Object to add. This object can be double or contain additional information (e.g. Nitrogen) of food being added</param>
        /// <param name="activity">Name of activity adding resource</param>
        /// <param name="reason">Name of individual adding resource</param>
        public new void Add(object resourceAmount, CLEMModel activity, string reason)
        {
            if (resourceAmount.GetType().ToString() != "System.Double")
            {
                throw new Exception(String.Format("ResourceAmount object of type {0} is not supported Add method in {1}", resourceAmount.GetType().ToString(), this.Name));
            }
            double addAmount = (double)resourceAmount;
            if (addAmount > 0)
            {
                amount += addAmount;

                ResourceTransaction details = new ResourceTransaction
                {
                    Gain = addAmount,
                    Activity = activity,
                    Reason = reason,
                    ResourceType = this
                };
                LastTransaction = details;
                TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
                OnTransactionOccurred(te);
            }
        }

        /// <summary>
        /// Remove from finance type store
        /// </summary>
        /// <param name="request">Resource request class with details.</param>
        public new void Remove(ResourceRequest request)
        {
            if (request.Required == 0)
            {
                return;
            }
            // avoid taking too much
            double amountRemoved = request.Required;
            amountRemoved = Math.Min(this.Amount, amountRemoved);
            this.amount -= amountRemoved;

            request.Provided = amountRemoved;
            ResourceTransaction details = new ResourceTransaction
            {
                ResourceType = this,
                Loss = amountRemoved,
                Activity = request.ActivityModel,
                Reason = request.Reason
            };
            LastTransaction = details;
            TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
            OnTransactionOccurred(te);
        }

        /// <summary>
        /// Set the amount in an account.
        /// </summary>
        /// <param name="newAmount"></param>
        public new void Set(double newAmount)
        {
            amount = newAmount;
        }

        #endregion

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "<div class=\"activityentry\">";
            html += "Manure will decay at a rate of <span class=\"setvalue\">" + this.DecayRate.ToString("0.###") + "</span> each month and will only last for <span class=\"setvalue\">" + this.MaximumAge.ToString("0.#") + "</span> months.</div>";
            html += "<div class=\"activityentry\">";
            html += "Fresh manure is <span class=\"setvalue\">" + this.ProportionMoistureFresh.ToString("0.##%") + "</span> moisture and delines by "+ this.MoistureDecayRate.ToString("0.###") + "</span> each month.";
            html += "</div>";
            return html;
        }

    }

    /// <summary>
    /// Individual store of uncollected manure
    /// </summary>
    public class ManureStoreUncollected
    {
        /// <summary>
        /// Name of store (eg yards, paddock name etc)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Pools of manure in this store
        /// </summary>
        public List<ManurePool> Pools = new List<ManurePool>();
    }

    /// <summary>
    /// Individual uncollected manure pool to track age and decomposition
    /// </summary>
    public class ManurePool
    {
        /// <summary>
        /// Age of pool (in timesteps)
        /// </summary>
        public int Age { get; set; }
        /// <summary>
        /// Amount (dry weight) in pool
        /// </summary>
        public double Amount { get; set; }
        
        /// <summary>
        /// Proportion water in pool
        /// </summary>
        public double ProportionMoisture { get; set; }

        /// <summary>
        /// Acluclate wet weight of pool
        /// </summary>
        /// <param name="moistureDecayRate"></param>
        /// <param name="proportionMoistureFresh"></param>
        /// <returns></returns>
        public double WetWeight(double moistureDecayRate, double proportionMoistureFresh)
        {
            double moisture = proportionMoistureFresh;
            for (int i = 0; i < Age; i++)
            {
                moisture *= moistureDecayRate;
            }
            moisture = Math.Max(moisture, 0.05);
            return Amount * (1 + moisture);
        }

    }
}
