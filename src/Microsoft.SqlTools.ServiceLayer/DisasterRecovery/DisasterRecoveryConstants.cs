using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery
{

    public static class BackupConstants
    {
        public static string Backup = "Backup";

        public static string RecoveryModelFull = "Full";
        public static string RecoveryModelSimple = "Simple";
        public static string RecoveryModelBulk = "BulkLogged";

        public static string BackupTypeFull = "Full";
        public static string BackupTypeDiff = "Differential";
        public static string BackupTypeTLog = "Transaction Log";

        public static string Database = "Database";
        public static string FileFilegroups = "File and Filegroups";
        public static string Filegroup = "Filegroup";
        public static string File = "File";        
    }

    public static class RestoreConstants
    {
        public static string File = "File";
        public static string Url = "URL";

        public static string Data = "Rows Data";
        public static string FileStream = "FILESTREAM Data";
        public static string Log = "Log";
        public static string FullText = "Full Text";
        public static string NotKnown = "Not known";

        public static string TypeFull = "Full";
        public static string TypeTransactionLog = "Transaction Log";
        public static string TypeDifferential = "Differential";
        public static string TypeFilegroup = "Filegroup";
        public static string TypeFilegroupDifferential = "Filegroup Differential";
        public static string ComponentDatabase = "Database";
        public static string ComponentFile = "File";

    }
}
