﻿using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Models.Core.Attributes;

namespace Models.CLEM
{
    ///<summary>
    /// Resource transmutation
    /// Will convert one resource into another (e.g. $ => labour) 
    /// These re defined under each ResourceType in the Resources section of the UI tree
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IResourceType))]
    [Description("This Transmutation will convert any other resource into the current resource where there is a shortfall. This is placed under any resource type where you need to provide a transmutation. For example to convert Finance Type (money) into a Animal Food Store Type (Lucerne) or effectively purchase fodder when low.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Transmutation/Transmutation.htm")]
    public class Transmutation: CLEMModel, IValidatableObject
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public Transmutation()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResourceLevel2;
        }

        /// <summary>
        /// Amount of this resource per unit purchased
        /// </summary>
        [Description("Amount of this resource per unit purchased")]
        [Required, GreaterThanEqualValue(0)]
        public double AmountPerUnitPurchase { get; set; }

        /// <summary>
        /// Validate this object
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (this.Children.Where(a => a.GetType().Name.Contains("TransmutationCost")).Count() == 0) //   Apsim.Children (this, typeof(TransmutationCost)).Count() == 0)
            {
                string[] memberNames = new string[] { "TransmutationCosts" };
                results.Add(new ValidationResult("No costs provided under this transmutation", memberNames));
            }
            return results;
        }

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "<div class=\"resourcebannerlight\">";
            if (AmountPerUnitPurchase > 0)
            {
                html += "When needed <span class=\"setvalue\">" + AmountPerUnitPurchase.ToString("#,##0.##") + "</span> of this resource will be converted from";
            }
            else
            {
                html += "<span class=\"errorlink\">Invalid transmutation provided. No amout to purchase set</span>";
            }
            html += "</div>";

            //if (this.Children.OfType<TransmutationCost>().Count() + this.Children.OfType<TransmutationCostLabour>().Count() == 0)
            if (this.Children.Where(a => a.GetType().Name.Contains("TransmutationCost")).Count() == 0)
            {
                html += "<div class=\"resourcebannerlight\">";
                html += "Invalid transmutation provided. No transmutation costs provided";
                html += "</div>";
            }
            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerClosingTags(bool formatForParentControl)
        {
            string html = "";
            html += "\n</div>";
            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerOpeningTags(bool formatForParentControl)
        {
            string html = "";
            html += "\n<div class=\"resourcecontentlight clearfix\">";
            if (!(this.Children.Where(a => a.GetType().Name.Contains("TransmutationCost")).Count() >= 1))
            {
                html += "<div class=\"errorlink\">No transmutation costs provided</div>";
            }
            return html;
        }
    }

    ///<summary>
    /// Resource transmutation cost item
    /// Determines the amount of resource required for the transmutation
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Transmutation))]
    [Description("This Transmutation cost specifies how much of a given resource (e.g. money) is needed to convert to the needed resource. Any number of these can be supplied under a Transmutation such that you may need money and labour to purchase supplements.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"content/features/transmutation/transmutationcost.htm")]
    public class TransmutationCost : CLEMModel, IValidatableObject, ITransmutationCost
    {
        [XmlIgnore]
        [Link]
        private ResourcesHolder Resources = null;

        /// <summary>
        /// Type of resource to use
        /// </summary>
        public Type ResourceType { get; set; }

        /// <summary>
        /// Name of resource type to use
        /// </summary>
        [Description("Name of Resource Type to use")]
        [Models.Core.Display(Type = DisplayType.CLEMResourceName, CLEMResourceNameResourceGroups = new Type[] { typeof(AnimalFoodStore), typeof(Finance), typeof(HumanFoodStore), typeof(GreenhouseGases), typeof(Labour), typeof(ProductStore), typeof(WaterStore) })]
        [Required]
        public string ResourceTypeName { get; set; }

        /// <summary>
        /// Cost of transmutation
        /// </summary>
        [Description("Cost per unit")]
        [Required, GreaterThanEqualValue(0)]
        public double CostPerUnit { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public TransmutationCost()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResource;
        }

        /// <summary>
        /// Validate this object
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (ResourceTypeName != null && ResourceTypeName != "")
            {
                if (!ResourceTypeName.Contains("."))
                {
                    string[] memberNames = new string[] { "ResourceTypeName" };
                    results.Add(new ValidationResult("Invalid resource type entry. Please select resource type from the drop down list provided or ensure the value is formatted as ResourceGroup.ResourceType", memberNames));
                }
                else
                {
                    object result = Resources.GetResourceGroupByName(ResourceTypeName.Split('.').First());
                    if (result == null)
                    {
                        string[] memberNames = new string[] { "ResourceTypeName" };
                        results.Add(new ValidationResult("Could not find resource " + ResourceTypeName.Split('.').First() + " in transmutation cost", memberNames));
                    }
                }
            }
            return results;
        }

        // This was in commencing, but I don't think there is any reason it has to be
        // could be a problem in future, thus this message.


        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            // determine resource type from name
            object result = Resources.GetResourceGroupByName(ResourceTypeName.Split('.').First());
            if (result != null)
            {
                ResourceType = result.GetType();
            }
        }

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            if (CostPerUnit > 0)
            {
                html += "<div class=\"activityentry\">";
                html += "<span class=\"setvalue\">"+CostPerUnit.ToString("#,##0.##") + "</span> x ";
                html += (ResourceTypeName!=null && ResourceTypeName!="")? "<span class=\"resourcelink\">" + ResourceTypeName+"</span>": "<span class=\"errorlink\">Unknown Resource</span>";
                html += "</div>";
            }
            else
            {
                html += "<div class=\"errorlink\">";
                html += "Invalid transmutation cost. No cost per unit provided.";
                html += "</div>";
            }
            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryClosingTags(bool formatForParentControl)
        {
            return "";
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryOpeningTags(bool formatForParentControl)
        {
            return "";
        }

    }

    ///<summary>
    /// Resource transmutation labour cost item
    /// Determines the amount of labour required for the transmutation
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Transmutation))]
    [Description("This Transmutation cost specifies how much of a given resource (e.g. money) is needed to convert to the needed resource. Any number of these can be supplied under a Transmutation such that you may need money and labour to purchase supplements.")]
    [HelpUri(@"content/features/transmutation/transmutationcostlabour.htm")]
    public class TransmutationCostLabour : CLEMModel, IValidatableObject, ITransmutationCost
    {
        /// <summary>
        /// Type of resource to use
        /// </summary>
        public Type ResourceType { get; set; }

        /// <summary>
        /// Cost of transmutation
        /// </summary>
        [Description("Days per unit")]
        [Required, GreaterThanEqualValue(0)]
        public double CostPerUnit { get; set; }

        /// <summary>
        /// Name of resource type to use
        /// Not used in this model but part of interface
        /// </summary>
        public string ResourceTypeName { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public TransmutationCostLabour()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResource;
        }

        /// <summary>
        /// Validate this object
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            return results;
        }

        // This was in commencing, but I don't think there is any reason it has to be
        // could be a problem in future, thus this message.

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            ResourceType = typeof(Labour);
        }

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            if (CostPerUnit > 0)
            {
                html += "<div class=\"activityentry\">";
                html += "<span class=\"setvalue\">" + CostPerUnit.ToString("#,##0.##") + "</span> days from ";
                html += "</div>";
            }
            else
            {
                html += "<div class=\"errorlink\">";
                html += "Invalid transmutation cost. No days labour per unit provided.";
                html += "</div>";
            }
            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryClosingTags(bool formatForParentControl)
        {
            return "";
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryOpeningTags(bool formatForParentControl)
        {
            return "";
        }

    }

    ///<summary>
    /// Resource transmutation cost using defined finance pricing
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Transmutation))]
    [Description("This Transmutation cost uses the pricing drfined for the given resource.")]
    [HelpUri(@"content/features/transmutation/transmutationcostusepricing.htm")]
    public class TransmutationCostUsePricing : CLEMModel, IValidatableObject, ITransmutationCost
    {
        private ResourcePricing pricing;

        /// <summary>
        /// Type of resource to use
        /// </summary>
        public Type ResourceType { get; set; }

        /// <summary>
        /// Name of resource type to use
        /// </summary>
        [Description("Name of Resource Type to use")]
        [Models.Core.Display(Type = DisplayType.CLEMResourceName, CLEMResourceNameResourceGroups = new Type[] { typeof(Finance) })]
        [Required]
        public string ResourceTypeName { get; set; }

        /// <summary>
        /// Cost per unit taken from pricing component if available.
        /// </summary>
        [XmlIgnore]
        public double CostPerUnit
        {
            get
            {
                if(pricing != null)
                {
                    return pricing.PricePerPacket;
                }
                else
                {
                    return 0;
                }
            }
            set
            {

            }
        }

        /// <summary>
        /// Get the price object for this transmutation cost
        /// </summary>
        public ResourcePricing Pricing { get {return pricing; } }

        /// <summary>
        /// Constructor
        /// </summary>
        public TransmutationCostUsePricing()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResource;
        }

        /// <summary>
        /// Validate this object
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            return results;
        }

        // This was in commencing, but I don't think there is any reason it has to be
        // could be a problem in future, thus this message.

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            ResourceType = typeof(Finance);

            // get pricing if available
            pricing = Apsim.Children(Apsim.Parent(this, typeof(IResourceType)), typeof(ResourcePricing)).FirstOrDefault() as ResourcePricing;
        }

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";

            // get the pricing 
            ResourcePricing price = Apsim.Children(Apsim.Parent(this, typeof(IResourceType)), typeof(ResourcePricing)).FirstOrDefault() as ResourcePricing;
            if (price!=null)
            {
                html += "<div class=\"activityentry\">Use ";
                html += (ResourceTypeName != null && ResourceTypeName != "") ? "<span class=\"resourcelink\">" + ResourceTypeName + "</span>" : "<span class=\"errorlink\">Account not set</span>";
                html += " based upon the <span class=\"resourcelink\">" + price.Name+"</span> packet size and price for this resource</div>";
            }
            else
            {
                html += "<div class=\"errorlink\">";
                html += "Invalid transmutation cost. Cannot find a [r=ResourcePricing] for this resource.";
                html += "</div>";
            }
            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryClosingTags(bool formatForParentControl)
        {
            return "";
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryOpeningTags(bool formatForParentControl)
        {
            return "";
        }

    }


    /// <summary>
    /// Interface for transmutation costs
    /// </summary>
    public interface ITransmutationCost
    {
        /// <summary>
        ///Resource type to use
        /// </summary>
        [Description("Resource type to use")]
        Type ResourceType { get; set; }

        /// <summary>
        /// Cost per unit
        /// </summary>
        [Description("Cost per unit")]
        double CostPerUnit { get; set; }

        /// <summary>
        /// Name of resource type to use
        /// </summary>
        [Description("Name of Resource Type to use")]
        string ResourceTypeName { get; set; }
    }

}
