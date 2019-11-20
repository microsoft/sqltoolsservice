//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;

namespace Microsoft.Kusto.ServiceLayer.Scripting.Contracts
{
    /// <summary>
    /// Class to represent a database object that can be scripted.
    /// </summary>
    public sealed class ScriptingObject : IEquatable<ScriptingObject>
    {
        /// <summary>
        /// Gets or sets the database object type.  
        /// </summary>
        /// <remarks>
        /// This underlying values are determined by the SqlScriptPublishModel.GetDatabaseObjectTypes() and
        /// can change depending on the version of SMO used by the tools service.  Values can be: 
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
                StringComparer.OrdinalIgnoreCase.GetHashCode(this.Type ?? string.Empty) ^
                StringComparer.OrdinalIgnoreCase.GetHashCode(this.Schema ?? string.Empty) ^
                StringComparer.OrdinalIgnoreCase.GetHashCode(this.Name ?? string.Empty);
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
