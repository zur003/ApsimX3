﻿using Models.Core;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models.Core.Attributes;

namespace Models.CLEM.Activities
{
    /// <summary>Crop activity task</summary>
    /// <summary>This activity will perform costs and labour for a crop activity</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CropActivityManageProduct))]
    [Description("This is a crop task (e.g. sowing) with associated costs and labour requirements.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Crop/CropTask.htm")]
    public class CropActivityTask: CLEMActivityBase, IValidatableObject
    {
        [Link]
        Clock Clock = null;

        private bool timingIssueReported = false;

        /// <summary>
        /// Constructor
        /// </summary>
        protected CropActivityTask()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubActivity;
        }

        /// <summary>
        /// Validate model
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if(this.Parent.GetType() != typeof(CropActivityManageProduct))
            {
                string[] memberNames = new string[] { "Parent model" };
                results.Add(new ValidationResult("A crop activity task must be placed immediately below a CropActivityManageProduct model component", memberNames));
            }
            return results;
        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns>List of required resource requests</returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            return null;
        }

        /// <summary>An event handler to allow to call all Activities in tree to request their resources in order.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMGetResourcesRequired")]
        private void OnGetResourcesRequired(object sender, EventArgs e)
        {
            // if first step of parent rotation
            // and timer failed because of harvest data
            int start = (Apsim.Parent(this, typeof(CropActivityManageProduct)) as CropActivityManageProduct).FirstTimeStepOfRotation;
            if (Clock.Today.Year*100+Clock.Today.Month == start)
            {
                // check if it can only occur before this rotation started
                ActivityTimerCropHarvest chtimer = Apsim.Children(this, typeof(ActivityTimerCropHarvest)).FirstOrDefault() as ActivityTimerCropHarvest;
                if (chtimer != null)
                {
                    if (chtimer.ActivityPast)
                    {
                        this.Status = ActivityStatus.Warning;
                        if(!timingIssueReported)
                        {
                            Summary.WriteWarning(this, String.Format("The harvest timer for crop task [a="+this.Name+"] did not allow the task to be performed. This is likely due to insufficient time between rotating to a crop and the next harvest date.", this.Name));
                            timingIssueReported = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Determines how much labour is required from this activity based on the requirement provided
        /// </summary>
        /// <param name="requirement">The details of how labour are to be provided</param>
        /// <returns></returns>
        public override double GetDaysLabourRequired(LabourRequirement requirement)
        {
            double daysNeeded;
            double numberUnits;
            switch (requirement.UnitType)
            {
                case LabourUnitType.Fixed:
                    daysNeeded = requirement.LabourPerUnit;
                    break;
                case LabourUnitType.perHa:
                    CropActivityManageCrop cropParent = Apsim.Parent(this, typeof(CropActivityManageCrop)) as CropActivityManageCrop;
                    CropActivityManageProduct productParent = Apsim.Parent(this, typeof(CropActivityManageProduct)) as CropActivityManageProduct;
                    numberUnits = cropParent.Area * productParent.UnitsToHaConverter / requirement.UnitSize;
                    if (requirement.WholeUnitBlocks)
                    {
                        numberUnits = Math.Ceiling(numberUnits);
                    }

                    daysNeeded = numberUnits * requirement.LabourPerUnit;
                    break;
                case LabourUnitType.perTree:
                    cropParent = Apsim.Parent(this, typeof(CropActivityManageCrop)) as CropActivityManageCrop;
                    productParent = Apsim.Parent(this, typeof(CropActivityManageProduct)) as CropActivityManageProduct;
                    numberUnits = productParent.TreesPerHa * cropParent.Area * productParent.UnitsToHaConverter / requirement.UnitSize;
                    if (requirement.WholeUnitBlocks)
                    {
                        numberUnits = Math.Ceiling(numberUnits);
                    }

                    daysNeeded = numberUnits * requirement.LabourPerUnit;
                    break;
                case LabourUnitType.perKg:
                    productParent = Apsim.Parent(this, typeof(CropActivityManageProduct)) as CropActivityManageProduct;
                    numberUnits = productParent.AmountHarvested;
                    if (requirement.WholeUnitBlocks)
                    {
                        numberUnits = Math.Ceiling(numberUnits);
                    }

                    daysNeeded = numberUnits * requirement.LabourPerUnit;
                    break;
                case LabourUnitType.perUnit:
                    productParent = Apsim.Parent(this, typeof(CropActivityManageProduct)) as CropActivityManageProduct;
                    numberUnits = productParent.AmountHarvested / requirement.UnitSize;
                    if (requirement.WholeUnitBlocks)
                    {
                        numberUnits = Math.Ceiling(numberUnits);
                    }

                    daysNeeded = numberUnits * requirement.LabourPerUnit;
                    break;
                default:
                    throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", requirement.UnitType, requirement.Name, this.Name));
            }
            return daysNeeded;
        }

        /// <summary>
        /// Method used to perform activity if it can occur as soon as resources are available.
        /// </summary>
        public override void DoActivity()
        {
            return;
        }

        /// <summary>
        /// Method to determine resources required for initialisation of this activity
        /// </summary>
        /// <returns></returns>
        public override List<ResourceRequest> GetResourcesNeededForinitialisation()
        {
            return null;
        }

        /// <summary>
        /// Resource shortfall event handler
        /// </summary>
        public override event EventHandler ResourceShortfallOccurred;

        /// <summary>
        /// Shortfall occurred 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnShortfallOccurred(EventArgs e)
        {
            ResourceShortfallOccurred?.Invoke(this, e);
        }

        /// <summary>
        /// Resource shortfall occured event handler
        /// </summary>
        public override event EventHandler ActivityPerformed;

        /// <summary>
        /// Shortfall occurred 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnActivityPerformed(EventArgs e)
        {
            ActivityPerformed?.Invoke(this, e);
        }

        /// <summary>
        /// The method allows the activity to adjust resources requested based on shortfalls (e.g. labour) before they are taken from the pools
        /// </summary>
        public override void AdjustResourcesNeededForActivity()
        {
            return;
        }

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            if(Apsim.Children(this, typeof(CropActivityFee)).Count() + Apsim.Children(this, typeof(LabourRequirement)).Count() == 0)
            {
                html += "<div class=\"errorlink\">This task is not needed as it has no fee or labour requirement</div>";
            }
            return html;
        }


    }
}
