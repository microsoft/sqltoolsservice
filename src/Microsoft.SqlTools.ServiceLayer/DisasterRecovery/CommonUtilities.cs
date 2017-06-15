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
    public enum BackupType
    {
        Full,
        Differential,
        TransactionLog
    }

    public enum BackupComponent
    {
        Database,
        Files
    }


    public enum BackupsetType
    {
        BackupsetDatabase,            
        BackupsetLog,
        BackupsetDifferential,
        BackupsetFiles
    }

    public enum   RecoveryOption
    {
        Recovery,
        NoRecovery,
        StandBy
    }

    public class RestoreItemSource
    {
        public string       RestoreItemLocation;
        public DeviceType   RestoreItemDeviceType;        
        public bool         IsLogicalDevice;

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


    public class RestoreItem
    {
        public  BackupsetType   ItemBackupsetType;
        public  int             ItemPosition;
        public ArrayList        ItemSources;
        public string           ItemName;
        public string           ItemDescription;
        public string           ItemMediaName;
    }

    /// <summary>
    /// Common methods used for backup and restore
    /// </summary>
    public class CommonUtilities
    {
        private     CDataContainer DataContainer;
        private     ServerConnection SqlConnection = null;
        private ArrayList ExcludedDbs;
        
        public CommonUtilities(CDataContainer dataContainer, ServerConnection sqlConnection)
        {           
            DataContainer   = dataContainer;
            this.SqlConnection  = sqlConnection;

            ExcludedDbs = new ArrayList();
            ExcludedDbs.Add("master");
            ExcludedDbs.Add("tempdb");
        }

        
        public int GetServerVersion()
        {
            return DataContainer.Server.Information.Version.Major;            
        }

        /// <summary>
        /// Maps a string devicetype to the enum Smo.DeviceType
        /// <param name="stringDeviceType">Localized device type</param>
        /// </summary>
        public Microsoft.SqlServer.Management.Smo.DeviceType    GetDeviceType(string stringDeviceType)
        {
            if(String.Compare(stringDeviceType, RestoreConstants.File, StringComparison.OrdinalIgnoreCase) == 0)
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
        public Microsoft.SqlServer.Management.Smo.DeviceType    GetDeviceType(int numDeviceType)
        {
            if(numDeviceType == 1)
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
            Enumerator  en  = new Enumerator();
            Request     req = new Request();
            DataSet     ds  = new DataSet();
            ds.Locale = System.Globalization.CultureInfo.InvariantCulture;

            req.Urn         = "Server/BackupDevice[@Name='" + Urn.EscapeString(deviceName) + "']";
            ds = en.Process(SqlConnection, req);

            int iCount  = ds.Tables[0].Rows.Count;
            if(iCount > 0)
            {                    
                BackupDeviceType ControllerType = (BackupDeviceType)(Convert.ToInt16(ds.Tables[0].Rows[0]["BackupDeviceType"], System.Globalization.CultureInfo.InvariantCulture));
                
                return ControllerType;
            }
            else
            {
                throw new Exception("Unexpected error");    
            }
        }

        
        public bool     ServerHasTapes()
        {
            try
            {
                Enumerator  en  = new Enumerator();
                Request     req = new Request();
                DataSet     ds  = new DataSet();
                ds.Locale = System.Globalization.CultureInfo.InvariantCulture;

                req.Urn         = "Server/TapeDevice";

                ds = en.Process(SqlConnection, req);

                int iCount  = ds.Tables[0].Rows.Count;
                if(iCount > 0)
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


        public bool     ServerHasLogicalDevices()
        {
            try
            {
                Enumerator  en  = new Enumerator();
                Request     req = new Request();
                DataSet     ds  = new DataSet();
                ds.Locale = System.Globalization.CultureInfo.InvariantCulture;

                req.Urn         = "Server/BackupDevice";
                ds              = en.Process(SqlConnection,req);

                int iCount  = ds.Tables[0].Rows.Count;
                if(iCount > 0)
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
        /// 
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
        
        
        public  RecoveryModel   GetRecoveryModel(string databaseName)
        {
            Enumerator en                   = null;
            DataSet ds                      = new DataSet();
            ds.Locale = System.Globalization.CultureInfo.InvariantCulture;
            Request req                     = new Request();
            RecoveryModel recoveryModel = RecoveryModel.Simple;         

            en              = new Enumerator();

            req.Urn         = "Server/Database[@Name='" + Urn.EscapeString(databaseName) + "']/Option";
            req.Fields      = new string[1];
            req.Fields[0]   = "RecoveryModel";


            ds = en.Process(this.SqlConnection, req);

            int iCount = ds.Tables[0].Rows.Count;

            if (iCount > 0)
            {   
                recoveryModel = (RecoveryModel)(ds.Tables[0].Rows[0]["RecoveryModel"]);         
            }                               
            return recoveryModel;
        }
        

        public  string  GetRecoveryModelAsString(RecoveryModel recoveryModel)
        {
            string  szRecoveryModel = string.Empty;

            if (recoveryModel == RecoveryModel.Full)
            {
                szRecoveryModel = BackupConstants.RecoveryModelFull;
            }
            else if (recoveryModel == RecoveryModel.Simple)
            {
                szRecoveryModel = BackupConstants.RecoveryModelSimple;
            }
            else if (recoveryModel == RecoveryModel.BulkLogged)
            {
                szRecoveryModel = BackupConstants.RecoveryModelBulk;
            }
             
            return szRecoveryModel;
        }


        public string GetDefaultBackupFolder()
        {
            string BackupFolder = "";

            Enumerator en = null;
            DataSet ds = new DataSet();
            ds.Locale = System.Globalization.CultureInfo.InvariantCulture;
            Request req = new Request();

            en = new Enumerator();

            req.Urn = "Server/Setting";

            ds = en.Process(SqlConnection, req);

            int iCount = ds.Tables[0].Rows.Count;

            if (iCount > 0)
            {
                BackupFolder = Convert.ToString(ds.Tables[0].Rows[0]["BackupDirectory"], System.Globalization.CultureInfo.InvariantCulture);
            }
            return BackupFolder;
        }

        
        public  int     GetMediaRetentionValue()
        {
            int AfterXDays = 0;
            try
            {
                Enumerator en = new Enumerator();
                Request req = new Request();
                DataSet ds = new DataSet();
                ds.Locale = System.Globalization.CultureInfo.InvariantCulture;

                req.Urn = "Server/Configuration";                

                ds = en.Process(this.SqlConnection, req);               
                for (int i = 0 ; i < ds.Tables[0].Rows.Count; i++)
                {
                    if (Convert.ToString(ds.Tables[0].Rows[i]["Name"], System.Globalization.CultureInfo.InvariantCulture) == "media retention")
                    {
                        AfterXDays = Convert.ToInt32(ds.Tables[0].Rows[i]["RunValue"], System.Globalization.CultureInfo.InvariantCulture);
                        break;
                    }
                }                                                                               
                return AfterXDays;
            }
            catch (Exception)
            {   
                return AfterXDays;
            }           
        }

        public bool IsDestinationPathValid(string path, ref bool IsFolder)
        {
            Enumerator en = null;
            DataTable dt;
            Request req = new Request();

            en = new Enumerator();
            req.Urn = "Server/File[@FullName='" + Urn.EscapeString(path) + "']";

            dt = en.Process(this.SqlConnection, req);

            int iCount = dt.Rows.Count;

            if (iCount > 0)
            {
                IsFolder = !(Convert.ToBoolean(dt.Rows[0]["IsFile"], System.Globalization.CultureInfo.InvariantCulture));
                return true;
            }
            else
            {
                IsFolder = false;
                return false;
            }
        }

        public string   GetMediaNameFromBackupSetId(int backupSetId)
        {
            Enumerator          en  = null;
            DataSet             ds  = new DataSet();
            ds.Locale = System.Globalization.CultureInfo.InvariantCulture;
            Request             req = new Request();    

            int mediaId             = -1;
            string mediaName        = string.Empty;
                        
            en = new Enumerator();

            req.Urn = "Server/BackupSet[@ID='" + Urn.EscapeString(Convert.ToString(backupSetId, System.Globalization.CultureInfo.InvariantCulture)) + "']";

            try
            {
                ds = en.Process(SqlConnection, req);

                int iCount = ds.Tables[0].Rows.Count;

                if (iCount > 0)
                {
                    mediaId = Convert.ToInt32(ds.Tables[0].Rows[0]["MediaSetId"], System.Globalization.CultureInfo.InvariantCulture);
                    ds.Clear();

                    req.Urn = "Server/BackupMediaSet[@ID='" + Urn.EscapeString(Convert.ToString(mediaId, System.Globalization.CultureInfo.InvariantCulture)) + "']";
                    ds = en.Process(SqlConnection, req);

                    iCount = ds.Tables[0].Rows.Count;
                    if (iCount > 0)
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
        
        public string   GetNewPhysicalRestoredFileName(string OriginalName, string dbName, bool isNewDatabase, string type, ref int fileIndex)
        {
            if (string.IsNullOrEmpty(OriginalName))
            {
                //shall never come here
                return string.Empty;
            }
            
            string  result      = string.Empty;
            string  origfile    = OriginalName;
            int     Idx         = origfile.LastIndexOf('\\');
            string  origpath    = origfile.Substring(0,Idx);

            //getting the filename
            string origname = origfile.Substring(Idx + 1);
            Idx = origname.LastIndexOf('.');
            string origext = origname.Substring(Idx + 1);

            bool isFolder = true;
            bool isValidPath    = IsDestinationPathValid(origpath, ref isFolder);

            if (!isValidPath || !isFolder)
            {
                SMO.Server server = new SMO.Server(this.SqlConnection);
                if (type != RestoreConstants.Log)
                {
                    origpath = server.Settings.DefaultFile;
                    if (origpath.Length == 0)
                    {
                        origpath = server.Information.MasterDBPath;
                    }
                }
                else 
                {
                    origpath = server.Settings.DefaultLog;
                    if (origpath.Length == 0)
                    {
                        origpath = server.Information.MasterDBLogPath;
                    }
                }
            }
            else
            {
                if (!isNewDatabase)
                {
                    return OriginalName;
                }
            }

            if (!isNewDatabase)
            {
                result = origpath + "\\" + dbName + "." + origext;
            }
            else
            {
                if (0 != string.Compare(origext, "mdf", StringComparison.OrdinalIgnoreCase))
                {
                    result = origpath + "\\" + dbName + "_" + Convert.ToString(fileIndex, System.Globalization.CultureInfo.InvariantCulture) + "." + origext;
                    fileIndex++;
                }
                else
                {
                    result = origpath + "\\" + dbName + "." + origext;
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
            SMO.Server server = new SMO.Server(this.SqlConnection);

            // TODO: SqlConnection.ServerVersion is used instead of server.ServerVersion
            if ((this.SqlConnection.ServerVersion.Major < 11) || (!server.IsHadrEnabled))
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
            SMO.Server server = new SMO.Server(this.SqlConnection);
            // TODO: SqlConnection.ServerVersion is used instead of server.ServerVersion
            if (this.SqlConnection.ServerVersion.Major < 9)
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

            bool retVal = true;
            bool localPrimaryReplica = this.IsLocalPrimaryReplica(hadrDb);

            if (localPrimaryReplica)
            {
                return retVal;
            }

            //Set un-supported backuptype to false
            if(0 == string.Compare(backupTypeStr, BackupConstants.BackupTypeFull, StringComparison.OrdinalIgnoreCase))
            {
                if (!copyOnly)
                {
                    //Full
                    retVal = false;
                }
            }
            else if(0 == string.Compare(backupTypeStr, BackupConstants.BackupTypeDiff, StringComparison.OrdinalIgnoreCase))
            {
                //Diff
                retVal = false;
            }
            else if(0 == string.Compare(backupTypeStr, BackupConstants.BackupTypeTLog, StringComparison.OrdinalIgnoreCase))
            {
                //Log-CopyOnly
                if (copyOnly)
                {
                    retVal = false;
                }
            }

            return retVal;
        }

                
        public bool IsDatabaseOnServer(string databaseName)
        {
            Enumerator          en  = new Enumerator();
            DataSet             ds  = new DataSet();
            ds.Locale = System.Globalization.CultureInfo.InvariantCulture;
            Request             req = new Request();                                        
            
            req.Urn             = "Server/Database[@Name='" + Urn.EscapeString(databaseName) + "']";
            req.Fields          = new string[1];
            req.Fields[0]       = "Name";

            ds = en.Process(SqlConnection, req);

            int iCount  = ds.Tables[0].Rows.Count;

            return (iCount > 0) ? true : false;
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

        public void GetBackupSetTypeAndComponent(int numType, ref string Type, ref string Component)
        {
            switch (numType)
            {
                case 1:
                    Type = RestoreConstants.TypeFull;
                    Component = RestoreConstants.ComponentDatabase;
                    break;
                case 2:
                    Type = RestoreConstants.TypeTransactionLog;
                    Component = "";
                    break;                
                case 4:
                    Type = RestoreConstants.TypeFilegroup;
                    Component = RestoreConstants.ComponentFile;
                    break;
                case 5:
                    Type = RestoreConstants.TypeDifferential;
                    Component = RestoreConstants.ComponentDatabase;
                    break;
                case 6:
                    Type = RestoreConstants.TypeFilegroupDifferential;
                    Component = RestoreConstants.ComponentFile;
                    break;
                default:
                    Type = RestoreConstants.NotKnown;
                    Component = RestoreConstants.NotKnown;
                    break;
            }

        }
        
        
        public void GetBackupSetTypeAndComponent(string strType, ref string Type, ref string Component)
        {           
            string type = strType.ToUpperInvariant();

            if (type == "D")
            {
                Type = RestoreConstants.TypeFull;
                Component = RestoreConstants.ComponentDatabase;
            }
            else
            {
                if (type == "I")
                {
                    Type = RestoreConstants.TypeDifferential;
                    Component = RestoreConstants.ComponentDatabase;
                }
                else
                {
                    if (type == "L")
                    {
                        Type = RestoreConstants.TypeTransactionLog;
                        Component = "";
                    }
                    else
                    {
                        if (type == "F")
                        {
                            Type = RestoreConstants.TypeFilegroup;
                            Component = RestoreConstants.ComponentFile;
                        }
                        else
                        {
                            if (type == "G")
                            {
                                Type = RestoreConstants.TypeFilegroupDifferential;
                                Component = RestoreConstants.ComponentFile;
                            }
                            else
                            {
                                Type = RestoreConstants.NotKnown;
                                Component = RestoreConstants.NotKnown;
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
            BackupsetType   Result  = BackupsetType.BackupsetDatabase;
            switch(type)
            {
                case 1:
                    Result  = BackupsetType.BackupsetDatabase;
                    break;
                case 2:
                    Result  = BackupsetType.BackupsetLog;
                    break;
                case 5:
                    Result  = BackupsetType.BackupsetDifferential;
                    break;
                case 4:
                    Result  = BackupsetType.BackupsetFiles;
                    break;
                default:
                    Result  = BackupsetType.BackupsetDatabase;
                    break;
            }
            return Result;
        }

        
        public BackupsetType GetBackupsetTypeFromBackupTypesOnHistory(string type)
        {
            BackupsetType   Result  = BackupsetType.BackupsetDatabase;
            switch(type)
            {
                case "D":
                    Result  = BackupsetType.BackupsetDatabase;
                    break;
                case "I":
                    Result  = BackupsetType.BackupsetDifferential;
                    break;
                case "L":
                    Result  = BackupsetType.BackupsetLog;
                    break;
                case "F":
                    Result  = BackupsetType.BackupsetFiles;
                    break;
                default:
                    Result  = BackupsetType.BackupsetDatabase;
                    break;
            }
            return Result;
        }

        
        public DataSet  GetBackupSetFiles(int backupsetId)
        {
            Enumerator  en              = new Enumerator();
            Request     req             = new Request();
            DataSet     backupsetfiles  = new DataSet();
            backupsetfiles.Locale = System.Globalization.CultureInfo.InvariantCulture;

            if(backupsetId > 0)
            {
                req.Urn = "Server/BackupSet[@ID='" + Urn.EscapeString(Convert.ToString(backupsetId, System.Globalization.CultureInfo.InvariantCulture)) + "']/File";
            }
            else
            {
                req.Urn             = "Server/BackupSet/File";
            }
            backupsetfiles      = en.Process(SqlConnection, req);

            return backupsetfiles;
        }
        
        
        public DataSet  GetBackupSetById(int backupsetId)
        {
            
            SqlExecutionModes    executionMode   = SqlConnection.SqlExecutionModes;
            SqlConnection.SqlExecutionModes      = SqlExecutionModes.ExecuteSql;
            Enumerator  en          = new Enumerator();
            Request     req         = new Request();
            DataSet     backupset   = new DataSet();
            backupset.Locale = System.Globalization.CultureInfo.InvariantCulture;

            req.Urn = "Server/BackupSet[@ID='" + Urn.EscapeString(Convert.ToString(backupsetId, System.Globalization.CultureInfo.InvariantCulture)) + "']";
            backupset           = en.Process(SqlConnection, req);

            SqlConnection.SqlExecutionModes      = executionMode;

            return backupset;

        }
        
        
        public ArrayList GetBackupSetPhysicalSources(int backupsetId)
        {
            
            SqlExecutionModes    executionMode   = SqlConnection.SqlExecutionModes;
            SqlConnection.SqlExecutionModes      = SqlExecutionModes.ExecuteSql;

            ArrayList Sources   = new ArrayList();

            DataSet     BackupSet   = GetBackupSetById(backupsetId);
            if(BackupSet.Tables[0].Rows.Count == 1)
            {
                string MediaSetID = Convert.ToString(BackupSet.Tables[0].Rows[0]["MediaSetId"], System.Globalization.CultureInfo.InvariantCulture);

                Enumerator  en          = new Enumerator();
                Request     req         = new Request();
                DataSet     mediafamily = new DataSet();
                mediafamily.Locale = System.Globalization.CultureInfo.InvariantCulture;            

                req.Urn             = "Server/BackupMediaSet[@ID='"+Urn.EscapeString(MediaSetID)+"']/MediaFamily";
                mediafamily         = en.Process(SqlConnection, req);

                if(mediafamily.Tables[0].Rows.Count > 0)
                {
                    for(int j = 0 ; j < mediafamily.Tables[0].Rows.Count; j ++)
                    {
                        
                        RestoreItemSource   ItemSource  = new RestoreItemSource();

                        ItemSource.RestoreItemLocation = Convert.ToString(mediafamily.Tables[0].Rows[j]["PhysicalDeviceName"], System.Globalization.CultureInfo.InvariantCulture);

                        BackupDeviceType  backupDeviceType = (BackupDeviceType)Enum.Parse(typeof(BackupDeviceType), mediafamily.Tables[0].Rows[j]["BackupDeviceType"].ToString());
                        
                        if (BackupDeviceType.Disk == backupDeviceType )
                        {
                            ItemSource.RestoreItemDeviceType = DeviceType.File;                                    
                        }
                        else if (BackupDeviceType.Url == backupDeviceType)
                        {
                            ItemSource.RestoreItemDeviceType = DeviceType.Url;
                        }                                
                        else
                        {
                            ItemSource.RestoreItemDeviceType = DeviceType.Tape;                                    
                        }                                
                        Sources.Add(ItemSource);                        
                    }
                }
            }

            SqlConnection.SqlExecutionModes      = executionMode;
            return Sources;
        }
        
        
        public  RestoreActionType   GetRestoreTaskFromBackupSetType(BackupsetType type)
        {
            RestoreActionType   Result  = RestoreActionType.Database;
            
            switch(type)
            {
                case BackupsetType.BackupsetDatabase:
                    Result  = RestoreActionType.Database;
                    break;
                case BackupsetType.BackupsetDifferential:
                    Result  = RestoreActionType.Database;
                    break;
                case BackupsetType.BackupsetLog:
                    Result  = RestoreActionType.Log;
                    break;
                case BackupsetType.BackupsetFiles:
                    Result  = RestoreActionType.Files;
                    break;
                default:
                    Result  = RestoreActionType.Database;
                    break;
            }
            return Result;
        }

        
        public  int GetLatestBackup(string DatabaseName,string BackupSetName)
        {
            Enumerator  en          = new Enumerator();
            Request     req         = new Request();
            DataSet     backupSets  = new DataSet();
            backupSets.Locale = System.Globalization.CultureInfo.InvariantCulture;            
            OrderBy     ob;

            req.Urn             = "Server/BackupSet[@Name='"+Urn.EscapeString(BackupSetName)+"' and @DatabaseName='"+ Urn.EscapeString(DatabaseName)+"']";
            
            req.OrderByList     = new OrderBy[1];
            ob                  = new OrderBy("BackupFinishDate", OrderBy.Direction.Desc);
            req.OrderByList[0]  = ob;

            backupSets          = en.Process(SqlConnection, req);

            if(backupSets.Tables[0].Rows.Count > 0)
            {
                return Convert.ToInt32(backupSets.Tables[0].Rows[0]["Position"], System.Globalization.CultureInfo.InvariantCulture);
            }
            else
            {
                return -1;
            }
        }

        
        public List<RestoreItemSource> GetLatestBackupLocations(string DatabaseName)
        {
            List<RestoreItemSource>  LatestLocations   = new List<RestoreItemSource>();

            Enumerator en = null;
            DataSet ds = new DataSet();
            ds.Locale = System.Globalization.CultureInfo.InvariantCulture;
            Request req = new Request();    
                        
            en = new Enumerator();

            req.Urn = "Server/BackupSet[@DatabaseName='" + Urn.EscapeString(DatabaseName) + "']";
            req.OrderByList = new OrderBy[1];
            req.OrderByList[0] = new OrderBy();
            req.OrderByList[0].Field = "BackupFinishDate";
            req.OrderByList[0].Dir = OrderBy.Direction.Desc;

            try
            {
                ds = en.Process(SqlConnection, req);

                int iCount = ds.Tables[0].Rows.Count;
                if (iCount > 0)
                {
                    string MediaSetID = Convert.ToString(ds.Tables[0].Rows[0]["MediaSetId"], System.Globalization.CultureInfo.InvariantCulture);
                    ds.Clear();
                    req = new Request();
                    req.Urn = "Server/BackupMediaSet[@ID='" + Urn.EscapeString(MediaSetID) + "']/MediaFamily";
                    ds = en.Process(SqlConnection, req);
                    iCount = ds.Tables[0].Rows.Count;
                    if (iCount > 0)
                    {
                        for (int i = 0; i < iCount; i++)
                        {
                            RestoreItemSource Location = new RestoreItemSource();

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
                            Location.RestoreItemDeviceType = deviceType;
                            Location.RestoreItemLocation = location;
                            Location.IsLogicalDevice = isLogical;
                            LatestLocations.Add(Location);
                        }
                    }
                }
            }
            /// LPU doesn't have rights to enumerate msdb.backupset
            catch (Exception)
            {                
            }            
            return LatestLocations;            
        }                       
        
        public string   GetDefaultDatabaseForLogin(string LoginName)
        {
            
            string DefaultDatabase  = string.Empty;

            Enumerator en = new Enumerator();
            DataSet ds = new DataSet();
            ds.Locale = System.Globalization.CultureInfo.InvariantCulture;
            Request req = new Request();                                        
            
            req.Urn = "Server/Login[@Name='"+Urn.EscapeString(LoginName)+"']";
            req.Fields = new string[1];
            req.Fields[0] = "DefaultDatabase";

            ds = en.Process(SqlConnection, req);

            int iCount = ds.Tables[0].Rows.Count;
            if (iCount > 0)
            {
                DefaultDatabase = Convert.ToString(ds.Tables[0].Rows[0]["DefaultDatabase"], System.Globalization.CultureInfo.InvariantCulture);
            }
            return DefaultDatabase;   

        }

        
        public bool     IsPathExisting(string path,ref bool IsFolder)
        {
            Enumerator en   = new Enumerator();
            DataSet ds      = new DataSet();
            ds.Locale = System.Globalization.CultureInfo.InvariantCulture;
            Request req     = new Request();
                        
            req.Urn = "Server/File[@FullName='" + Urn.EscapeString(path) + "']";

            ds = en.Process(SqlConnection, req);

            int iCount = ds.Tables[0].Rows.Count;

            if (iCount > 0)
            {
                IsFolder = !(Convert.ToBoolean(ds.Tables[0].Rows[0]["IsFile"], System.Globalization.CultureInfo.InvariantCulture));
                return true;
            }
            else
            {
                IsFolder = false;
                return false;
            }
        }


        public ArrayList    IsPhysicalPathInLogicalDevice(string physicalPath)
        {
            
            Enumerator en   = new Enumerator();
            DataSet ds      = new DataSet();
            ds.Locale = System.Globalization.CultureInfo.InvariantCulture;
            Request req     = new Request();
            ArrayList Result= null;            
            
            int iCount      = 0;            
                                               
            req.Urn             = "Server/BackupDevice[@PhysicalLocation='" +Urn.EscapeString(physicalPath)+ "']";

            ds          = en.Process(SqlConnection, req);           
            iCount      = ds.Tables[0].Rows.Count;
            
            if(iCount > 0)
            {
                Result  = new ArrayList(iCount);
                for(int i =0 ; i < iCount ; i++)
                {
                    Result.Add(Convert.ToString(ds.Tables[0].Rows[0]["Name"], System.Globalization.CultureInfo.InvariantCulture));
                }
            }               
            return Result;
        }

        #region Implementation - HelperFunctions - Machine Name
        private const string cLocalSqlServer = "(local)";
        private const string cLocalMachineName = ".";
        public string   GetMachineName(string sqlServerName)
        {
            System.Diagnostics.Debug.Assert(sqlServerName != null);

            // special case (local) which is accepted SQL(MDAC) but by OS
            if (
                (sqlServerName.ToLowerInvariant().Trim() == cLocalSqlServer) ||
                (sqlServerName.ToLowerInvariant().Trim() == cLocalMachineName)
                )
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
        
        #endregion
    }
}