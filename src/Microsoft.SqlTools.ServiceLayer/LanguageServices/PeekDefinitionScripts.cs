//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Specialized;
using Microsoft.SqlServer.Management.SqlParser.Intellisense;
namespace Microsoft.SqlTools.ServiceLayer.LanguageServices
{
	internal partial class PeekDefinition
    {

		private void Initialize()
		{
			AddSupportedType(DeclarationType.Table, GetTableScripts, "Table");
			AddSupportedType(DeclarationType.View, GetViewScripts, "View");
			AddSupportedType(DeclarationType.StoredProcedure, GetStoredProcedureScripts, "Procedure");
		}
		/// <summary>
		/// Script a Table using SMO
		/// </summary>
		/// <param name="objectName">Table name</param>
		/// <param name="schemaName">Schema name</param>
		/// <returns>String collection of scripts</returns>
		internal StringCollection GetTableScripts(string objectName, string schemaName)
		{
			return (schemaName != null) ? Database?.Tables[objectName, schemaName]?.Script(): Database?.Tables[objectName]?.Script();
		}
		/// <summary>
		/// Script a View using SMO
		/// </summary>
		/// <param name="objectName">View name</param>
		/// <param name="schemaName">Schema name</param>
		/// <returns>String collection of scripts</returns>
		internal StringCollection GetViewScripts(string objectName, string schemaName)
		{
			return (schemaName != null) ? Database?.Views[objectName, schemaName]?.Script(): Database?.Views[objectName]?.Script();
		}
		/// <summary>
		/// Script a StoredProcedure using SMO
		/// </summary>
		/// <param name="objectName">StoredProcedure name</param>
		/// <param name="schemaName">Schema name</param>
		/// <returns>String collection of scripts</returns>
		internal StringCollection GetStoredProcedureScripts(string objectName, string schemaName)
		{
			return (schemaName != null) ? Database?.StoredProcedures[objectName, schemaName]?.Script(): Database?.StoredProcedures[objectName]?.Script();
		}
	}
}
	