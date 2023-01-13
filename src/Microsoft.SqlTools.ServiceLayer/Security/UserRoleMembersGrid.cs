//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
//using System.Collections;
//using System.Collections.Specialized;
using Microsoft.SqlServer.Management.UI.Grid;


using Microsoft.SqlServer.Management.SqlManagerUI.UserData;

namespace Microsoft.SqlTools.ServiceLayer.Security
{
    /// <summary>
    /// Grid for displaying schema ownership in the Users dbCommander
    /// </summary>
    internal class UserRoleMembersGrid : UserGrid
	{
        private UserPrototypeNew prototype;    

		/// <summary>
		/// constructor
		/// </summary>
		public UserRoleMembersGrid(UserPrototypeNew prototype, string gridAccessibleName) :
			base(UserSR.HeaderRoleMembers, gridAccessibleName)
		{
			this.prototype = prototype;
		}

		/// <summary>
		/// populate the grid rows with database roles
		/// </summary>
		protected override void PopulateGridRows()
		{
            List<string> roles = this.prototype.DatabaseRoleNames;

			for (int roleIndex = 0; roleIndex < roles.Count; ++roleIndex)
			{
				string				roleName	= roles[roleIndex];
				bool				isMember	= this.prototype.IsRoleMember(roleName);
				GridCheckBoxState	state		= isMember ?  GridCheckBoxState.Checked : GridCheckBoxState.Unchecked; 
				
				this.AddGridRow(roleName, state);
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
			bool 				isMember	= (GridCheckBoxState.Unchecked == currentState); 
			GridCheckBoxState	newState	= isMember ? GridCheckBoxState.Checked : GridCheckBoxState.Unchecked;

			this.prototype.SetRoleMembership(name, isMember);
			this.SetGridCheckState(row, newState);	
		} 
	}
}








