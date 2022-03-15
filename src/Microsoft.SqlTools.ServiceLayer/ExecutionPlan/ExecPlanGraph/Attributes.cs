//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.ExecPlanGraph
{
    #region FloatTypeConverter

    /// <summary>
    /// FloatTypeConverter is used to get a desired float / double representation 
    /// in the property sheet and the tool tip.
    /// The currently used scientific format isn't very readable
    /// 
    /// To use this converter, add the following attribute on top of the property:
    /// [TypeConverter(typeof(FloatTypeConverter))]
    /// </summary>

    public class FloatTypeConverter : TypeConverter
    {
        /// <summary>
        /// Converts the object value to another type.
        /// In this case the method only supports conversion to string.
        /// </summary>
        /// <param name="context">An ITypeDescriptorContext that provides a format context.</param>
        /// <param name="culture">A CultureInfo object. If a null reference (Nothing in Visual Basic) is passed, the current culture is assumed.</param>
        /// <param name="value">The Object to convert.</param>
        /// <param name="destinationType">The Type to convert the value parameter to.</param>
        /// <returns>The converted value.</returns>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                TypeConverter converter = TypeDescriptor.GetConverter(value);
                if (converter.CanConvertTo(typeof(double)))
                {
                    double doubleValue = (double)converter.ConvertTo(value, typeof(double));
                    return doubleValue.ToString("0.#######", CultureInfo.CurrentCulture);
                }
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
    #endregion

    #region DataSizeTypeConverter
    /// <summary>
    /// DataSizeTypeConverter is used to represent data size in bytes,
    /// kilobytes, megabytes, etc., depending on the actual number. 
    /// 
    /// To use this converter, add the following attribute on top of the property:
    /// [TypeConverter(typeof(DataSizeTypeConverter))]
    /// </summary>

    public class DataSizeTypeConverter : TypeConverter
    {
        /// <summary>
        /// Converts the object value to another type.
        /// In this case the method only supports conversion to string.
        /// </summary>
        /// <param name="context">An ITypeDescriptorContext that provides a format context.</param>
        /// <param name="culture">A CultureInfo object. If a null reference (Nothing in Visual Basic) is passed, the current culture is assumed.</param>
        /// <param name="value">The Object to convert.</param>
        /// <param name="destinationType">The Type to convert the value parameter to.</param>
        /// <returns>The converted value.</returns>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            return this.ConvertTo(context, culture, value, destinationType, 0);
        }

        /// <summary>
        /// Converts the object value to another type.
        /// In this case the method only supports conversion to string.
        /// </summary>
        /// <param name="context">An ITypeDescriptorContext that provides a format context.</param>
        /// <param name="culture">A CultureInfo object. If a null reference (Nothing in Visual Basic) is passed, the current culture is assumed.</param>
        /// <param name="value">The Object to convert.</param>
        /// <param name="destinationType">The Type to convert the value parameter to.</param>
        /// <param name="formatIndex">The index in size formats to start with.</param>
        /// <returns>The converted value.</returns>
        protected object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType, int formatIndex)
        {
            if (destinationType == typeof(string))
            {
                TypeConverter converter = TypeDescriptor.GetConverter(value);
                if (converter.CanConvertTo(typeof(double)))
                {
                    double dataSize = (double)converter.ConvertTo(value, typeof(double));
                    Debug.Assert(dataSize >= 0, "Data size must not be a negative number.");

                    // This cycle finds an optimal range for the size where no more than
                    // 4 digits are displayed. Furthermore the result is rounded to a neareast
                    // integer. So it will display sizes up to 9999 bytes in bytes then switch to
                    // to 10K. Then it will go up to 9999 KB and switch to 10M.
                    // Please note that 10000 bytes = 9.76 KB and will be rounded to 10 KB.
                    while (formatIndex < sizeFormats.Length - 1)
                    {
                        if ((long)Math.Round(dataSize) < 10000)
                        {
                            break;
                        }

                        dataSize /= 1024;
                        formatIndex++;
                    }

                    return String.Format(culture, sizeFormats[formatIndex], (long)Math.Round(dataSize));
                }
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        static string[] sizeFormats = new string[]
        {
            SR.SizeInBytesFormat,
            SR.SizeInKiloBytesFormat,
            SR.SizeInMegaBytesFormat,
            SR.SizeInGigaBytesFormat,
            SR.SizeInTeraBytesFormat
        };
    }

    #endregion

    #region KBSizeTypeConverter

    /// <summary>
    /// KBSizeTypeConverter is used to represent data size in 
    /// kilobytes, megabytes, etc., depending on the actual number. 
    /// Assumes input is in kilobytes
    /// 
    /// To use this converter, add the following attribute on top of the property:
    /// [TypeConverter(typeof(KBSizeTypeConverter))]
    /// </summary>
    public sealed class KBSizeTypeConverter : DataSizeTypeConverter
    {
        /// <summary>
        /// Converts the object value to another type.
        /// In this case the method only supports conversion to string.
        /// </summary>
        /// <param name="context">An ITypeDescriptorContext that provides a format context.</param>
        /// <param name="culture">A CultureInfo object. If a null reference (Nothing in Visual Basic) is passed, the current culture is assumed.</param>
        /// <param name="value">The Object to convert.</param>
        /// <param name="destinationType">The Type to convert the value parameter to.</param>
        /// <returns>The converted value.</returns>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            // formatIndex of 1 indicates value of input should be in KB
            return base.ConvertTo(context, culture, value, destinationType, formatIndex: 1);
        }
    }

    #endregion

    #region DisplayNameAndDescription

    /// <summary>
    /// Describes property DisplayName and description keywords.
    /// </summary>
    public class DisplayNameDescriptionAttribute : Attribute
    {
        /// <summary>
        /// Public default constructor
        /// </summary>
        /// <param name="displayName">Property DisplayName key.</param>
        public DisplayNameDescriptionAttribute(string displayName)
        {
            this.displayName = displayName;
        }

        /// <summary>
        /// Public default constructor
        /// </summary>
        /// <param name="displayName">Property DisplayName key.</param>
        /// <param name="description">Property Description key.</param>
        public DisplayNameDescriptionAttribute(string displayName, string description)
        {
            this.displayName = displayName;
            this.description = description;
        }

        /// <summary>
        /// Gets Display name
        /// </summary>
        public string DisplayName
        {
            get { return this.displayName; }
        }
        /// <summary>
        /// Gets Description
        /// </summary>
        public string Description
        {
            get { return this.description; }
        }

        private string displayName;
        private string description;
    }

    #endregion

    #region DisplayOrder

    /// <summary>
    /// Represent order attribute. Tool tip window uses this attribute to sort properties accordingly
    /// </summary>
    public sealed class DisplayOrderAttribute : Attribute
    {
        /// <summary>
        /// Public default constructor
        /// </summary>
        /// <param name="displayOrder"></param>
        public DisplayOrderAttribute(int displayOrder)
        {
            this.displayOrder = displayOrder;
        }

        /// <summary>
        /// Display order
        /// </summary>
        public int DisplayOrder
        {
            get { return this.displayOrder; }
        }

        private int displayOrder;
    }

    #endregion

    #region ShowInToolTip

    /// <summary>
    /// Represent order attribute. Tool tip window uses this attribute to sort properties accordingly
    /// </summary>

    public sealed class ShowInToolTipAttribute : Attribute
    {
        /// <summary>
        /// Public default constructor.
        /// </summary>
        public ShowInToolTipAttribute()
        {
            this.value = true;
        }

        /// <summary>
        /// Public constructor.
        /// </summary>
        /// <param name="value">Specifies whether the corresponding property should be visible in tool tips.
        /// The default value is true.</param>
        public ShowInToolTipAttribute(bool value)
        {
            this.value = value;
        }

        /// <summary>
        /// True if a property should be shown in ToolTip; otherwise false.
        /// </summary>
        public bool Value
        {
            get { return this.value; }
        }

        /// <summary>
        /// True if a property is a long string and should take an entire row in a tool tip; otherwise false.
        /// </summary>
        public bool LongString
        {
            get { return this.longString; }
            set { this.longString = value; }
        }

        private bool value = true;
        private bool longString = false;
    }

    #endregion
}
