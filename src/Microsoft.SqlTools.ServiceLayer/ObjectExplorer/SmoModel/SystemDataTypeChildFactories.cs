//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Nodes;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer.SmoModel
{
    internal partial class SystemExactNumericsChildFactory
    {
        private static readonly HashSet<string> _exactNumerics = new HashSet<string>{
                                                    "bit",
                                                    "tinyint",
                                                    "smallint",
                                                    "int",
                                                    "bigint",
                                                    "numeric",
                                                    "decimal",
                                                    "smallmoney",
                                                    "money"
                                                };
        public override bool PassesFinalFilters(TreeNode parent, object contextObject)
        {
            NamedSmoObject smoObject = contextObject as NamedSmoObject;
            if (smoObject != null)
            {
                string name = smoObject.Name;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    return _exactNumerics.Contains(name);
                }
            }
            return false;
        }
    }
    internal partial class SystemApproximateNumericsChildFactory
    {
        private static readonly HashSet<string> _approxNumerics = new HashSet<string>{
                                                    "float",
                                                    "real"
                                                };
        public override bool PassesFinalFilters(TreeNode parent, object contextObject)
        {
            NamedSmoObject smoObject = contextObject as NamedSmoObject;
            if (smoObject != null)
            {
                string name = smoObject.Name;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    return _approxNumerics.Contains(name);
                }
            }
            return false;
        }
    }
    internal partial class SystemDateAndTimesChildFactory
    {
        private static readonly HashSet<string> _dateAndTime = new HashSet<string>{
                                                    "datetime",
                                                    "smalldatetime",
                                                    "date",
                                                    "time",
                                                    "datetimeoffset",
                                                    "datetime2",
                                                };
        public override bool PassesFinalFilters(TreeNode parent, object contextObject)
        {
            NamedSmoObject smoObject = contextObject as NamedSmoObject;
            if (smoObject != null)
            {
                string name = smoObject.Name;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    return _dateAndTime.Contains(name);
                }
            }
            return false;
        }
    }
    internal partial class SystemCharacterStringsChildFactory
    {
        private static readonly HashSet<string> _characterStrings = new HashSet<string>{
                                                    "char",
                                                    "varchar",
                                                    "text",
                                                };
        public override bool PassesFinalFilters(TreeNode parent, object contextObject)
        {
            NamedSmoObject smoObject = contextObject as NamedSmoObject;
            if (smoObject != null)
            {
                string name = smoObject.Name;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    return _characterStrings.Contains(name);
                }
            }
            return false;
        }
    }
    internal partial class SystemUnicodeCharacterStringsChildFactory
    {
        private static readonly HashSet<string> _unicodeCharacterStrings = new HashSet<string>
                                            {
                                                "nchar",
                                                "nvarchar",
                                                "ntext",
                                            };
        public override bool PassesFinalFilters(TreeNode parent, object contextObject)
        {
            NamedSmoObject smoObject = contextObject as NamedSmoObject;
            if (smoObject != null)
            {
                string name = smoObject.Name;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    return _unicodeCharacterStrings.Contains(name);
                }
            }
            return false;
        }
    }
    internal partial class SystemBinaryStringsChildFactory
    {
        private static readonly HashSet<string> _binaryStrings = new HashSet<string>{
                                                    "binary",
                                                    "varbinary",
                                                    "image",
                                                };
        public override bool PassesFinalFilters(TreeNode parent, object contextObject)
        {
            NamedSmoObject smoObject = contextObject as NamedSmoObject;
            if (smoObject != null)
            {
                string name = smoObject.Name;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    return _binaryStrings.Contains(name);
                }
            }
            return false;
        }
    }
    internal partial class SystemOtherDataTypesChildFactory
    {
        private static readonly HashSet<string> _otherDataTypes = new HashSet<string>{
                                                    "sql_variant",
                                                    "timestamp",
                                                    "uniqueidentifier",
                                                    "xml",
                                                };
        public override bool PassesFinalFilters(TreeNode parent, object contextObject)
        {
            NamedSmoObject smoObject = contextObject as NamedSmoObject;
            if (smoObject != null)
            {
                string name = smoObject.Name;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    return _otherDataTypes.Contains(name);
                }
            }
            return false;
        }
    }
    internal partial class SystemClrDataTypesChildFactory
    {
        private static readonly HashSet<string> _clrDataTypes = new HashSet<string>{
                                                    "hierarchyid",
                                                };
        public override bool PassesFinalFilters(TreeNode parent, object contextObject)
        {
            NamedSmoObject smoObject = contextObject as NamedSmoObject;
            if (smoObject != null)
            {
                string name = smoObject.Name;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    return _clrDataTypes.Contains(name);
                }
            }
            return false;
        }
    }
    internal partial class SystemSpatialDataTypesChildFactory
    {
        private static readonly HashSet<string> _spatialDataTypes = new HashSet<string>{
                                                    "geometry",
                                                    "geography",
                                                };
        public override bool PassesFinalFilters(TreeNode parent, object contextObject)
        {
            NamedSmoObject smoObject = contextObject as NamedSmoObject;
            if (smoObject != null)
            {
                string name = smoObject.Name;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    return _spatialDataTypes.Contains(name);
                }
            }
            return false;
        }
    }
}
