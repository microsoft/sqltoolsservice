using Microsoft.SqlTools.ServiceLayer.Admin;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts;
using System.Data.SqlClient;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery
{
    public interface IBackupUtilities
    {
        void Initialize(CDataContainer dataContainer, SqlConnection sqlConnection);
        void SetBackupInput(BackupInfo input);
        void PerformBackup();
        BackupConfigInfo GetBackupConfigInfo(string databaseName);
        void CancelBackup();
    }
}
