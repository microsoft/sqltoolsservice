//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Linq;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts
{
    /// <summary>
    /// Class includes information about a file related to a database operation. 
    /// Can be used for backup set files or restored database files
    /// </summary>
    public class DatabaseFileInfo
    {
        /// <summary>
        /// The property name used for ids
        /// </summary>
        public const string IdPropertyName = "Id";

        public DatabaseFileInfo(LocalizedPropertyInfo[] properties)
        {
            Validate.IsNotNull("properties", properties);

            this.Properties = properties;
            if (this.Properties != null )
            {
                var idProperty = this.Properties.FirstOrDefault(x => x.PropertyName == IdPropertyName);
                Id = idProperty == null || idProperty.PropertyValue == null ? string.Empty : idProperty.PropertyValue.ToString();
            }
            IsSelected = true;
        }

        /// <summary>
        /// Properties
        /// </summary>
        public LocalizedPropertyInfo[] Properties { get; private set; }

        public string GetPropertyValueAsString(string name)
        {
            string value = string.Empty;
            if (Properties != null)
            {
                var property = Properties.FirstOrDefault(x => x.PropertyName == name);
                value = property == null || property.PropertyValue == null ? string.Empty : property.PropertyValue.ToString();
            }
            return value;
        }

        /// <summary>
        /// Unique id for this item
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// Indicates whether the item is selected in client
        /// </summary>
        public bool IsSelected { get; set; }
    }
}
