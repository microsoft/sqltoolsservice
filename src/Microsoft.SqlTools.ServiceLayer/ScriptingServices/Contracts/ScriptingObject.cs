//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.ServiceLayer.ScriptingServices.Contracts
{
    /// <summary>
    /// Class to represent a database object that can be scripted.
    /// </summary>
    public class ScriptingObject : IEquatable<ScriptingObject>
    {
        /// <summary>
        /// Gets or sets the database object type.  
        /// </summary>
        /// <remarks>
        /// Values can be: 
        ///     Table,
        ///     View,
        ///     StoredProcedure,
        ///     UserDefinedFunction,
        ///     UserDefinedDataType,
        ///     User,
        ///     Default,
        ///     Rule,
        ///     DatabaseRole,
        ///     ApplicationRole,
        ///     SqlAssembly,
        ///     DdlTrigger,
        ///     Synonym,
        ///     XmlSchemaCollection,
        ///     Schema,
        ///     PlanGuide,
        ///     UserDefinedType,
        ///     UserDefinedAggregate,
        ///     FullTextCatalog,
        ///     UserDefinedTableType
        /// </remarks>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the schema of the database object.
        /// </summary>
        public string Schema { get; set; }

        /// <summary>
        /// Gets or sets the database object name.
        /// </summary>
        public string Name { get; set; }

        public override string ToString()
        {
            string objectName = string.IsNullOrEmpty(this.Schema)
                ? this.Name
                : this.Schema + "." + this.Name;

            return objectName;
        }

        public override int GetHashCode()
        {
            return
                StringComparer.OrdinalIgnoreCase.GetHashCode(this.Type) ^
                StringComparer.OrdinalIgnoreCase.GetHashCode(this.Schema) ^
                StringComparer.OrdinalIgnoreCase.GetHashCode(this.Name);
        }

        public override bool Equals(object obj)
        {
            return 
                obj != null &&
                this.GetType() == obj.GetType() && 
                this.Equals((ScriptingObject)obj);
        }

        public bool Equals(ScriptingObject other)
        {
            if (other == null)
            {
                return false;
            }

            return
                string.Equals(this.Type, other.Type, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(this.Schema, other.Schema, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }
    }
}
