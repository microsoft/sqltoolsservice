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

        public string Name { get; set; }

        public string DisplayName { get; set; }

        public string Description {get; set; }

         public string GroupName {get; set; }

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

