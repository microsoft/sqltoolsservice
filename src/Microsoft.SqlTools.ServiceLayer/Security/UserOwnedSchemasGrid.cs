//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;

using Microsoft.SqlServer.Management.UI.Grid;


using Microsoft.SqlServer.Management.SqlManagerUI.UserData;

namespace Microsoft.SqlTools.ServiceLayer.Security
{
    /// <summary>
    /// Grid for displaying schema ownership in the Users dbCommander
    /// </summary>
    internal class UserOwnedSchemasGrid : UserGrid
	{
        private UserPrototypeNew prototype;    

		/// <summary>
		/// constructor
		/// </summary>
		public UserOwnedSchemasGrid(UserPrototypeNew prototype, string gridAccessibleName) :
			base(UserSR.HeaderOwnedSchemas, gridAccessibleName)
		{
			this.prototype = prototype;
		}

		/// <summary>
		/// populate the grid rows with schemas
		/// </summary>
		protected override void PopulateGridRows()
		{
			if (this.prototype.IsYukonOrLater)
			{
				// If a schema is owned by the user, then it can't be "unowned" by unchecking the box -
				// another user has to take ownership instead.  To reflect this reality, unowned
				// schemas get an unchecked checkbox, while owned schemas get a disabled but checked
				// checkbox.

				List<string> schemaNames = this.prototype.SchemaNames;

				for (int schemaIndex = 0; schemaIndex < schemaNames.Count; ++schemaIndex)
				{
					string schemaName = schemaNames[schemaIndex];

					if ((0 != String.Compare(schemaName, "dbo", StringComparison.Ordinal))	&&
						(0 != String.Compare(schemaName, "sys", StringComparison.Ordinal))	&&
						(0 != String.Compare(schemaName, "INFORMATION_SCHEMA", StringComparison.Ordinal)))
					{
						bool				owned	= this.prototype.IsSchemaOwner(schemaName);
						GridCheckBoxState	state	= owned ? GridCheckBoxState.Indeterminate : GridCheckBoxState.Unchecked;

						this.AddGridRow(schemaName, state);
					}
				}
			}
		}

		/// <summary>
		/// Handle click events on the grid checkboxes.
		/// </summary>
		/// <param name="row">The index of the row that was clicked</param>
		/// <param name="name">The name of the schema</param>
		/// <param name="currentState">The state of the checkbox when it was clicked</param>
		protected override void HandleClickOnCheckbox(int row, string name, GridCheckBoxState currentState)
		{
			if (currentState != GridCheckBoxState.Indeterminate)
			{
				bool 				isOwned 	= (GridCheckBoxState.Unchecked == currentState); 
				GridCheckBoxState	newState	= isOwned ? GridCheckBoxState.Checked : GridCheckBoxState.Unchecked;

				this.prototype.SetSchemaOwner(name, isOwned);
				this.SetGridCheckState(row, newState);
			}
		} 
	}
}








