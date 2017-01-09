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
			AddSupportedType(DeclarationType.UserDefinedDataType, GetUserDefinedDataTypeScripts, "Type");
			AddSupportedType(DeclarationType.UserDefinedTableType, GetUserDefinedTableTypeScripts, "Type");
			AddSupportedType(DeclarationType.UserDefinedAggregate, GetUserDefinedAggregateScripts, "Aggregate");
			AddSupportedType(DeclarationType.Synonym, GetSynonymScripts, "Synonym");
			AddSupportedType(DeclarationType.DatabaseRole, GetDatabaseRoleScripts, "Role");
			AddSupportedType(DeclarationType.DatabaseDdlTrigger, GetDatabaseDdlTriggerScripts, "Trigger");
			AddSupportedType(DeclarationType.ScalarValuedFunction, GetScalarValuedFunctionScripts, "Function");
			AddSupportedType(DeclarationType.TableValuedFunction, GetTableValuedFunctionScripts, "Function");
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
				Logger.Write(LogLevel.Error,"Exception at PeekDefinition GetTableScripts : " + ex.Message);
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
				Logger.Write(LogLevel.Error,"Exception at PeekDefinition GetViewScripts : " + ex.Message);
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
				Logger.Write(LogLevel.Error,"Exception at PeekDefinition GetStoredProcedureScripts : " + ex.Message);
				return null;
			}
		}

		/// <summary>
		/// Script a UserDefinedDataType using SMO
		/// </summary>
		/// <param name="objectName">UserDefinedDataType name</param>
		/// <param name="schemaName">Schema name</param>
		/// <returns>String collection of scripts</returns>
		internal StringCollection GetUserDefinedDataTypeScripts(string objectName, string schemaName)
		{
			try
			{
				UserDefinedDataType smoObject = string.IsNullOrEmpty(schemaName) ? new UserDefinedDataType(this.Database, objectName) : new UserDefinedDataType(this.Database, objectName, schemaName);
				smoObject.Refresh();
				return smoObject.Script();
			}
			catch (Exception ex)
			{
				Logger.Write(LogLevel.Error,"Exception at PeekDefinition GetUserDefinedDataTypeScripts : " + ex.Message);
				return null;
			}
		}

		/// <summary>
		/// Script a UserDefinedTableType using SMO
		/// </summary>
		/// <param name="objectName">UserDefinedTableType name</param>
		/// <param name="schemaName">Schema name</param>
		/// <returns>String collection of scripts</returns>
		internal StringCollection GetUserDefinedTableTypeScripts(string objectName, string schemaName)
		{
			try
			{
				UserDefinedTableType smoObject = string.IsNullOrEmpty(schemaName) ? new UserDefinedTableType(this.Database, objectName) : new UserDefinedTableType(this.Database, objectName, schemaName);
				smoObject.Refresh();
				return smoObject.Script();
			}
			catch (Exception ex)
			{
				Logger.Write(LogLevel.Error,"Exception at PeekDefinition GetUserDefinedTableTypeScripts : " + ex.Message);
				return null;
			}
		}

		/// <summary>
		/// Script a UserDefinedAggregate using SMO
		/// </summary>
		/// <param name="objectName">UserDefinedAggregate name</param>
		/// <param name="schemaName">Schema name</param>
		/// <returns>String collection of scripts</returns>
		internal StringCollection GetUserDefinedAggregateScripts(string objectName, string schemaName)
		{
			try
			{
				UserDefinedAggregate smoObject = string.IsNullOrEmpty(schemaName) ? new UserDefinedAggregate(this.Database, objectName) : new UserDefinedAggregate(this.Database, objectName, schemaName);
				smoObject.Refresh();
				return smoObject.Script();
			}
			catch (Exception ex)
			{
				Logger.Write(LogLevel.Error,"Exception at PeekDefinition GetUserDefinedAggregateScripts : " + ex.Message);
				return null;
			}
		}

		/// <summary>
		/// Script a Synonym using SMO
		/// </summary>
		/// <param name="objectName">Synonym name</param>
		/// <param name="schemaName">Schema name</param>
		/// <returns>String collection of scripts</returns>
		internal StringCollection GetSynonymScripts(string objectName, string schemaName)
		{
			try
			{
				Synonym smoObject = string.IsNullOrEmpty(schemaName) ? new Synonym(this.Database, objectName) : new Synonym(this.Database, objectName, schemaName);
				smoObject.Refresh();
				return smoObject.Script();
			}
			catch (Exception ex)
			{
				Logger.Write(LogLevel.Error,"Exception at PeekDefinition GetSynonymScripts : " + ex.Message);
				return null;
			}
		}

		/// <summary>
		/// Script a DatabaseRole using SMO
		/// </summary>
		/// <param name="objectName">DatabaseRole name</param>
		/// <param name="schemaName">Schema name</param>
		/// <returns>String collection of scripts</returns>
		internal StringCollection GetDatabaseRoleScripts(string objectName, string schemaName)
		{
			try
			{
				DatabaseRole smoObject = new DatabaseRole(this.Database, objectName);
				smoObject.Refresh();
				return smoObject.Script();
			}
			catch (Exception ex)
			{
				Logger.Write(LogLevel.Error,"Exception at PeekDefinition GetDatabaseRoleScripts : " + ex.Message);
				return null;
			}
		}

		/// <summary>
		/// Script a DatabaseDdlTrigger using SMO
		/// </summary>
		/// <param name="objectName">DatabaseDdlTrigger name</param>
		/// <param name="schemaName">Schema name</param>
		/// <returns>String collection of scripts</returns>
		internal StringCollection GetDatabaseDdlTriggerScripts(string objectName, string schemaName)
		{
			try
			{
				DatabaseDdlTrigger smoObject = new DatabaseDdlTrigger(this.Database, objectName);
				smoObject.Refresh();
				return smoObject.Script();
			}
			catch (Exception ex)
			{
				Logger.Write(LogLevel.Error,"Exception at PeekDefinition GetDatabaseDdlTriggerScripts : " + ex.Message);
				return null;
			}
		}

		/// <summary>
		/// Script a ScalarValuedFunction using SMO
		/// </summary>
		/// <param name="objectName">ScalarValuedFunction name</param>
		/// <param name="schemaName">Schema name</param>
		/// <returns>String collection of scripts</returns>
		internal StringCollection GetScalarValuedFunctionScripts(string objectName, string schemaName)
		{
			try
			{
				UserDefinedFunction smoObject = string.IsNullOrEmpty(schemaName) ? new UserDefinedFunction(this.Database, objectName) : new UserDefinedFunction(this.Database, objectName, schemaName);
				smoObject.Refresh();
				return smoObject.Script();
			}
			catch (Exception ex)
			{
				Logger.Write(LogLevel.Error,"Exception at PeekDefinition GetScalarValuedFunctionScripts : " + ex.Message);
				return null;
			}
		}

		/// <summary>
		/// Script a TableValuedFunction using SMO
		/// </summary>
		/// <param name="objectName">TableValuedFunction name</param>
		/// <param name="schemaName">Schema name</param>
		/// <returns>String collection of scripts</returns>
		internal StringCollection GetTableValuedFunctionScripts(string objectName, string schemaName)
		{
			try
			{
				UserDefinedFunction smoObject = string.IsNullOrEmpty(schemaName) ? new UserDefinedFunction(this.Database, objectName) : new UserDefinedFunction(this.Database, objectName, schemaName);
				smoObject.Refresh();
				return smoObject.Script();
			}
			catch (Exception ex)
			{
				Logger.Write(LogLevel.Error,"Exception at PeekDefinition GetTableValuedFunctionScripts : " + ex.Message);
				return null;
			}
		}

	}
}
	