//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery.RestoreOperation
{
    public class BackupSetsFilterInfo
    {
        private HashSet<Guid> selectedBackupSets = new HashSet<Guid>();


        public bool IsBackupSetSelected(Guid backupGuid)
        {
            bool isSelected = false;
            if (backupGuid != Guid.Empty)
            {
                isSelected = this.selectedBackupSets.Any(x => x == backupGuid);
            }
            return isSelected;
        }

        public bool IsBackupSetSelected(BackupSet backupSet)
        {
            return IsBackupSetSelected(backupSet != null ? backupSet.BackupSetGuid : Guid.Empty);
        }

        public bool AnySelected
        {
            get
            {
                return this.selectedBackupSets != null && this.selectedBackupSets.Any();
            }
        }

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

        public void Clear()
        {
            this.selectedBackupSets.Clear();
        }
    }
}
