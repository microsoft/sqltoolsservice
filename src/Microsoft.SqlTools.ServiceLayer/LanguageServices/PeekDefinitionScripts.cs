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
using Microsoft.SqlTools.ServiceLayer.Utility;
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
			try
			{
				Table smoObject = string.IsNullOrEmpty(schemaName) ? new Table(this.Database, objectName) : new Table(this.Database, objectName, schemaName); 
				smoObject.Refresh();
				return smoObject.Script();
			}
			catch (Exception ex)
			{
				Logger.Write(LogLevel.Error,"Exception at PeekDefinition GetTableScripts : " + ex.Message));
				return null;
			}
		}

		/// <summary>
		/// Script a View using SMO
		/// </summary>
		/// <param name="objectName">View name</param>
		/// <param name="schemaName">Schema name</param>
		/// <returns>String collection of scripts</returns>
		internal StringCollection GetViewScripts(string objectName, string schemaName)
		{
			try
			{
				View smoObject = string.IsNullOrEmpty(schemaName) ? new View(this.Database, objectName) : new View(this.Database, objectName, schemaName); 
				smoObject.Refresh();
				return smoObject.Script();
			}
			catch (Exception ex)
			{
				Logger.Write(LogLevel.Error,"Exception at PeekDefinition GetViewScripts : " + ex.Message));
				return null;
			}
		}

		/// <summary>
		/// Script a StoredProcedure using SMO
		/// </summary>
		/// <param name="objectName">StoredProcedure name</param>
		/// <param name="schemaName">Schema name</param>
		/// <returns>String collection of scripts</returns>
		internal StringCollection GetStoredProcedureScripts(string objectName, string schemaName)
		{
			try
			{
				StoredProcedure smoObject = string.IsNullOrEmpty(schemaName) ? new StoredProcedure(this.Database, objectName) : new StoredProcedure(this.Database, objectName, schemaName); 
				smoObject.Refresh();
				return smoObject.Script();
			}
			catch (Exception ex)
			{
				Logger.Write(LogLevel.Error,"Exception at PeekDefinition GetStoredProcedureScripts : " + ex.Message));
				return null;
			}
		}

	}
}
	