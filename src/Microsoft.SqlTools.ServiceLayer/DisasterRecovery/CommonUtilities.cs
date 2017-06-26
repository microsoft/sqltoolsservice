//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Data.Tools.DataSets;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using SMO = Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Admin;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery
{
    /// <summary>
    /// Backup Type
    /// </summary>
    public enum BackupType
    {
        Full,
        Differential,
        TransactionLog
    }

    /// <summary>
    /// Backup component
    /// </summary>
    public enum BackupComponent
    {
        Database,
        Files
    }
    
    /// <summary>
    /// Backup set type
    /// </summary>
    public enum BackupsetType
    {
        BackupsetDatabase,            
        BackupsetLog,
        BackupsetDifferential,
        BackupsetFiles
    }

    /// <summary>
    /// Recovery option
    /// </summary>
    public enum RecoveryOption
    {
        Recovery,
        NoRecovery,
        StandBy
    }

    /// <summary>
    /// Restore item source
    /// </summary>
    public class RestoreItemSource
    {
        public string RestoreItemLocation { get; set; }
        public DeviceType RestoreItemDeviceType { get; set; }
        public bool IsLogicalDevice { get; set; }

        public override int GetHashCode()
        {
            if (string.IsNullOrEmpty(RestoreItemLocation))
            {
                return (RestoreItemDeviceType.ToString() + IsLogicalDevice.ToString()).GetHashCode();
            }
            else
            {
                return (RestoreItemLocation+RestoreItemDeviceType.ToString() + IsLogicalDevice.ToString()).GetHashCode();
            }
        }
    }

    /// <summary>
    /// Restore item
    /// </summary>
    public class RestoreItem
    {
        public BackupsetType ItemBackupsetType { get; set; }
        public int ItemPosition { get; set; }
        public ArrayList ItemSources { get; set; }
        public string ItemName { get; set; }
        public string ItemDescription { get; set; }
        public string ItemMediaName { get; set; }
    }

    /// <summary>
    /// Common methods used for backup and restore
    /// </summary>
    public class CommonUtilities
    {
        private CDataContainer dataContainer;
        private ServerConnection sqlConnection = null;
        private ArrayList excludedDatabases;
        private const string LocalSqlServer = "(local)";
        private const string LocalMachineName = ".";

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="dataContainer"></param>
        /// <param name="sqlConnection"></param>
        public CommonUtilities(CDataContainer dataContainer, ServerConnection sqlConnection)
        {           
            this.dataContainer = dataContainer;
            this.sqlConnection = sqlConnection;
            this.excludedDatabases = new ArrayList();
            this.excludedDatabases.Add("master");
            this.excludedDatabases.Add("tempdb");
        }
        
        public int GetServerVersion()
        {
            return this.dataContainer.Server.Information.Version.Major;            
        }

        /// <summary>
        /// Maps a string devicetype to the enum Smo.DeviceType
        /// <param name="stringDeviceType">Localized device type</param>
        /// </summary>
        public DeviceType GetDeviceType(string stringDeviceType)
        {
            if (String.Compare(stringDeviceType, RestoreConstants.File, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return DeviceType.File;
            }
            else if (String.Compare(stringDeviceType, RestoreConstants.Url, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return DeviceType.Url;
            }            
            else
            {
                return DeviceType.LogicalDevice;
            }
        }

        /// <summary>
        /// Maps a integer devicetype to the enum Smo.DeviceType
        /// <param name="numDeviceType">Device type</param>
        /// </summary>
        public DeviceType GetDeviceType(int numDeviceType)
        {
            if (numDeviceType == 1)
            {
                return DeviceType.File;
            }
            else if (numDeviceType == 3)
            {
                return DeviceType.Url;
            }
            else
            {
                return DeviceType.LogicalDevice;
            }
        }
        
        public BackupDeviceType GetPhisycalDeviceTypeOfLogicalDevice(string deviceName)
        {
            Enumerator  enumerator = new Enumerator();
            Request request = new Request();
            DataSet dataset = new DataSet();
            dataset.Locale = System.Globalization.CultureInfo.InvariantCulture;
            request.Urn = "Server/BackupDevice[@Name='" + Urn.EscapeString(deviceName) + "']";
            dataset = enumerator.Process(this.sqlConnection, request);

            if (dataset.Tables[0].Rows.Count > 0)
            {                    
                BackupDeviceType controllerType = (BackupDeviceType)(Convert.ToInt16(dataset.Tables[0].Rows[0]["BackupDeviceType"], System.Globalization.CultureInfo.InvariantCulture));
                return controllerType;
            }
            else
            {
                throw new Exception("Unexpected error");    
            }
        }
        
        public bool ServerHasTapes()
        {
            try
            {
                Enumerator en = new Enumerator();
                Request req = new Request();
                DataSet ds = new DataSet();
                ds.Locale = System.Globalization.CultureInfo.InvariantCulture;
                req.Urn = "Server/TapeDevice";
                ds = en.Process(this.sqlConnection, req);

                if (ds.Tables[0].Rows.Count > 0)
                {             
                    return true;
                }
                return false;
            }
            catch(Exception)
            {
                return false;
            }
        }

        public bool ServerHasLogicalDevices()
        {
            try
            {
                Enumerator en = new Enumerator();
                Request req = new Request();
                DataSet ds = new DataSet();
                ds.Locale = System.Globalization.CultureInfo.InvariantCulture;
                req.Urn = "Server/BackupDevice";
                ds = en.Process(this.sqlConnection,req);
                
                if (ds.Tables[0].Rows.Count > 0)
                {             
                   return true;
                }
                return false;
            }
            catch(Exception)
            {
                return false;
            }            
        }

        /// <summary>
        /// Sanitize file name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string SanitizeFileName(string name)
        {
            char[] result = name.ToCharArray();
            string illegalCharacters = "\\/:*?\"<>|";

            int resultLength    = result.GetLength(0);
            int illegalLength   = illegalCharacters.Length;

            for (int resultIndex = 0; resultIndex < resultLength; resultIndex++)
            {
                for (int illegalIndex = 0; illegalIndex < illegalLength; illegalIndex++)
                {
                    if (result[resultIndex] == illegalCharacters[illegalIndex])
                    {
                        result[resultIndex] = '_';
                    }
                }
            }
    
            return new string(result);
        }
        
        public RecoveryModel GetRecoveryModel(string databaseName)
        {
            Enumerator en = null;
            DataSet ds = new DataSet();
            ds.Locale = System.Globalization.CultureInfo.InvariantCulture;
            Request req = new Request();
            RecoveryModel recoveryModel = RecoveryModel.Simple;         

            en = new Enumerator();
            req.Urn = "Server/Database[@Name='" + Urn.EscapeString(databaseName) + "']/Option";
            req.Fields = new string[1];
            req.Fields[0] = "RecoveryModel";
            ds = en.Process(this.sqlConnection, req);

            if (ds.Tables[0].Rows.Count > 0)
            {   
                recoveryModel = (RecoveryModel)(ds.Tables[0].Rows[0]["RecoveryModel"]);         
            }                               
            return recoveryModel;
        }

        public string GetRecoveryModelAsString(RecoveryModel recoveryModel)
        {
            string recoveryModelString = string.Empty;

            if (recoveryModel == RecoveryModel.Full)
            {
                recoveryModelString = BackupConstants.RecoveryModelFull;
            }
            else if (recoveryModel == RecoveryModel.Simple)
            {
                recoveryModelString = BackupConstants.RecoveryModelSimple;
            }
            else if (recoveryModel == RecoveryModel.BulkLogged)
            {
                recoveryModelString = BackupConstants.RecoveryModelBulk;
            }
             
            return recoveryModelString;
        }

        public string GetDefaultBackupFolder()
        {
            string backupFolder = "";

            Enumerator en = null;
            DataSet ds = new DataSet();
            ds.Locale = System.Globalization.CultureInfo.InvariantCulture;
            Request req = new Request();
            en = new Enumerator();
            req.Urn = "Server/Setting";
            ds = en.Process(this.sqlConnection, req);

            if (ds.Tables[0].Rows.Count > 0)
            {
                backupFolder = Convert.ToString(ds.Tables[0].Rows[0]["BackupDirectory"], System.Globalization.CultureInfo.InvariantCulture);
            }
            return backupFolder;
        }

        public int GetMediaRetentionValue()
        {
            int afterDays = 0;
            try
            {
                Enumerator en = new Enumerator();
                Request req = new Request();
                DataSet ds = new DataSet();
                ds.Locale = System.Globalization.CultureInfo.InvariantCulture;

                req.Urn = "Server/Configuration";
                ds = en.Process(this.sqlConnection, req);               
                for (int i = 0 ; i < ds.Tables[0].Rows.Count; i++)
                {
                    if (Convert.ToString(ds.Tables[0].Rows[i]["Name"], System.Globalization.CultureInfo.InvariantCulture) == "media retention")
                    {
                        afterDays = Convert.ToInt32(ds.Tables[0].Rows[i]["RunValue"], System.Globalization.CultureInfo.InvariantCulture);
                        break;
                    }
                }                                                                               
                return afterDays;
            }
            catch (Exception)
            {   
                return afterDays;
            }           
        }

        public bool IsDestinationPathValid(string path, ref bool isFolder)
        {
            Enumerator en = null;
            DataTable dt;
            Request req = new Request();

            en = new Enumerator();
            req.Urn = "Server/File[@FullName='" + Urn.EscapeString(path) + "']";
            dt = en.Process(this.sqlConnection, req);
            
            if (dt.Rows.Count > 0)
            {
                isFolder = !(Convert.ToBoolean(dt.Rows[0]["IsFile"], System.Globalization.CultureInfo.InvariantCulture));
                return true;
            }
            else
            {
                isFolder = false;
                return false;
            }
        }

        public string GetMediaNameFromBackupSetId(int backupSetId)
        {
            Enumerator en  = null;
            DataSet ds  = new DataSet();
            ds.Locale = System.Globalization.CultureInfo.InvariantCulture;
            Request req = new Request();    

            int mediaId = -1;
            string mediaName = string.Empty;
            en = new Enumerator();
            req.Urn = "Server/BackupSet[@ID='" + Urn.EscapeString(Convert.ToString(backupSetId, System.Globalization.CultureInfo.InvariantCulture)) + "']";

            try
            {
                ds = en.Process(this.sqlConnection, req);
                if (ds.Tables[0].Rows.Count > 0)
                {
                    mediaId = Convert.ToInt32(ds.Tables[0].Rows[0]["MediaSetId"], System.Globalization.CultureInfo.InvariantCulture);
                    ds.Clear();
                    req.Urn = "Server/BackupMediaSet[@ID='" + Urn.EscapeString(Convert.ToString(mediaId, System.Globalization.CultureInfo.InvariantCulture)) + "']";
                    ds = en.Process(this.sqlConnection, req);

                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        mediaName = Convert.ToString(ds.Tables[0].Rows[0]["Name"], System.Globalization.CultureInfo.InvariantCulture);
                    }
                }
            }
            /// LPU doesn't have rights to enumerate in msdb.backupset
            catch (Exception)
            {
            }
            return mediaName;                        
        }

        public string GetFileType(string type)
        {
            string result = string.Empty;
            switch (type.ToUpperInvariant())
            {
                case "D":
                    result = RestoreConstants.Data;
                    break;
                case "S":
                    result = RestoreConstants.FileStream;
                    break;
                case "L":
                    result = RestoreConstants.Log;
                    break;
                case "F":
                    result = RestoreConstants.FullText;
                    break;
                default:
                    result = RestoreConstants.NotKnown;
                    break;
            }

            return result;
        }
        
        public string GetNewPhysicalRestoredFileName(string filePathParam, string dbName, bool isNewDatabase, string type, ref int fileIndex)
        {
            if (string.IsNullOrEmpty(filePathParam))
            {
                return string.Empty;
            }
            
            string result = string.Empty;
            string filePath = filePathParam;
            int idx = filePath.LastIndexOf('\\');
            string folderPath = filePath.Substring(0,idx);
            
            string fileName = filePath.Substring(idx + 1);
            idx = fileName.LastIndexOf('.');
            string fileExtension = fileName.Substring(idx + 1);

            bool isFolder = true;
            bool isValidPath = IsDestinationPathValid(folderPath, ref isFolder);

            if (!isValidPath || !isFolder)
            {
                SMO.Server server = new SMO.Server(this.sqlConnection);
                if (type != RestoreConstants.Log)
                {
                    folderPath = server.Settings.DefaultFile;
                    if (folderPath.Length == 0)
                    {
                        folderPath = server.Information.MasterDBPath;
                    }
                }
                else 
                {
                    folderPath = server.Settings.DefaultLog;
                    if (folderPath.Length == 0)
                    {
                        folderPath = server.Information.MasterDBLogPath;
                    }
                }
            }
            else
            {
                if (!isNewDatabase)
                {
                    return filePathParam;
                }
            }

            if (!isNewDatabase)
            {
                result = folderPath + "\\" + dbName + "." + fileExtension;
            }
            else
            {
                if (0 != string.Compare(fileExtension, "mdf", StringComparison.OrdinalIgnoreCase))
                {
                    result = folderPath + "\\" + dbName + "_" + Convert.ToString(fileIndex, System.Globalization.CultureInfo.InvariantCulture) + "." + fileExtension;
                    fileIndex++;
                }
                else
                {
                    result = folderPath + "\\" + dbName + "." + fileExtension;
                }
            }

            return result;
        }

        // TODO: This is implemented as internal property in SMO. 
        public bool IsLocalPrimaryReplica(string databaseName)
        {
            /*
            SMO.Server server = new SMO.Server(this.SqlConnection);
            return server.Databases[databaseName].IsLocalPrimaryReplica();*/
            return false;
        }

        public bool IsHADRDatabase(string databaseName)
        {
            SMO.Server server = new SMO.Server(this.sqlConnection);
            if ((this.sqlConnection.ServerVersion.Major < 11) || (!server.IsHadrEnabled))
            {
                return false;
            }
            return !string.IsNullOrEmpty(server.Databases[databaseName].AvailabilityGroupName);
        }
        
        /// <summary>
        /// Returns whether mirroring is enabled on a database or not
        /// </summary>>
        public bool IsMirroringEnabled(string databaseName)
        {
            SMO.Server server = new SMO.Server(this.sqlConnection);
            if (this.sqlConnection.ServerVersion.Major < 9)
            {
                return false;
            }

            Database db = server.Databases[databaseName];
            if (db == null || db.DatabaseEngineType != DatabaseEngineType.Standalone)
            {
                return false;
            }

            return db.IsMirroringEnabled;
        }

        public bool IsBackupTypeSupportedInReplica(string hadrDb, bool backupFileAndFileGroup, string backupTypeStr, bool copyOnly)
        {
            //Method should be called for HADR db
            System.Diagnostics.Debug.Assert(IsHADRDatabase(hadrDb));

            bool result = true;
            bool localPrimaryReplica = this.IsLocalPrimaryReplica(hadrDb);

            if (localPrimaryReplica)
            {
                return result;
            }

            //Set un-supported backuptype to false
            if (0 == string.Compare(backupTypeStr, BackupConstants.BackupTypeFull, StringComparison.OrdinalIgnoreCase))
            {
                if (!copyOnly)
                {
                    //Full
                    result = false;
                }
            }
            else if (0 == string.Compare(backupTypeStr, BackupConstants.BackupTypeDiff, StringComparison.OrdinalIgnoreCase))
            {
                //Diff
                result = false;
            }
            else if (0 == string.Compare(backupTypeStr, BackupConstants.BackupTypeTLog, StringComparison.OrdinalIgnoreCase))
            {
                //Log-CopyOnly
                if (copyOnly)
                {
                    result = false;
                }
            }

            return result;
        }
                
        public bool IsDatabaseOnServer(string databaseName)
        {
            Enumerator en = new Enumerator();
            DataSet ds = new DataSet();
            ds.Locale = System.Globalization.CultureInfo.InvariantCulture;
            Request req = new Request();                                        
            
            req.Urn = "Server/Database[@Name='" + Urn.EscapeString(databaseName) + "']";
            req.Fields = new string[1];
            req.Fields[0] = "Name";

            ds = en.Process(sqlConnection, req);            
            return (ds.Tables[0].Rows.Count > 0) ? true : false;
        }

        /* TODO: This is needed for Restore
        /// <summary>
        /// 
        /// </summary>
        /// <param name="excludeSystemDatabase"></param>
        /// <returns>Dictionary of Database names</returns>
        public Dictionary<string, string> EnumerateDatabasesOnServer(bool excludeSystemDatabase)
        {
            ServerComparer serverComparer = new ServerComparer(SqlConnection);
            DatabaseNameEqualityComparer DbNameComparer = new DatabaseNameEqualityComparer(serverComparer);
            Dictionary<string, string> dictDBList = new Dictionary<string, string>(DbNameComparer);
            
            Enumerator          en  = new Enumerator();
            DataSet             ds  = new DataSet();
            ds.Locale = System.Globalization.CultureInfo.InvariantCulture;
            Request             req = new Request();                                        
            
            req.Urn             = "Server/Database";
            req.Fields          = new string[1];
            req.Fields[0]       = "Name";

            req.OrderByList = new OrderBy[1];
            req.OrderByList[0] = new OrderBy();            
            req.OrderByList[0].Field = "Name";
            req.OrderByList[0].Dir = OrderBy.Direction.Asc;

            ds = en.Process(SqlConnection, req);

            int iCount  = ds.Tables[0].Rows.Count;

            for(int i = 0; i < iCount; i++)
            {
                string DbName = Convert.ToString(ds.Tables[0].Rows[i]["Name"], System.Globalization.CultureInfo.InvariantCulture);

                if (!excludeSystemDatabase || !this.ExcludedDbs.Contains(DbName))
                {
                    dictDBList.Add(DbName, DbName);
                }                
            }
            return dictDBList;
        }
        */

        /* TODO: This is needed for Restore
        public Dictionary<string, string> EnumerateDatabasesWithBackups()
        {
            Enumerator en = new Enumerator();
            DataSet ds = new DataSet();
            ds.Locale = System.Globalization.CultureInfo.InvariantCulture;
            Request req = new Request();

            req.Urn = "Server/BackupSet";
            req.Fields = new string[1];
            req.Fields[0] = "DatabaseName";
            req.OrderByList = new OrderBy[1];
            req.OrderByList[0] = new OrderBy();
            req.OrderByList[0].Field = "DatabaseName";
            req.OrderByList[0].Dir = OrderBy.Direction.Asc;

            ServerComparer serverComparer = new ServerComparer(SqlConnection);
            DatabaseNameEqualityComparer DbNameComparer = new DatabaseNameEqualityComparer(serverComparer);
            Dictionary<string, string> dict = new Dictionary<string, string>(DbNameComparer);

            ds = en.Process(SqlConnection, req);

            foreach (DataRow row in ds.Tables[0].Rows)
            {
                string dbName = Convert.ToString(row["DatabaseName"], System.Globalization.CultureInfo.InvariantCulture);
                if (!this.ExcludedDbs.Contains(dbName))
                {
                    //A database may contain multiple backupsets with different name. Backupset name are always case sensitive
                    //Removing duplicates for a database
                    if (!dict.ContainsKey(dbName))
                    {
                        dict.Add(dbName, dbName);
                    }
                }
            }
            return dict;
        }*/

        public void GetBackupSetTypeAndComponent(int numType, ref string backupType, ref string backupComponent)
        {
            switch (numType)
            {
                case 1:
                    backupType = RestoreConstants.TypeFull;
                    backupComponent = RestoreConstants.ComponentDatabase;
                    break;
                case 2:
                    backupType = RestoreConstants.TypeTransactionLog;
                    backupComponent = "";
                    break;                
                case 4:
                    backupType = RestoreConstants.TypeFilegroup;
                    backupComponent = RestoreConstants.ComponentFile;
                    break;
                case 5:
                    backupType = RestoreConstants.TypeDifferential;
                    backupComponent = RestoreConstants.ComponentDatabase;
                    break;
                case 6:
                    backupType = RestoreConstants.TypeFilegroupDifferential;
                    backupComponent = RestoreConstants.ComponentFile;
                    break;
                default:
                    backupType = RestoreConstants.NotKnown;
                    backupComponent = RestoreConstants.NotKnown;
                    break;
            }
        }
        
        public void GetBackupSetTypeAndComponent(string strType, ref string backupType, ref string backupComponent)
        {           
            string type = strType.ToUpperInvariant();

            if (type == "D")
            {
                backupType = RestoreConstants.TypeFull;
                backupComponent = RestoreConstants.ComponentDatabase;
            }
            else
            {
                if (type == "I")
                {
                    backupType = RestoreConstants.TypeDifferential;
                    backupComponent = RestoreConstants.ComponentDatabase;
                }
                else
                {
                    if (type == "L")
                    {
                        backupType = RestoreConstants.TypeTransactionLog;
                        backupComponent = "";
                    }
                    else
                    {
                        if (type == "F")
                        {
                            backupType = RestoreConstants.TypeFilegroup;
                            backupComponent = RestoreConstants.ComponentFile;
                        }
                        else
                        {
                            if (type == "G")
                            {
                                backupType = RestoreConstants.TypeFilegroupDifferential;
                                backupComponent = RestoreConstants.ComponentFile;
                            }
                            else
                            {
                                backupType = RestoreConstants.NotKnown;
                                backupComponent = RestoreConstants.NotKnown;
                            }
                        }
                    }
                }
            }                       
        }

        public void GetFileType(string backupType, string tempFileType, ref string fileType)
        {
            string bkType = backupType.ToUpperInvariant();
            string type = tempFileType.ToUpperInvariant();

            if (bkType == "D" || bkType == "I" || bkType == "F" || bkType == "G")
            {
                switch (type)
                {
                    case "D": fileType = RestoreConstants.Data;
                        break;
                    case "S": fileType = RestoreConstants.FileStream;
                        break;
                    default: fileType = RestoreConstants.NotKnown;
                        break;
                }
            }
        }
        
        public BackupsetType GetBackupsetTypeFromBackupTypesOnDevice(int type)
        {            
            BackupsetType Result = BackupsetType.BackupsetDatabase;
            switch(type)
            {
                case 1:
                    Result = BackupsetType.BackupsetDatabase;
                    break;
                case 2:
                    Result = BackupsetType.BackupsetLog;
                    break;
                case 5:
                    Result = BackupsetType.BackupsetDifferential;
                    break;
                case 4:
                    Result = BackupsetType.BackupsetFiles;
                    break;
                default:
                    Result = BackupsetType.BackupsetDatabase;
                    break;
            }
            return Result;
        }

        
        public BackupsetType GetBackupsetTypeFromBackupTypesOnHistory(string type)
        {
            BackupsetType result = BackupsetType.BackupsetDatabase;
            switch(type)
            {
                case "D":
                    result = BackupsetType.BackupsetDatabase;
                    break;
                case "I":
                    result = BackupsetType.BackupsetDifferential;
                    break;
                case "L":
                    result = BackupsetType.BackupsetLog;
                    break;
                case "F":
                    result = BackupsetType.BackupsetFiles;
                    break;
                default:
                    result = BackupsetType.BackupsetDatabase;
                    break;
            }
            return result;
        }
        
        public DataSet GetBackupSetFiles(int backupsetId)
        {
            Enumerator en = new Enumerator();
            Request req = new Request();
            DataSet backupsetfiles = new DataSet();
            backupsetfiles.Locale = System.Globalization.CultureInfo.InvariantCulture;

            if(backupsetId > 0)
            {
                req.Urn = "Server/BackupSet[@ID='" + Urn.EscapeString(Convert.ToString(backupsetId, System.Globalization.CultureInfo.InvariantCulture)) + "']/File";
            }
            else
            {
                req.Urn = "Server/BackupSet/File";
            }
            backupsetfiles = en.Process(sqlConnection, req);

            return backupsetfiles;
        }
        
        public DataSet GetBackupSetById(int backupsetId)
        {   
            SqlExecutionModes executionMode = this.sqlConnection.SqlExecutionModes;
            this.sqlConnection.SqlExecutionModes = SqlExecutionModes.ExecuteSql;
            Enumerator en = new Enumerator();
            Request req = new Request();
            DataSet backupset = new DataSet();
            backupset.Locale = System.Globalization.CultureInfo.InvariantCulture;

            req.Urn = "Server/BackupSet[@ID='" + Urn.EscapeString(Convert.ToString(backupsetId, System.Globalization.CultureInfo.InvariantCulture)) + "']";
            backupset = en.Process(this.sqlConnection, req);

            this.sqlConnection.SqlExecutionModes = executionMode;
            return backupset;
        }
        
        public ArrayList GetBackupSetPhysicalSources(int backupsetId)
        {            
            SqlExecutionModes executionMode = this.sqlConnection.SqlExecutionModes;
            this.sqlConnection.SqlExecutionModes = SqlExecutionModes.ExecuteSql;

            ArrayList sources = new ArrayList();
            DataSet backupSet = GetBackupSetById(backupsetId);
            if(backupSet.Tables[0].Rows.Count == 1)
            {
                string mediaSetID = Convert.ToString(backupSet.Tables[0].Rows[0]["MediaSetId"], System.Globalization.CultureInfo.InvariantCulture);

                Enumerator en = new Enumerator();
                Request req = new Request();
                DataSet mediafamily = new DataSet();
                mediafamily.Locale = System.Globalization.CultureInfo.InvariantCulture;            

                req.Urn = "Server/BackupMediaSet[@ID='"+Urn.EscapeString(mediaSetID)+"']/MediaFamily";
                mediafamily = en.Process(this.sqlConnection, req);

                if (mediafamily.Tables[0].Rows.Count > 0)
                {
                    for (int j = 0 ; j < mediafamily.Tables[0].Rows.Count; j ++)
                    {
                        RestoreItemSource itemSource = new RestoreItemSource();
                        itemSource.RestoreItemLocation = Convert.ToString(mediafamily.Tables[0].Rows[j]["PhysicalDeviceName"], System.Globalization.CultureInfo.InvariantCulture);
                        BackupDeviceType backupDeviceType = (BackupDeviceType)Enum.Parse(typeof(BackupDeviceType), mediafamily.Tables[0].Rows[j]["BackupDeviceType"].ToString());
                        
                        if (BackupDeviceType.Disk == backupDeviceType)
                        {
                            itemSource.RestoreItemDeviceType = DeviceType.File;                                    
                        }
                        else if (BackupDeviceType.Url == backupDeviceType)
                        {
                            itemSource.RestoreItemDeviceType = DeviceType.Url;
                        }                                
                        else
                        {
                            itemSource.RestoreItemDeviceType = DeviceType.Tape;                                    
                        }                                
                        sources.Add(itemSource);                        
                    }
                }
            }

            this.sqlConnection.SqlExecutionModes = executionMode;
            return sources;
        }
        
        public RestoreActionType GetRestoreTaskFromBackupSetType(BackupsetType type)
        {
            RestoreActionType result = RestoreActionType.Database;
            
            switch (type)
            {
                case BackupsetType.BackupsetDatabase:
                    result = RestoreActionType.Database;
                    break;
                case BackupsetType.BackupsetDifferential:
                    result = RestoreActionType.Database;
                    break;
                case BackupsetType.BackupsetLog:
                    result = RestoreActionType.Log;
                    break;
                case BackupsetType.BackupsetFiles:
                    result = RestoreActionType.Files;
                    break;
                default:
                    result = RestoreActionType.Database;
                    break;
            }
            return result;
        }
        
        public int GetLatestBackup(string databaseName, string backupSetName)
        {
            Enumerator en = new Enumerator();
            Request req = new Request();
            DataSet backupSets = new DataSet();
            backupSets.Locale = System.Globalization.CultureInfo.InvariantCulture;            
            OrderBy orderByBackupDate;

            req.Urn = "Server/BackupSet[@Name='"+Urn.EscapeString(backupSetName)+"' and @DatabaseName='"+ Urn.EscapeString(databaseName)+"']";
            req.OrderByList = new OrderBy[1];
            orderByBackupDate = new OrderBy("BackupFinishDate", OrderBy.Direction.Desc);
            req.OrderByList[0] = orderByBackupDate;
            backupSets = en.Process(this.sqlConnection, req);

            if (backupSets.Tables[0].Rows.Count > 0)
            {
                return Convert.ToInt32(backupSets.Tables[0].Rows[0]["Position"], System.Globalization.CultureInfo.InvariantCulture);
            }
            else
            {
                return -1;
            }
        }
        
        public List<RestoreItemSource> GetLatestBackupLocations(string databaseName)
        {
            List<RestoreItemSource> latestLocations = new List<RestoreItemSource>();
            Enumerator en = null;
            DataSet ds = new DataSet();
            ds.Locale = System.Globalization.CultureInfo.InvariantCulture;
            Request req = new Request();
            en = new Enumerator();

            req.Urn = "Server/BackupSet[@DatabaseName='" + Urn.EscapeString(databaseName) + "']";
            req.OrderByList = new OrderBy[1];
            req.OrderByList[0] = new OrderBy();
            req.OrderByList[0].Field = "BackupFinishDate";
            req.OrderByList[0].Dir = OrderBy.Direction.Desc;

            try
            {
                ds = en.Process(this.sqlConnection, req);
                if (ds.Tables[0].Rows.Count > 0)
                {
                    string mediaSetID = Convert.ToString(ds.Tables[0].Rows[0]["MediaSetId"], System.Globalization.CultureInfo.InvariantCulture);
                    ds.Clear();
                    req = new Request();
                    req.Urn = "Server/BackupMediaSet[@ID='" + Urn.EscapeString(mediaSetID) + "']/MediaFamily";
                    ds = en.Process(this.sqlConnection, req);
                    int count = ds.Tables[0].Rows.Count;
                    if (count > 0)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            RestoreItemSource restoreItemSource = new RestoreItemSource();
                            DeviceType deviceType = (DeviceType)(Convert.ToInt16(ds.Tables[0].Rows[i]["BackupDeviceType"], System.Globalization.CultureInfo.InvariantCulture));
                            string location = Convert.ToString(ds.Tables[0].Rows[i]["LogicalDeviceName"], System.Globalization.CultureInfo.InvariantCulture);
                            bool isLogical = (location.Length > 0);
                            if (false == isLogical)
                            {
                                location = Convert.ToString(ds.Tables[0].Rows[i]["PhysicalDeviceName"], System.Globalization.CultureInfo.InvariantCulture);
                            }
                            else
                            {
                                // We might receive the logical name as "logicaldevicename(physicalpath)"
                                // We try to get the device name out of it
                                int pos = location.IndexOf('(');
                                if (pos > 0)
                                {
                                    location = location.Substring(0, pos);
                                }
                            }
                            restoreItemSource.RestoreItemDeviceType = deviceType;
                            restoreItemSource.RestoreItemLocation = location;
                            restoreItemSource.IsLogicalDevice = isLogical;
                            latestLocations.Add(restoreItemSource);
                        }
                    }
                }
            }
            /// LPU doesn't have rights to enumerate msdb.backupset
            catch (Exception)
            {                
            }            
            return latestLocations;            
        }                       
        
        public string GetDefaultDatabaseForLogin(string loginName)
        {   
            string defaultDatabase  = string.Empty;
            Enumerator en = new Enumerator();
            DataSet ds = new DataSet();
            ds.Locale = System.Globalization.CultureInfo.InvariantCulture;
            Request req = new Request();                                        
            
            req.Urn = "Server/Login[@Name='"+Urn.EscapeString(loginName)+"']";
            req.Fields = new string[1];
            req.Fields[0] = "DefaultDatabase";
            ds = en.Process(this.sqlConnection, req);

            if (ds.Tables[0].Rows.Count > 0)
            {
                defaultDatabase = Convert.ToString(ds.Tables[0].Rows[0]["DefaultDatabase"], System.Globalization.CultureInfo.InvariantCulture);
            }
            return defaultDatabase;
        }

        public bool IsPathExisting(string path, ref bool isFolder)
        {
            Enumerator en = new Enumerator();
            DataSet ds = new DataSet();
            ds.Locale = System.Globalization.CultureInfo.InvariantCulture;
            Request req = new Request();
            req.Urn = "Server/File[@FullName='" + Urn.EscapeString(path) + "']";
            ds = en.Process(this.sqlConnection, req);

            if (ds.Tables[0].Rows.Count > 0)
            {
                isFolder = !(Convert.ToBoolean(ds.Tables[0].Rows[0]["IsFile"], System.Globalization.CultureInfo.InvariantCulture));
                return true;
            }
            else
            {
                isFolder = false;
                return false;
            }
        }

        public ArrayList IsPhysicalPathInLogicalDevice(string physicalPath)
        {   
            Enumerator en = new Enumerator();
            DataSet ds = new DataSet();
            ds.Locale = System.Globalization.CultureInfo.InvariantCulture;
            Request req = new Request();
            ArrayList result = null;
            int count = 0;               
            req.Urn = "Server/BackupDevice[@PhysicalLocation='" +Urn.EscapeString(physicalPath)+ "']";

            ds = en.Process(this.sqlConnection, req);           
            count = ds.Tables[0].Rows.Count;
            
            if (count > 0)
            {
                result = new ArrayList(count);
                for(int i = 0; i < count; i++)
                {
                    result.Add(Convert.ToString(ds.Tables[0].Rows[0]["Name"], System.Globalization.CultureInfo.InvariantCulture));
                }
            }               
            return result;
        }

        public string GetMachineName(string sqlServerName)
        {
            System.Diagnostics.Debug.Assert(sqlServerName != null);

            // special case (local) which is accepted SQL(MDAC) but by OS
            if ((sqlServerName.ToLowerInvariant().Trim() == LocalSqlServer) ||
                (sqlServerName.ToLowerInvariant().Trim() == LocalMachineName))
            {
                return System.Environment.MachineName;
            }

            string machineName = sqlServerName;         
            if (sqlServerName.Trim().Length != 0)
            {
                // [0] = machine, [1] = instance
                return sqlServerName.Split('\\')[0];
            }
            else
            {
                // we have default instance of default machine
                return machineName;
            }
        }
    }
}