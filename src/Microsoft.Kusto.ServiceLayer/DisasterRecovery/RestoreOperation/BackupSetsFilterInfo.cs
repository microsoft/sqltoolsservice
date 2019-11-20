//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.Kusto.ServiceLayer.DisasterRecovery.RestoreOperation
{
    /// <summary>
    /// Class include info about selected back sets
    /// </summary>
    public class BackupSetsFilterInfo
    {
        private HashSet<Guid> selectedBackupSets = new HashSet<Guid>();

        /// <summary>
        /// Returns true if given backup set is selected
        /// </summary>
        public bool IsBackupSetSelected(Guid backupGuid)
        {
            bool isSelected = false;
            if (backupGuid != Guid.Empty)
            {
                isSelected = this.selectedBackupSets.Any(x => x == backupGuid);
            }
            return isSelected;
        }

        /// <summary>
        /// Returns true if given backup set is selected
        /// </summary>
        public bool IsBackupSetSelected(BackupSet backupSet)
        {
            return IsBackupSetSelected(backupSet != null ? backupSet.BackupSetGuid : Guid.Empty);
        }

        /// <summary>
        /// Returns true if any backup set is selected
        /// </summary>
        public bool AnySelected
        {
            get
            {
                return this.selectedBackupSets != null && this.selectedBackupSets.Any();
            }
        }

        /// <summary>
        /// Adds backup set to selected list if not added aleady 
        /// </summary>
        /// <param name="backupSet"></param>
        public void Add(BackupSet backupSet)
        {
            if (backupSet != null)
            {
                if (!this.selectedBackupSets.Contains(backupSet.BackupSetGuid))
                {
                    this.selectedBackupSets.Add(backupSet.BackupSetGuid);
                }
            }
        }

        /// <summary>
        /// Clears the list
        /// </summary>
        public void Clear()
        {
            this.selectedBackupSets.Clear();
        }
    }
}
