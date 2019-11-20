//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Globalization;

namespace Microsoft.Kusto.ServiceLayer.DisasterRecovery.Contracts
{
    public class LocalizedPropertyInfo
    {

        private string propertyValueDisplayName;
        private string propertyDisplayName;

        /// <summary>
        /// Property name
        /// </summary>
        public string PropertyName { get; set; }


        /// <summary>
        /// Property value
        /// </summary>
        public object PropertyValue { get; set; }

        /// <summary>
        /// Property display name
        /// </summary>
        public string PropertyDisplayName
        {
            get
            {
                return string.IsNullOrEmpty(this.propertyDisplayName) ? PropertyName : this.propertyDisplayName;
            }
            set
            {
                this.propertyDisplayName = value;
            }
        }

        /// <summary>
        /// Property display name for the value
        /// </summary>
        public string PropertyValueDisplayName
        {
            get
            {
                return string.IsNullOrEmpty(propertyValueDisplayName) ? GetLocalizedPropertyValue() : propertyValueDisplayName;
            }
            set
            {
                this.propertyValueDisplayName = value;
            }
        }

        private string GetLocalizedPropertyValue()
        {
            string displayName = string.Empty;
            if(PropertyValue is DateTime)
            {
                displayName = ((DateTime)PropertyValue) != DateTime.MinValue ? Convert.ToString(PropertyValue, CultureInfo.CurrentCulture) : string.Empty;
            }
            else
            {
                displayName = Convert.ToString(PropertyValue, CultureInfo.CurrentCulture);
            }
            return displayName;
        }
    }
}
