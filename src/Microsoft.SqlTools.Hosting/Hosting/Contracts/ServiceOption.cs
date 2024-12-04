    //
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.Hosting.Contracts
{
    public class ServiceOption
    {
        public static readonly string ValueTypeString = "string";
        public static readonly string ValueTypeMultiString = "multistring";
        public static readonly string ValueTypePassword = "password";
        public static readonly string ValueTypeNumber = "number";
        public static readonly string ValueTypeCategory = "category";
        public static readonly string ValueTypeBoolean = "boolean";
        public static readonly string ValueTypeObject = "object";

        /// <summary>
        /// Defined name that can be used to reference a specific option
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Translated name of the option for display in a UI
        /// </summary>
        public string DisplayName { get; set; }

        public string Description {get; set; }

        /// <summary>
        /// Defined name that can be used to organize by group
        /// </summary>
        public string GroupName {get; set; }

        /// <summary>
        /// Translated name of the group for display in a UI
        /// </summary>
        public string GroupDisplayName { get; set; }

        /// <summary>
        /// Type of the parameter.  Can be either string, number, or category.
        /// </summary>
        public string ValueType { get; set; }

        public string DefaultValue { get; set; }

        public string ObjectType { get; set; }

        /// <summary>
        /// Set of permitted values if ValueType is category.
        /// </summary>
        public CategoryValue[] CategoryValues { get; set; }

        /// <summary>
        /// Flag to indicate that this option is required
        /// </summary>
        public bool IsRequired { get; set; }

        public bool IsArray { get; set; }
    }
}

