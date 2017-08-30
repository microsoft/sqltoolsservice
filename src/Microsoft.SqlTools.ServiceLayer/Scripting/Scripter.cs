//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.SqlParser.Intellisense;
using Microsoft.SqlTools.Utility;
using Microsoft.SqlTools.ServiceLayer.Scripting.Contracts;
namespace Microsoft.SqlTools.ServiceLayer.Scripting
{
	internal partial class Scripter
    {

		private void Initialize()
        {
            AddSupportedType(DeclarationType.Table, "Table", "table", typeof(Table));
            AddSupportedType(DeclarationType.View, "View", "view", typeof(View));
            AddSupportedType(DeclarationType.StoredProcedure, "Procedure", "stored procedure", typeof(StoredProcedure));
            AddSupportedType(DeclarationType.Schema, "Schema", "schema", typeof(Schema));
            AddSupportedType(DeclarationType.Database, "Database", "database", typeof(Database));
            AddSupportedType(DeclarationType.UserDefinedDataType, "UserDefinedDataType", "user-defined data type", typeof(UserDefinedDataType));
            AddSupportedType(DeclarationType.UserDefinedTableType, "UserDefinedTableType", "user-defined table type", typeof(UserDefinedTableType));
            AddSupportedType(DeclarationType.Synonym, "Synonym", "", typeof(Synonym));
            AddSupportedType(DeclarationType.ScalarValuedFunction, "Function", "scalar-valued function", typeof(UserDefinedFunction));
            AddSupportedType(DeclarationType.TableValuedFunction, "Function", "table-valued function", typeof(UserDefinedFunction));
        }
	}
}
	