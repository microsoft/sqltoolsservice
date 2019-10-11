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
using  Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlTools.ServiceLayer.Scripting
{
	internal partial class Scripter
    {
		private void Initialize()
        {
            // Instantiate the mapping dictionaries 

            // Mapping for supported type
            AddSupportedType(DeclarationType.Table, "Table", "table", typeof(Table));
            AddSupportedType(DeclarationType.View, "View", "view", typeof(View));
            AddSupportedType(DeclarationType.StoredProcedure, "StoredProcedure", "stored procedure", typeof(StoredProcedure));
            AddSupportedType(DeclarationType.Schema, "Schema", "schema", typeof(Schema));
            AddSupportedType(DeclarationType.UserDefinedDataType, "UserDefinedDataType", "user-defined data type", typeof(UserDefinedDataType));
            AddSupportedType(DeclarationType.UserDefinedTableType, "UserDefinedTableType", "user-defined table type", typeof(UserDefinedTableType));
            AddSupportedType(DeclarationType.Synonym, "Synonym", "", typeof(Synonym));
            AddSupportedType(DeclarationType.ScalarValuedFunction, "Function", "scalar-valued function", typeof(UserDefinedFunction));
            AddSupportedType(DeclarationType.TableValuedFunction, "Function", "table-valued function", typeof(UserDefinedFunction));

            // Mapping for database engine edition
            targetDatabaseEngineEditionMap.Add(DatabaseEngineEdition.Unknown, "SqlServerEnterpriseEdition"); //default case
            targetDatabaseEngineEditionMap.Add(DatabaseEngineEdition.Personal, "SqlServerPersonalEdition");
            targetDatabaseEngineEditionMap.Add(DatabaseEngineEdition.Standard, "SqlServerStandardEdition");
            targetDatabaseEngineEditionMap.Add(DatabaseEngineEdition.Enterprise, "SqlServerEnterpriseEdition");
            targetDatabaseEngineEditionMap.Add(DatabaseEngineEdition.Express, "SqlServerExpressEdition");
            targetDatabaseEngineEditionMap.Add(DatabaseEngineEdition.SqlDatabase, "SqlAzureDatabaseEdition");
            targetDatabaseEngineEditionMap.Add(DatabaseEngineEdition.SqlDataWarehouse, "SqlDatawarehouseEdition");
            targetDatabaseEngineEditionMap.Add(DatabaseEngineEdition.SqlStretchDatabase, "SqlServerStretchEdition");
            targetDatabaseEngineEditionMap.Add(DatabaseEngineEdition.SqlManagedInstance, "SqlServerManagedInstance");
            targetDatabaseEngineEditionMap.Add(DatabaseEngineEdition.SqlOnDemand, "SqlOnDemand");

            // Mapping for database engine type
            serverVersionMap.Add(9, "Script90Compat");
            serverVersionMap.Add(10, "Script100Compat");
            serverVersionMap.Add(11, "Script110Compat");
            serverVersionMap.Add(12, "Script120Compat");
            serverVersionMap.Add(13, "Script140Compat");
            serverVersionMap.Add(14, "Script140Compat");
            serverVersionMap.Add(15, "Script140Compat");

            // Mapping the object types for scripting
            objectScriptMap.Add("table", "Table");
            objectScriptMap.Add("view", "View");
            objectScriptMap.Add("function", "Function");
            objectScriptMap.Add("storedprocedure", "Procedure");
            objectScriptMap.Add("userdefinedfunction", "Function");
            objectScriptMap.Add("tablevaluedfunction", "Function");
            objectScriptMap.Add("userdefineddatatype", "Type");
            objectScriptMap.Add("user", "User");
            objectScriptMap.Add("default", "Default");
            objectScriptMap.Add("rule", "Rule");
            objectScriptMap.Add("databaserole", "Role");
            objectScriptMap.Add("applicationrole", "Application Role");
            objectScriptMap.Add("sqlassembly", "Assembly");
            objectScriptMap.Add("ddltrigger", "Trigger");
            objectScriptMap.Add("synonym", "Synonym");
            objectScriptMap.Add("xmlschemacollection", "Xml Schema Collection");
            objectScriptMap.Add("schema", "Schema");
            objectScriptMap.Add("planguide", "sp_create_plan_guide");
            objectScriptMap.Add("userdefinedtype", "Type");
            objectScriptMap.Add("userdefinedaggregate", "Aggregate");
            objectScriptMap.Add("fulltextcatalog", "Fulltext Catalog");
            objectScriptMap.Add("userdefinedtabletype", "Type");
        }
	}
}
	