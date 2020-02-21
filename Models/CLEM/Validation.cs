﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM
{
    /// <summary>
    /// Tests if date greater than specified property name
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DateGreaterThanAttribute : ValidationAttribute
    {
        private string DefaultErrorMessage;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dateToCompareToFieldName"></param>
        public DateGreaterThanAttribute(string dateToCompareToFieldName)
        {
            this.dateToCompareToFieldName = dateToCompareToFieldName;
        }

        private string dateToCompareToFieldName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            // check valid property name
            if (validationContext.ObjectType.GetProperty(dateToCompareToFieldName) == null)
            {
                throw new Exception(String.Format("Invalid property name [{0}] provided for validation attribute [DateGreaterThan] on property [{1}] in [{2}]", dateToCompareToFieldName, validationContext.MemberName, validationContext.ObjectInstance.ToString()));
            }

            DateTime laterDate = (DateTime)value;
            DateTime earlierDate = (DateTime)validationContext.ObjectType.GetProperty(dateToCompareToFieldName).GetValue(validationContext.ObjectInstance, null);
            string[] memberNames = new string[] { validationContext.MemberName };

            if (laterDate > earlierDate)
            {
                return ValidationResult.Success;
            }
            else
            {
                DefaultErrorMessage = "Date [" + laterDate.ToString() + "] must be greater than " + dateToCompareToFieldName.ToString() +"[" + earlierDate.ToString() +"]";
                return new ValidationResult(ErrorMessage ?? DefaultErrorMessage, memberNames);
            }
        }
    }

    /// <summary>
    /// Tests if double/int is percentage
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class PercentageAttribute : ValidationAttribute
    {
        private string DefaultErrorMessage = "Value must be a percentage (0-100)";

        /// <summary>
        /// 
        /// </summary>
        public PercentageAttribute()
        {
        }

        private double compareValue { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            double maxvalue = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            string[] memberNames = new string[] { validationContext.MemberName };

            if ((maxvalue >= 0)&&(maxvalue<=100))
            {
                return ValidationResult.Success;
            }
            else
            {
                return new ValidationResult(ErrorMessage ?? DefaultErrorMessage, memberNames);
            }
        }
    }

    /// <summary>
    /// Tests if double/int is percentage
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ProportionAttribute : ValidationAttribute
    {
        private string DefaultErrorMessage = "Value must be a proportion (0-1)";

        /// <summary>
        /// 
        /// </summary>
        public ProportionAttribute()
        {
        }

        private double compareValue { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            double maxvalue = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            string[] memberNames = new string[] { validationContext.MemberName };

            if ((maxvalue >= 0) && (maxvalue <= 1))
            {
                return ValidationResult.Success;
            }
            else
            {
                return new ValidationResult(ErrorMessage ?? DefaultErrorMessage, memberNames);
            }
        }
    }

    /// <summary>
    /// Tests if int is month range
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class MonthAttribute : ValidationAttribute
    {
        private string DefaultErrorMessage = "Value must represent a month from [1] (Jan) to [12] (Dec)";

        /// <summary>
        /// 
        /// </summary>
        public MonthAttribute()
        {
        }

        private double compareValue { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            int monthvalue = Convert.ToInt32(value, CultureInfo.InvariantCulture);
            string[] memberNames = new string[] { validationContext.MemberName };

            if ((monthvalue >= 1) && (monthvalue <= 12))
            {
                return ValidationResult.Success;
            }
            else
            {
                return new ValidationResult(ErrorMessage ?? DefaultErrorMessage, memberNames);
            }
        }
    }

    /// <summary>
    /// Tests if double/int greater than specified value
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class GreaterThanValueAttribute : ValidationAttribute
    {
        private string DefaultErrorMessage;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public GreaterThanValueAttribute(object value)
        {
            compareValue = Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }

        private double compareValue { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            double maxvalue = Convert.ToDouble(value);
            string[] memberNames = new string[] { validationContext.MemberName };

            if (maxvalue > compareValue)
            {
                return ValidationResult.Success;
            }
            else
            {
                DefaultErrorMessage = "Value [" + maxvalue.ToString() + "] must be greater than [" + compareValue.ToString() +"]";
                return new ValidationResult(ErrorMessage ?? DefaultErrorMessage, memberNames);
            }
        }
    }

    /// <summary>
    /// Tests if double/int greater than specified value
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class GreaterThanEqualValueAttribute : ValidationAttribute
    {
        private string DefaultErrorMessage;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public GreaterThanEqualValueAttribute(object value)
        {
            compareValue = Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }

        private double compareValue { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            double maxvalue = Convert.ToDouble(value);
            string[] memberNames = new string[] { validationContext.MemberName };

            if (maxvalue >= compareValue)
            {
                return ValidationResult.Success;
            }
            else
            {
                DefaultErrorMessage = "Value [" + maxvalue.ToString() + "] must be greater than or equal to [" + compareValue.ToString()+"]";
                return new ValidationResult(ErrorMessage ?? DefaultErrorMessage, memberNames);
            }
        }
    }


    /// <summary>
    /// Tests if date greater than specified property name
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class GreaterThanAttribute : ValidationAttribute
    {
        private string DefaultErrorMessage;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="compareToFieldName"></param>
        public GreaterThanAttribute(string compareToFieldName)
        {
            this.compareToFieldName = compareToFieldName;
        }

        private string compareToFieldName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            double maxvalue = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            string[] memberNames = new string[] { validationContext.MemberName };

            // check valid property name
            if(validationContext.ObjectType.GetProperty(compareToFieldName) == null)
            {
                throw new Exception(String.Format("Invalid property name [{0}] provided for validation attribute [GreaterThan] on property [{1}] in [{2}]", compareToFieldName, validationContext.MemberName, validationContext.ObjectInstance.ToString()));
            }

            double minvalue = Convert.ToDouble(validationContext.ObjectType.GetProperty(compareToFieldName).GetValue(validationContext.ObjectInstance, null), CultureInfo.InvariantCulture);

            if (maxvalue > minvalue)
            {
                return ValidationResult.Success;
            }
            else
            {
                DefaultErrorMessage = "Value [" + maxvalue.ToString() + "] must be greater than " + compareToFieldName + "[" + minvalue.ToString() +"]";
                return new ValidationResult(ErrorMessage ?? DefaultErrorMessage, memberNames);
            }
        }
    }

    /// <summary>
    /// Tests if date greater than specified property name
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class GreaterThanEqualAttribute : ValidationAttribute
    {
        private string DefaultErrorMessage;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="compareToFieldName"></param>
        public GreaterThanEqualAttribute(string compareToFieldName)
        {
            this.compareToFieldName = compareToFieldName;
        }

        private string compareToFieldName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            double maxvalue = Convert.ToDouble(value);
            string[] memberNames = new string[] { validationContext.MemberName };

            // check valid property name
            if (validationContext.ObjectType.GetProperty(compareToFieldName) == null)
            {
                throw new Exception(String.Format("Invalid property name [{0}] provided for validation attribute [DateGreaterThan] on property [{1}] in [{2}]", compareToFieldName, validationContext.MemberName, validationContext.ObjectInstance.ToString()));
            }

            double minvalue = Convert.ToDouble(validationContext.ObjectType.GetProperty(compareToFieldName).GetValue(validationContext.ObjectInstance, null), CultureInfo.InvariantCulture);

            if (maxvalue >= minvalue)
            {
                return ValidationResult.Success;
            }
            else
            {
                DefaultErrorMessage = "Value [" + maxvalue.ToString() + "] must be greater than or equal to " + compareToFieldName + "[" + minvalue.ToString() + "]";
                return new ValidationResult(ErrorMessage ?? DefaultErrorMessage, memberNames);
            }
        }
    }

    /// <summary>
    /// Tests if date greater than specified property name
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ArrayItemCountAttribute : ValidationAttribute
    {
        private string DefaultErrorMessage =
            "Invalid number of values supplied";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="arrayItems"></param>
        public ArrayItemCountAttribute(int arrayItems)
        {
            numberOfArrayItems = arrayItems;
        }

        private int numberOfArrayItems { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            DefaultErrorMessage += " (expecting " + numberOfArrayItems.ToString() + " values)";
            string[] memberNames = new string[] { validationContext.MemberName };

            if(value.GetType().IsArray)
            {
                if ((value as Array).Length == numberOfArrayItems)
                {
                    return ValidationResult.Success;
                }
                else
                {
                    return new ValidationResult(ErrorMessage ?? DefaultErrorMessage, memberNames);
                }
            }
            else
            {
                return new ValidationResult(ErrorMessage ?? DefaultErrorMessage, memberNames);
            }
        }
    }



}
