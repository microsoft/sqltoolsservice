//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Resources;
using System.Data;
using System.IO;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Smo = Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Diagnostics;
using System.Globalization;
using System.Data.SqlClient;
using System.Collections.Generic;
using AzureEdition = Microsoft.SqlTools.ServiceLayer.Admin.AzureSqlDbHelper.AzureEdition;
using DataSet = Microsoft.Data.Tools.DataSets.DataSet;
using DataTable = Microsoft.Data.Tools.DataSets.DataTable;

namespace Microsoft.SqlTools.ServiceLayer.Admin
{
    public enum DefaultCursor
    {
        Local,
        Global
    }

    public static class CDataContainerExtender
    {
        /// <summary>
        /// Check if Query Store is supported.
        /// </summary>
        /// <param name="dataContainer">The data container.</param>
        /// <param name="database">The database.</param>
        /// <returns>True, if the Query Store feature is supported.</returns>
        internal static bool IsQueryStoreSupported(this CDataContainer dataContainer, Smo.Database database)
        {
            // Query store is not supported on SQL DW.
            // Query store is only supported on SQL 2016 and above and Azure V12 and above.
            return (database.DatabaseEngineEdition != DatabaseEngineEdition.SqlDataWarehouse &&
                    ((dataContainer.SqlServerVersion >= 13) ||
                     (dataContainer.SqlServerVersion >= 12 &&
                      dataContainer.Server.ServerType == DatabaseEngineType.SqlAzureDatabase)));
        }
    }

    /// <summary>
    /// FileGroup Prototype
    /// </summary>
    public class FilegroupPrototype
    {
        #region data members

        private class FilegroupData
        {
            public string name;
            public bool isReadOnly;
            public bool isDefault;
            public FileGroupType fileGroupType = FileGroupType.RowsFileGroup;

            /// <summary>
            /// Creates an instance of FilegroupData
            /// </summary>
            public FilegroupData()
            {
                this.name = String.Empty;
                this.isReadOnly = false;
                this.isDefault = false;
            }

            /// <summary>
            /// Creates an instance of FilegroupData
            /// </summary>
            public FilegroupData(FileGroupType fileGroupType)
            {
                this.name = String.Empty;
                this.isReadOnly = false;
                this.isDefault = false;
                this.fileGroupType = fileGroupType;
            }

            /// <summary>
            /// Initializes a new instance of the FilegroupData class.
            /// </summary>
            /// <param name="name">filegroup name</param>
            /// <param name="isReadOnly">Readonly or not</param>
            /// <param name="isDefault">Default filegroup or not</param>
            /// <param name="fileGroupType">FileGroupType</param>
            public FilegroupData(string name, bool isReadOnly, bool isDefault, FileGroupType fileGroupType)
            {
                this.name = name;
                this.isReadOnly = isReadOnly;
                this.isDefault = isDefault;
                this.fileGroupType = fileGroupType;
            }

            /// <summary>
            /// Creates an instance of FilegroupData from another instance
            /// </summary>
            /// <param name="other"></param>
            public FilegroupData(FilegroupData other)
            {
                this.name = other.name;
                this.isReadOnly = other.isReadOnly;
                this.isDefault = other.isDefault;
                this.fileGroupType = other.fileGroupType;
            }

            /// <summary>
            /// Clones the instance oc FileGroupData
            /// </summary>
            /// <returns></returns>
            public FilegroupData Clone()
            {
                return new FilegroupData(this);
            }
        }

        private FilegroupData originalState;
        private FilegroupData currentState;
        private bool filegroupExists;
        private bool removed;
        private DatabasePrototype parent;

        #endregion

        #region properties

        /// <summary>
        /// The name of the filegroup
        /// </summary>
        public string Name
        {
            get { return this.currentState.name; }

            set
            {
                if (!this.Exists)
                {
                    string oldname = this.currentState.name;
                    this.currentState.name = value;

                    this.NotifyFileGroupNameChanged(oldname, this.currentState.name);
                    this.parent.NotifyObservers();
                }
            }
        }

        /// <summary>
        /// Whether the filegroup is read-only
        /// </summary>
        public bool IsReadOnly
        {
            get { return this.currentState.isReadOnly; }

            set
            {
                this.currentState.isReadOnly = value;
                this.parent.NotifyObservers();
            }
        }

        /// <summary>
        /// Whether the filegroup is the default filegroup
        /// </summary>
        public bool IsDefault
        {
            get { return this.currentState.isDefault; }

            set
            {
                if (this.currentState.isDefault != value)
                {
                    this.currentState.isDefault = value;
                    NotifyFileGroupDefaultChanged(!value, value);
                    this.parent.NotifyObservers();
                }
            }
        }

        /// <summary>
        /// Whether the filegroup is of filestream type
        /// </summary>
        public bool IsFileStream
        {
            get { return (this.currentState.fileGroupType == Smo.FileGroupType.FileStreamDataFileGroup); }
        }

        /// <summary>
        /// Whether the filegroup is of memory Optimized type
        /// </summary>
        public bool IsMemoryOptimized
        {
            get { return (this.currentState.fileGroupType == Smo.FileGroupType.MemoryOptimizedDataFileGroup); }
        }

        /// <summary>
        /// FileGroupType
        /// </summary>
        public FileGroupType FileGroupType
        {
            get { return this.currentState.fileGroupType; }

            set
            {
                this.currentState.fileGroupType = value;
                this.parent.NotifyObservers();
            }
        }

        /// <summary>
        /// Whether the file group exists on the server
        /// </summary>
        public bool Exists
        {
            get { return this.filegroupExists; }

            set { this.filegroupExists = value; }
        }

        /// <summary>
        /// Whether the filegroup was removed
        /// </summary>
        public bool Removed
        {
            get { return this.removed; }

            set
            {
                this.removed = value;
                this.parent.NotifyObservers();
            }
        }

        #endregion

        /// <summary>
        /// File group name changed event
        /// </summary>
        public event FileGroupNameChangedEventHandler OnFileGroupNameChangedHandler;

        /// <summary>
        /// File group default status changed event
        /// </summary>
        public event FileGroupDefaultChangedEventHandler OnFileGroupDefaultChangedHandler;

        /// <summary>
        /// File group deleted event
        /// </summary>
        public event FileGroupDeletedEventHandler OnFileGroupDeletedHandler;

        /// <summary>
        /// Constructor
        /// </summary>
        public FilegroupPrototype(DatabasePrototype parent)
        {
            this.originalState = new FilegroupData();
            this.currentState = this.originalState.Clone();
            this.parent = parent;

            this.filegroupExists = false;
            this.removed = false;
        }

        /// <summary>
        /// Creates an instance of FilegroupPrototype
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="filegroupType"></param>
        public FilegroupPrototype(DatabasePrototype parent, FileGroupType filegroupType)
        {
            this.originalState = new FilegroupData(filegroupType);
            this.currentState = this.originalState.Clone();
            this.parent = parent;

            this.filegroupExists = false;
            this.removed = false;
        }

        /// <summary>
        ///  Initializes a new instance of the FilegroupPrototype class.
        /// </summary>
        /// <param name="parent">instance of DatabasePrototype</param>
        /// <param name="name">file group name</param>
        /// <param name="isReadOnly">whether it is readonly or not</param>
        /// <param name="isDefault">is default or not</param>
        /// <param name="filegroupType">filegrouptype</param>
        /// <param name="exists">filegroup exists or not</param>
        public FilegroupPrototype(DatabasePrototype parent, string name, bool isReadOnly, bool isDefault,
            FileGroupType filegroupType, bool exists)
        {
            this.originalState = new FilegroupData(name, isReadOnly, isDefault, filegroupType);
            this.currentState = this.originalState.Clone();
            this.parent = parent;

            this.filegroupExists = exists;
            this.removed = false;
        }

        /// <summary>
        /// Create, Alter, or Drop the filegroup on the server
        /// </summary>
        public void ApplyChanges(Database db)
        {
            if (this.ChangesExist())
            {
                if (this.Removed)
                {
                    if (this.Exists)
                    {
                        db.FileGroups[this.Name].Drop();
                    }
                }
                else
                {
                    FileGroup fg = null;
                    bool filegroupChanged = false;

                    if (this.Exists)
                    {
                        fg = db.FileGroups[this.Name];
                    }
                    else
                    {                  
                        fg = new FileGroup(db, this.Name, this.FileGroupType);                        
                        db.FileGroups.Add(fg);
                    }

                    if (!this.Exists || (fg.ReadOnly != this.IsReadOnly))
                    {
                        fg.ReadOnly = this.IsReadOnly;
                        filegroupChanged = true;
                    }

                    if (this.Exists && filegroupChanged)
                    {
                        fg.Alter();
                    }
                }
            }
        }

        /// <summary>
        /// Would applying changes do anything?
        /// </summary>
        /// <returns>True if changes exist, false otherwise</returns>
        public bool ChangesExist()
        {
            // name changes can only happen for non-existent filegroups, so no need to check for name changes

            bool result = (
                !this.Exists ||
                this.Removed ||
                (this.originalState.isDefault != this.currentState.isDefault) ||
                (this.originalState.isReadOnly != this.currentState.isReadOnly));

            return result;
        }

        /// <summary>
        /// Notify listeners that this filegroup has been deleted
        /// </summary>
        /// <param name="defaultFilegroup"></param>
        public void NotifyFileGroupDeleted(FilegroupPrototype defaultFilegroup)
        {
            this.Removed = true;

            if (OnFileGroupDeletedHandler != null)
            {
                FilegroupDeletedEventArgs e = new FilegroupDeletedEventArgs(this, defaultFilegroup);
                OnFileGroupDeletedHandler(this, e);
            }
        }

        /// <summary>
        /// Notify observers that the file group name has changed
        /// </summary>
        /// <param name="oldName">The file group name before the change</param>
        /// <param name="newName">The file group name after the change</param>
        private void NotifyFileGroupNameChanged(string oldName, string newName)
        {
            if (OnFileGroupNameChangedHandler != null)
            {
                NameChangedEventArgs e = new NameChangedEventArgs(oldName, newName);
                OnFileGroupNameChangedHandler(this, e);
            }
        }

        /// <summary>
        /// Notify observers that the file group's default status has changed
        /// </summary>
        /// <param name="oldValue">The old default-ness</param>
        /// <param name="newValue">THe new default-ness</param>
        private void NotifyFileGroupDefaultChanged(bool oldValue, bool newValue)
        {
            if (OnFileGroupDefaultChangedHandler != null)
            {
                BooleanValueChangedEventArgs e = new BooleanValueChangedEventArgs(oldValue, newValue);
                OnFileGroupDefaultChangedHandler(this, e);
            }
        }
    }


    /// <summary>
    /// File type - Data or Log
    /// </summary>
    public enum FileType
    {
        Data,
        Log,
        FileStream
    }

    /// <summary>
    /// File auto-growth data
    /// </summary>
    public class Autogrowth
    {
        #region data members

        private int growthInPercent;
        private double growthInKilobytes;
        private double maximumFileSize;
        private bool isEnabled;
        private bool isGrowthInPercent;
        private bool isGrowthRestricted;

        private DatabasePrototype parent;

        #endregion

        #region properties

        /// <summary>
        /// Whether auto-growth is enabled
        /// </summary>
        public bool IsEnabled
        {
            get { return this.isEnabled; }

            set { this.isEnabled = value; }
        }

        /// <summary>
        /// Whether auto-growth is in percent
        /// </summary>
        /// <remarks>
        /// true means growth is in percent, false means growth is in megabytes
        /// </remarks>
        public bool IsGrowthInPercent
        {
            get { return this.isGrowthInPercent; }

            set { this.isGrowthInPercent = value; }
        }

        /// <summary>
        /// How much the file grows when it grows, in percent
        /// </summary>
        public int GrowthInPercent
        {
            get { return this.growthInPercent; }

            set { this.growthInPercent = value; }
        }

        /// <summary>
        /// How much the file grows when it grows in  kilobytes
        /// </summary>
        public double GrowthInKilobytes
        {
            get { return this.growthInKilobytes; }

            set { this.growthInKilobytes = value; }
        }

        /// <summary>
        /// How much the file grows when it grows in  kilobytes
        /// </summary>
        public int GrowthInMegabytes
        {
            get { return DatabaseFilePrototype.KilobytesToMegabytes(this.growthInKilobytes); }

            set { this.growthInKilobytes = DatabaseFilePrototype.MegabytesToKilobytes(value); }
        }


        /// <summary>
        /// Whether file growth is restricted
        /// </summary>
        public bool IsGrowthRestricted
        {
            get { return this.isGrowthRestricted; }

            set { this.isGrowthRestricted = value; }
        }

        /// <summary>
        /// The maximum size of the file in megabytes
        /// </summary>
        public int MaximumFileSizeInMegabytes
        {
            get { return DatabaseFilePrototype.KilobytesToMegabytes(this.maximumFileSize); }

            set { this.maximumFileSize = DatabaseFilePrototype.MegabytesToKilobytes(value); }
        }

        /// <summary>
        /// The maximum size of the file in megabytes
        /// </summary>
        public double MaximumFileSizeInKilobytes
        {
            get { return this.maximumFileSize; }

            set { this.maximumFileSize = value; }
        }


        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public Autogrowth(DatabasePrototype parent)
        {
            Reset();
            this.parent = parent;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="other">the instance to copy</param>
        public Autogrowth(Autogrowth other)
        {
            this.growthInKilobytes = other.growthInKilobytes;
            this.growthInPercent = other.growthInPercent;
            this.maximumFileSize = other.maximumFileSize;
            this.isEnabled = other.isEnabled;
            this.isGrowthInPercent = other.isGrowthInPercent;
            this.isGrowthRestricted = other.isGrowthRestricted;
            this.parent = other.parent;
        }

        /// <summary>
        /// Constructor - extracts autogrowth information from a SMO DataFile object
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="file">The file whose autogrowth information is to be extracted</param>
        public Autogrowth(DatabasePrototype parent, DataFile file)
        {
            // this code looks stunningly similar to the code in Autogrowth(LogFile), 
            // but we can't use polymorphism to handle this because LogFile and DataFile
            // have no common base class or shared interface.

            if (FileGrowthType.None == file.GrowthType)
            {
                this.isEnabled = false;
                this.isGrowthInPercent = true;
                this.growthInPercent = 10;
                this.growthInKilobytes = 10240.0;
                this.isGrowthRestricted = false;
                this.maximumFileSize = 102400.0;
            }
            else
            {
                this.isEnabled = true;

                if (FileGrowthType.Percent == file.GrowthType)
                {
                    this.isGrowthInPercent = true;
                    this.growthInPercent = (int) file.Growth;
                    this.growthInKilobytes = 10240.0;

                    // paranoia - make sure percent amount is greater than 1
                    if (this.growthInPercent < 1)
                    {
                        this.growthInPercent = 1;
                    }
                }
                else
                {
                    this.isGrowthInPercent = false;
                    this.growthInKilobytes = file.Growth;
                    this.growthInPercent = 10;
                }

                // note:  double-precision math comparisons - can't do != or ==
                if (1e-16 < file.MaxSize)
                {
                    // max file size is greater than zero
                    this.isGrowthRestricted = true;
                    this.maximumFileSize = file.MaxSize;
                }
                else
                {
                    this.isGrowthRestricted = false;
                    this.maximumFileSize = 102400.0;
                }
            }

            this.parent = parent;
        }

        /// <summary>
        /// Constructor - extracts autogrowth information from a SMO LogFile object
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="file">The file whose autogrowth information is to be extracted</param>
        public Autogrowth(DatabasePrototype parent, LogFile file)
        {
            // this code looks stunningly similar to the code in Autogrowth(DataFile), 
            // but we can't use polymorphism to handle this because LogFile and DataFile
            // have no common base class or shared interface.

            FileGrowthType fileGrowthType = FileGrowthType.None;

            try
            {
                fileGrowthType = file.GrowthType;

            }
            catch (Exception)
            {
                /// do nothing
            }

            if (FileGrowthType.None == fileGrowthType)
            {
                this.isEnabled = false;
                this.isGrowthInPercent = true;
                this.growthInPercent = 10;
                this.growthInKilobytes = 10240.0;
                this.isGrowthRestricted = false;
                this.maximumFileSize = 102400.0;
            }
            else
            {
                this.isEnabled = true;

                if (FileGrowthType.Percent == fileGrowthType)
                {
                    this.isGrowthInPercent = true;
                    this.growthInPercent = (int) file.Growth;
                    this.growthInKilobytes = 10240.0;

                    // paranoia - make sure percent amount is greater than 1
                    if (this.growthInPercent < 1)
                    {
                        this.growthInPercent = 1;
                    }
                }
                else
                {
                    this.isGrowthInPercent = false;
                    this.growthInKilobytes = file.Growth;
                    this.growthInPercent = 10;
                }

                // note:  double-precision math comparisons - can't do != or ==
                if (1e-16 < file.MaxSize)
                {
                    // max file size is greater than zero
                    this.isGrowthRestricted = true;
                    this.maximumFileSize = file.MaxSize;
                }
                else
                {
                    this.isGrowthRestricted = false;
                    this.maximumFileSize = 102400.0;
                }
            }

            this.parent = parent;

        }

        /// <summary>
        /// Reset the property values to their defaults
        /// </summary>
        public void Reset()
        {
            this.IsEnabled = true;
            this.IsGrowthInPercent = true;
            this.IsGrowthRestricted = false;

            this.GrowthInPercent = 10;
            this.GrowthInKilobytes = 10240.0;
            this.MaximumFileSizeInKilobytes = 102400.0;
        }


        /// <summary>
        /// Determine whether this Autogrowth has the same value as another Autogrowth
        /// </summary>
        /// <param name="other">The Autogrowth to compare with</param>
        /// <returns>True if values are the same, false otherwise</returns>
        public bool HasSameValueAs(Autogrowth other)
        {
            bool result = true;

            if (this.isEnabled != other.isEnabled)
            {
                result = false;
            }
            else if (this.isEnabled)
            {
                if ((this.isGrowthInPercent != other.isGrowthInPercent) ||
                    (this.isGrowthRestricted != other.isGrowthRestricted) ||
                    (this.isGrowthInPercent && (this.growthInPercent != other.growthInPercent)) ||
                    (!this.isGrowthInPercent && (this.growthInKilobytes != other.growthInKilobytes)) ||
                    (this.isGrowthRestricted && (this.maximumFileSize != other.maximumFileSize)))
                {
                    result = false;
                }
            }

            return result;
        }

        /// <summary>
        /// Represent the auto-growth settings as a single string
        /// </summary>
        /// <remarks>
        /// The format is such that the result can be put into the Autogrowth column
        /// of the grid on the CreateDatabaseGeneral form.
        /// </remarks>
        /// <returns>The string representation</returns>
        public override string ToString()
        {
            ResourceManager manager = new ResourceManager("Microsoft.SqlTools.ServiceLayer.Localization.SR", typeof(DatabasePrototype).GetAssembly());
            string result = "";

            if (this.IsEnabled)
            {
                if (this.IsGrowthRestricted)
                {
                    if (this.IsGrowthInPercent)
                    {
                        result = String.Format(System.Globalization.CultureInfo.CurrentCulture,
                            manager.GetString("prototype.autogrowth.restrictedGrowthByPercent"),
                            this.GrowthInPercent,
                            this.MaximumFileSizeInMegabytes);
                    }
                    else
                    {
                        result = String.Format(System.Globalization.CultureInfo.CurrentCulture,
                            manager.GetString("prototype_autogrowth_restrictedGrowthByMB"),
                            this.GrowthInMegabytes,
                            this.MaximumFileSizeInMegabytes);
                    }
                }
                else
                {
                    if (this.IsGrowthInPercent)
                    {
                        result = String.Format(System.Globalization.CultureInfo.CurrentCulture,
                            manager.GetString("prototype_autogrowth_unrestrictedGrowthByPercent"),
                            this.GrowthInPercent);
                    }
                    else
                    {
                        result = String.Format(System.Globalization.CultureInfo.CurrentCulture,
                            manager.GetString("prototype_autogrowth_unrestrictedGrowthByMB"),
                            this.GrowthInMegabytes);
                    }


                }


            }
            else
            {
                result = manager.GetString("prototype.autogrowth.disabled");
            }

            return result;
        }
    }


    /// <summary>
    /// Prototype database file
    /// </summary>
    public class DatabaseFilePrototype
    {
        #region data members

        private class FileData
        {
            public string name;
            public string physicalName;
            public string folder;
            public FileType fileType;
            public FilegroupPrototype filegroup;
            public double initialSize;
            public Autogrowth autogrowth;
            public bool isPrimaryFile;

            /// <summary>
            /// Creates instance of FileData
            /// </summary>
            /// <param name="parent"></param>
            /// <param name="type"></param>
            public FileData(DatabasePrototype parent, FileType type)
            {
                this.name = String.Empty;
                this.physicalName = String.Empty;
                this.folder = String.Empty;
                this.fileType = type;
                this.filegroup = null;
                this.initialSize = 1024.0d;
                this.autogrowth = new Autogrowth(parent);
                this.isPrimaryFile = false;
            }

            /// <summary>
            /// Creates instaance of FileData
            /// </summary>
            /// <param name="parent"></param>
            /// <param name="filegroup"></param>
            /// <param name="file"></param>
            public FileData(DatabasePrototype parent, FilegroupPrototype filegroup, DataFile file)
            {
                this.autogrowth = new Autogrowth(parent, file);
                this.name = file.Name;

                if (file.FileName.EndsWith(":", StringComparison.Ordinal))
                {
                    // the data file is on a raw device
                    ResourceManager manager = new ResourceManager("Microsoft.SqlTools.ServiceLayer.Localization.SR", typeof(DatabasePrototype).GetAssembly());
                    this.physicalName = manager.GetString("general_rawDevice");
                    this.folder = file.FileName;
                }
                else
                {
                    this.physicalName = Path.GetFileName(file.FileName);
                    this.folder = PathWrapper.GetDirectoryName(file.FileName);
                }

                this.initialSize = file.Size;
                this.filegroup = filegroup;
                this.isPrimaryFile = file.IsPrimaryFile;

                switch (filegroup.FileGroupType)
                {
                    case FileGroupType.RowsFileGroup:
                        this.fileType = FileType.Data;
                        break;

                    case FileGroupType.FileStreamDataFileGroup:
                    case FileGroupType.MemoryOptimizedDataFileGroup:
                        this.fileType = FileType.FileStream;
                        break;

                    default:
                        throw new InvalidArgumentException("Unsupported filegroup type");

                }
            }

            /// <summary>
            /// Creates instance of FileData
            /// </summary>
            /// <param name="parent"></param>
            /// <param name="file"></param>
            public FileData(DatabasePrototype parent, LogFile file)
            {
                this.autogrowth = new Autogrowth(parent, file);
                this.name = file.Name;

                try
                {
                    if (file.FileName.EndsWith(":", StringComparison.Ordinal))
                    {
                        // the log file is on a raw device
                        ResourceManager manager = new ResourceManager("Microsoft.SqlTools.ServiceLayer.Localization.SR", typeof(DatabasePrototype).GetAssembly());
                        this.physicalName = manager.GetString("general_rawDevice");
                        this.folder = file.FileName;
                    }
                    else
                    {
                        this.physicalName = Path.GetFileName(file.FileName);
                        this.folder = PathWrapper.GetDirectoryName(file.FileName);
                    }

                    this.initialSize = file.Size;
                }
                catch (Exception)
                {
                    ResourceManager manager = new ResourceManager("Microsoft.SqlTools.ServiceLayer.Localization.SR", typeof(DatabasePrototype).GetAssembly());
                    this.physicalName = manager.GetString("unavailable");
                    this.folder = manager.GetString("unavailable");
                    this.initialSize = 0;
                }

                this.fileType = FileType.Log;
                this.filegroup = null;
                this.isPrimaryFile = false;
            }

            /// <summary>
            /// Creates a instance of FileData from another instance
            /// </summary>
            /// <param name="other"></param>
            public FileData(FileData other)
            {
                this.name = other.name;
                this.physicalName = other.physicalName;
                this.folder = other.folder;
                this.fileType = other.fileType;
                this.filegroup = other.filegroup;
                this.initialSize = other.initialSize;
                this.autogrowth = new Autogrowth(other.autogrowth);
                this.isPrimaryFile = other.isPrimaryFile;
            }

            /// <summary>
            /// Clone current instance of FileData
            /// </summary>
            /// <returns></returns>
            public FileData Clone()
            {
                return new FileData(this);
            }
        }

        private FileData originalState;
        private FileData currentState;

        private string defaultDataFolder = String.Empty;
        private string defaultLogFolder = String.Empty;
        private double defaultDataFileSize;
        private double defaultLogFileSize;
        private Autogrowth defaultDataAutogrowth;
        private Autogrowth defaultLogAutogrowth;
        private bool usingDefaultFolder;

        private bool fileExists;
        private bool removed;
        private DatabasePrototype database;

        private const double kilobytesPerMegabyte = 1024.0d;

        #endregion

        #region properties

        /// <summary>
        /// The logical name of the file, without extension
        /// </summary>
        public string Name
        {
            get { return this.currentState.name; }

            set
            {
                this.currentState.name = value;
                this.database.NotifyObservers();
            }
        }

        /// <summary>
        /// The physical name of the file
        /// </summary>
        public string PhysicalName
        {
            get { return this.currentState.physicalName; }

            set
            {
                this.currentState.physicalName = value;
                this.database.NotifyObservers();
            }
        }

        /// <summary>
        /// The folder in which the file is to be created
        /// </summary>
        public string Folder
        {
            get { return this.currentState.folder; }

            set
            {
                this.currentState.folder = value;
                this.usingDefaultFolder = (0 == String.Compare(value, this.DefaultFolder, StringComparison.Ordinal));
                this.database.NotifyObservers();
            }
        }

        /// <summary>
        /// The type of the file, either Log or Data
        /// </summary>
        public FileType DatabaseFileType
        {
            get { return this.currentState.fileType; }

            set
            {
                this.currentState.fileType = value;

                if (this.usingDefaultFolder)
                {
                    this.currentState.folder = this.DefaultFolder;
                }

                this.database.NotifyObservers();
            }
        }

        /// <summary>
        /// The prototype of the filegroup that is to contain this file
        /// </summary>
        public FilegroupPrototype FileGroup
        {
            get { return this.currentState.filegroup; }

            set
            {
                if ((FileType.Data == this.currentState.fileType ||
                     FileType.FileStream == this.currentState.fileType) && !this.Exists && (value != null))
                {
                    if (this.currentState.filegroup != null)
                    {
                        this.currentState.filegroup.OnFileGroupDeletedHandler -=
                            new FileGroupDeletedEventHandler(OnFilegroupDeleted);
                    }

                    this.currentState.filegroup = value;
                    this.currentState.filegroup.OnFileGroupDeletedHandler +=
                        new FileGroupDeletedEventHandler(OnFilegroupDeleted);
                    this.database.NotifyObservers();
                }
            }
        }

        /// <summary>
        /// The initial size of the file in Megabytes
        /// </summary>
        public int InitialSize
        {
            get
            {
                // size kept in kilobytes internally
                return DatabaseFilePrototype.KilobytesToMegabytes(this.currentState.initialSize);
            }

            set
            {
                // size kept in kilobytes internally
                this.currentState.initialSize = DatabaseFilePrototype.MegabytesToKilobytes(value);
                this.database.NotifyObservers();
            }
        }

        /// <summary>
        /// Auto-growth data for the file
        /// </summary>
        public Autogrowth Autogrowth
        {
            get { return this.currentState.autogrowth; }

            set
            {
                this.currentState.autogrowth = new Autogrowth(value);
                this.database.NotifyObservers();
            }
        }

        /// <summary>
        /// Whether this is the primary data file (the file with the .mdf extension)
        /// </summary>
        public bool IsPrimaryFile
        {
            get { return this.currentState.isPrimaryFile; }

            set
            {
                this.currentState.isPrimaryFile = value;
                this.database.NotifyObservers();
            }
        }

        /// <summary>
        /// The default folder for this file type
        /// </summary>
        public string DefaultFolder
        {
            get
            {
                if (this.currentState.fileType == FileType.Log)
                {
                    return this.defaultLogFolder;
                }
                else
                {
                    return this.defaultDataFolder;
                }
            }
        }

        /// <summary>
        /// The default size for this file type
        /// </summary>
        public int DefaultSize
        {
            get
            {
                if (this.currentState.fileType == FileType.Log)
                {
                    return DatabaseFilePrototype.KilobytesToMegabytes(this.defaultLogFileSize);
                }
                else
                {
                    return DatabaseFilePrototype.KilobytesToMegabytes(this.defaultDataFileSize);
                }
            }
            set
            {
                if (this.currentState.fileType == FileType.Log)
                {
                    this.defaultLogFileSize = DatabaseFilePrototype.MegabytesToKilobytes(value);
                }
                else
                {
                    this.defaultDataFileSize = DatabaseFilePrototype.MegabytesToKilobytes(value);
                }
            }
        }

        /// <summary>
        /// The default size for this file type
        /// </summary>
        public Autogrowth DefaultAutogrowth
        {
            get
            {
                if (this.currentState.fileType == FileType.Log)
                {
                    return this.defaultLogAutogrowth;
                }
                else
                {
                    return this.defaultDataAutogrowth;
                }
            }
            set
            {
                if (this.currentState.fileType == FileType.Log)
                {
                    this.defaultLogAutogrowth = value;
                }
                else
                {
                    this.defaultDataAutogrowth = value;
                }
            }
        }

        /// <summary>
        /// Whether the file exists on the server
        /// </summary>
        public bool Exists
        {
            get { return this.fileExists; }

            set { this.fileExists = value; }
        }

        /// <summary>
        /// Whether the file was removed
        /// </summary>
        public bool Removed
        {
            get { return this.removed; }

            set
            {
                this.removed = value;
                this.database.NotifyObservers();
            }
        }

        #endregion

        /// <summary>
        /// Constructor for new files
        /// </summary>
        /// <param name="context">server information</param>
        /// <param name="database">The parent database prototype</param>
        /// <param name="type">The type of the file</param>
        public DatabaseFilePrototype(CDataContainer context,
            DatabasePrototype database,
            FileType type)
        {
            Initialize(context, database, type);
        }

        /// <summary>
        /// Constructor for new files whose name is known
        /// </summary>
        /// <param name="context">server information</param>
        /// <param name="database">The parent database prototype</param>
        /// <param name="type">The type of the file</param>
        /// <param name="name">The name of the file, without extension</param>
        public DatabaseFilePrototype(CDataContainer context,
            DatabasePrototype database,
            FileType type,
            string name)
        {
            Initialize(context, database, type);

            this.currentState.name = name;
        }

        /// <summary>
        /// Constructor for existing data files
        /// </summary>
        /// <param name="database">Prototype database containing the prototype file</param>
        /// <param name="filegroup">Prototype file group containing the prototype file</param>
        /// <param name="file">The SMO DataFile object whose definition is to be extracted</param>
        public DatabaseFilePrototype(DatabasePrototype database,
            FilegroupPrototype filegroup,
            DataFile file)
        {
            this.originalState = new FileData(database, filegroup, file);
            this.currentState = this.originalState.Clone();

            this.fileExists = true;
            this.removed = false;
            this.database = database;
        }

        /// <summary>
        /// Constructor for existing log files
        /// </summary>
        /// <param name="database">Prototype database containing the prototype file</param>
        /// <param name="file">The SMO LogFile object whose definition is to be extracted</param>
        public DatabaseFilePrototype(DatabasePrototype database, LogFile file)
        {
            this.originalState = new FileData(database, file);
            this.currentState = this.originalState.Clone();

            this.fileExists = true;
            this.removed = false;
            this.database = database;
        }

        /// <summary>
        /// Apply the changes to the database
        /// </summary>
        /// <param name="db">The database whose definition is being modified</param>
        public void ApplyChanges(Database db)
        {
            if (this.ChangesExist())
            {
                if (this.Removed)
                {
                    this.RemoveFile(db);
                }
                else
                {
                    switch (this.DatabaseFileType)
                    {
                        case FileType.Data:
                            this.CreateOrAlterDataFile(db);
                            break;

                        case FileType.Log:
                            this.CreateOrAlterLogFile(db);
                            break;

                        case FileType.FileStream:
                            this.CreateOrAlterFileStreamFile(db);
                            break;

                        default:
                            throw new InvalidOperationException("Invalid DatabaseFileType");
                    }
                }
            }
        }

        /// <summary>
        /// Would calling ApplyChanges change anything?
        /// </summary>
        /// <returns>True if changes exist, false otherwise</returns>
        public bool ChangesExist()
        {
            bool result = (
                !this.Exists ||
                this.Removed ||
                (this.currentState.fileType != this.originalState.fileType) ||
                (this.currentState.filegroup != this.originalState.filegroup) ||
                (this.currentState.isPrimaryFile != this.originalState.isPrimaryFile) ||
                (this.currentState.initialSize != this.originalState.initialSize) ||
                !this.currentState.autogrowth.HasSameValueAs(this.originalState.autogrowth) ||
                (0 != String.Compare(this.currentState.name, this.originalState.name, StringComparison.Ordinal)) ||
                (0 != String.Compare(this.currentState.folder, this.originalState.folder, StringComparison.Ordinal)));

            return result;
        }

        /// <summary>
        /// Remove an existing file from the database
        /// </summary>
        /// <param name="db">The database from which the file is to be removed</param>
        private void RemoveFile(Database db)
        {
            if (this.Exists)
            {
                if (FileType.Log == this.DatabaseFileType)
                {
                    LogFile lf = db.LogFiles[this.originalState.name];
                    if (lf != null)
                    {
                        lf.Drop();
                    }
                }
                else
                {
                    if (!this.currentState.filegroup.Removed)
                    {
                        DataFile df = db.FileGroups[this.FileGroup.Name].Files[this.originalState.name];
                        if (df != null)
                        {
                            df.Drop();
                        }
                    }
                }
            }

        }

        /// <summary>
        /// Create a data file
        /// </summary>
        /// <param name="db">The database object that is to contain the new file</param>
        private void CreateOrAlterDataFile(Database db)
        {
            // note: This method looks strikingly similar to CreateLogFile(), below.
            // The initialization code can't be shared because LogFile and DataFile have
            // no common base class, so we can't use polymorphism to our advantage.

            // form the file path, create the data file
            string fileSuffix = this.IsPrimaryFile ? ".mdf" : ".ndf";
            DataFile file = null;
            FileGroup fg = db.FileGroups[this.FileGroup.Name];

            if (this.Exists)
            {
                file = db.FileGroups[this.FileGroup.Name].Files[this.originalState.name];

                if (!file.Name.Equals(this.Name))
                {
                    file.Rename(this.Name);
                }
            }
            else
            {
                file = new DataFile(fg, this.Name);
                fg.Files.Add(file);
            }

            if (this.IsPrimaryFile && !this.Exists)
            {
                file.IsPrimaryFile = true;
            }

            // set the file name (you can't change the file name after the file has been created)
            if (!this.Exists)
            {
                file.FileName = MakeDiskFileName(this.Name, this.PhysicalName, fileSuffix);
            }

            // set its initial size
            double originalSize = this.originalState.initialSize;
            double newSize = this.currentState.initialSize;

            // if the file does not exist or if the existing file size was changed in the UI and is 
            // significantly larger than the value on the server, set the file size
            if (!this.Exists || ((1e-6 < Math.Abs(newSize - originalSize)) && (1e-6 < (newSize - file.Size))))
            {
                file.Size = newSize;
            }
            else if ((newSize < originalSize) && (newSize < file.Size))
            {
                file.Shrink(KilobytesToMegabytes(newSize), ShrinkMethod.Default);
            }

            if (!this.Exists)
            {
                // if auto-growth is enabled, set auto-growth data
                if (this.Autogrowth.IsEnabled)
                {
                    if (this.Autogrowth.IsGrowthRestricted)
                    {
                        file.MaxSize = this.Autogrowth.MaximumFileSizeInKilobytes;
                    }

                    if (this.Autogrowth.IsGrowthInPercent)
                    {
                        file.GrowthType = FileGrowthType.Percent;
                        file.Growth = (double) this.Autogrowth.GrowthInPercent;
                    }
                    else
                    {
                        file.GrowthType = FileGrowthType.KB;
                        file.Growth = this.Autogrowth.GrowthInKilobytes;
                    }
                }
                else
                {
                    file.GrowthType = FileGrowthType.None;
                }
            }
            else
            {
                FileGrowthType newFileGrowthType = FileGrowthType.None;
                FileGrowthType originalFileGrowthType = FileGrowthType.None;
                double newGrowth = 0.0;
                double originalGrowth = 0.0;
                double newMaxSize = 0.0;
                double originalMaxSize = 0.0;

                if (this.currentState.autogrowth.IsEnabled)
                {
                    if (this.currentState.autogrowth.IsGrowthRestricted)
                    {
                        newMaxSize = this.currentState.autogrowth.MaximumFileSizeInKilobytes;
                    }

                    if (this.currentState.autogrowth.IsGrowthInPercent)
                    {
                        newFileGrowthType = FileGrowthType.Percent;
                        newGrowth = (double) this.currentState.autogrowth.GrowthInPercent;
                    }
                    else
                    {
                        newFileGrowthType = FileGrowthType.KB;
                        newGrowth = this.currentState.autogrowth.GrowthInKilobytes;
                    }
                }

                if (this.originalState.autogrowth.IsEnabled)
                {
                    if (this.originalState.autogrowth.IsGrowthRestricted)
                    {
                        originalMaxSize = this.originalState.autogrowth.MaximumFileSizeInKilobytes;
                    }

                    if (this.originalState.autogrowth.IsGrowthInPercent)
                    {
                        originalFileGrowthType = FileGrowthType.Percent;
                        originalGrowth = (double) this.originalState.autogrowth.GrowthInPercent;
                    }
                    else
                    {
                        originalFileGrowthType = FileGrowthType.KB;
                        originalGrowth = this.originalState.autogrowth.GrowthInKilobytes;
                    }
                }

                // if file growth type has changed in the UI and is different from the value on the server
                if ((newFileGrowthType != originalFileGrowthType) && (file.GrowthType != newFileGrowthType))
                {
                    // change the file growth type
                    file.GrowthType = newFileGrowthType;
                }

                // if the growth amount has changed in the UI and is different from the value on the server
                if ((1e-6 < (Math.Abs(newGrowth - originalGrowth))) && (1e-6 < Math.Abs(newGrowth - file.Growth)))
                {
                    // change the growth amount
                    file.Growth = newGrowth;
                }

                // when file size is unlimited, MaxSize is non-positive.  For the
                // purposes of our code, convert non-positive to zero, then compare
                // vs. the prototype file size.
                double fileMaxSize = (0.0 <= file.MaxSize) ? file.MaxSize : 0.0;

                // if the max size has changed in the UI and is different from the value on the server
                if ((1e-6 < Math.Abs(originalMaxSize - newMaxSize)) && (1e-6 < Math.Abs(fileMaxSize - newMaxSize)))
                {
                    // set the file max size
                    file.MaxSize = newMaxSize;
                }
            }
        }

        /// <summary>
        /// Create a filestream file
        /// </summary>
        /// <param name="db">The database object that is to contain the new file</param>
        private void CreateOrAlterFileStreamFile(Database db)
        {
            // form the file path, create the data file
            DataFile file = null;
            FileGroup fg = db.FileGroups[this.FileGroup.Name];

            if (this.Exists)
            {
                file = db.FileGroups[this.FileGroup.Name].Files[this.originalState.name];

                if (!file.Name.Equals(this.Name))
                {
                    file.Rename(this.Name);
                }
            }
            else
            {
                file = new DataFile(fg, this.Name);
                fg.Files.Add(file);
            }

            // set the file name (you can't change the file name after the file has been created)
            if (!this.Exists)
            {
                // filestream files ignore the filename but if provided it just puts it
                // into the transact-sql query. If in future that query starts using the
                // filename in some manner we get that functionality automatically by
                // passing in the same name to the server. -anchals
                file.FileName = MakeDiskFileName(this.Name, this.PhysicalName, String.Empty);
            }

            if (this.database.ServerVersion.Major >= 11)
            {
                double newMaxSize = 0.0;
                double originalMaxSize = 0.0;
                if (!this.Exists)
                {
                    if (this.Autogrowth.IsGrowthRestricted)
                    {
                        file.MaxSize = this.Autogrowth.MaximumFileSizeInKilobytes;
                    }
                }
                else
                {
                    if (this.currentState.autogrowth.IsGrowthRestricted)
                    {
                        newMaxSize = this.currentState.autogrowth.MaximumFileSizeInKilobytes;
                    }
                    if (this.originalState.autogrowth.IsGrowthRestricted)
                    {
                        originalMaxSize = this.originalState.autogrowth.MaximumFileSizeInKilobytes;
                    }

                    // when file size is unlimited, MaxSize is non-positive.  For the
                    // purposes of our code, convert non-positive to zero, then compare
                    // vs. the prototype file size.
                    double fileMaxSize = (0.0 <= file.MaxSize) ? file.MaxSize : 0.0;

                    // if the max size has changed in the UI and is different from the value on the server
                    if ((1e-6 < Math.Abs(originalMaxSize - newMaxSize)) && (1e-6 < Math.Abs(fileMaxSize - newMaxSize)))
                    {
                        // set the file max size
                        file.MaxSize = newMaxSize;
                    }
                }

            }
        }

        /// <summary>
        /// Create a log file
        /// </summary>
        /// <param name="db">The database object that is to contain the new file</param>
        private void CreateOrAlterLogFile(Database db)
        {
            // note: This method looks strikingly similar to CreateDataFile(), above.
            // The initialization code can't be shared because LogFile and DataFile have
            // no common base class, so we can't use polymorphism to our advantage.

            LogFile file = null;

            if (this.Exists)
            {
                file = db.LogFiles[this.originalState.name];

                if (!file.Name.Equals(this.Name))
                {
                    file.Rename(this.Name);
                }
            }
            else
            {
                file = new LogFile(db, this.Name);
                db.LogFiles.Add(file);
            }

            // set its path and initial size
            if (!this.Exists)
            {
                file.FileName = MakeDiskFileName(this.Name, this.PhysicalName, ".ldf");
            }

            // set its initial size
            double originalSize = this.originalState.initialSize;
            double newSize = this.currentState.initialSize;

            // if the file does not exist or if the existing file size was changed in the UI and is 
            // significantly larger than the value on the server, set the file size
            if (!this.Exists || ((1e-6 < Math.Abs(newSize - originalSize)) && (1e-6 < (newSize - file.Size))))
            {
                file.Size = newSize;
            }
            else if ((newSize < originalSize) && (newSize < file.Size))
            {
                file.Shrink(KilobytesToMegabytes(newSize), ShrinkMethod.Default);
            }


            // if auto-growth is enabled, set auto-growth data
            if (!this.Exists)
            {
                // if auto-growth is enabled, set auto-growth data
                if (this.Autogrowth.IsEnabled)
                {
                    if (this.Autogrowth.IsGrowthRestricted)
                    {
                        file.MaxSize = this.Autogrowth.MaximumFileSizeInKilobytes;
                    }

                    if (this.Autogrowth.IsGrowthInPercent)
                    {
                        file.GrowthType = FileGrowthType.Percent;
                        file.Growth = (double) this.Autogrowth.GrowthInPercent;
                    }
                    else
                    {
                        file.GrowthType = FileGrowthType.KB;
                        file.Growth = this.Autogrowth.GrowthInKilobytes;
                    }
                }
                else
                {
                    file.GrowthType = FileGrowthType.None;
                }
            }
            else
            {
                FileGrowthType newFileGrowthType = FileGrowthType.None;
                FileGrowthType originalFileGrowthType = FileGrowthType.None;
                double newGrowth = 0.0;
                double originalGrowth = 0.0;
                double newMaxSize = 0.0;
                double originalMaxSize = 0.0;

                if (this.currentState.autogrowth.IsEnabled)
                {
                    if (this.currentState.autogrowth.IsGrowthRestricted)
                    {
                        newMaxSize = this.currentState.autogrowth.MaximumFileSizeInKilobytes;
                    }

                    if (this.currentState.autogrowth.IsGrowthInPercent)
                    {
                        newFileGrowthType = FileGrowthType.Percent;
                        newGrowth = (double) this.currentState.autogrowth.GrowthInPercent;
                    }
                    else
                    {
                        newFileGrowthType = FileGrowthType.KB;
                        newGrowth = this.currentState.autogrowth.GrowthInKilobytes;
                    }
                }

                if (this.originalState.autogrowth.IsEnabled)
                {
                    if (this.originalState.autogrowth.IsGrowthRestricted)
                    {
                        originalMaxSize = this.originalState.autogrowth.MaximumFileSizeInKilobytes;
                    }

                    if (this.originalState.autogrowth.IsGrowthInPercent)
                    {
                        originalFileGrowthType = FileGrowthType.Percent;
                        originalGrowth = (double) this.originalState.autogrowth.GrowthInPercent;
                    }
                    else
                    {
                        originalFileGrowthType = FileGrowthType.KB;
                        originalGrowth = this.originalState.autogrowth.GrowthInKilobytes;
                    }
                }

                // if file growth type has changed in the UI and is different from the value on the server
                if ((newFileGrowthType != originalFileGrowthType) && (file.GrowthType != newFileGrowthType))
                {
                    // change the file growth type
                    file.GrowthType = newFileGrowthType;
                }

                // if the growth amount has changed in the UI and is different from the value on the server
                if ((1e-6 < (Math.Abs(newGrowth - originalGrowth))) && (1e-6 < Math.Abs(newGrowth - file.Growth)))
                {
                    // change the growth amount
                    file.Growth = newGrowth;
                }

                // when file size is unlimited, MaxSize is non-positive.  For the
                // purposes of our code, convert non-positive to zero, then compare
                // vs. the prototype file size.
                double fileMaxSize = (0.0 <= file.MaxSize) ? file.MaxSize : 0.0;

                // if the max size has changed in the UI and is different from the value on the server
                if ((1e-6 < Math.Abs(originalMaxSize - newMaxSize)) && (1e-6 < Math.Abs(fileMaxSize - newMaxSize)))
                {
                    // set the file max size
                    file.MaxSize = newMaxSize;
                }
            }
        }

        /// <summary>
        /// Shared construction code for new files
        /// </summary>
        /// <param name="context"></param>
        /// <param name="database">The parent database prototype</param>
        /// <param name="type">The type of the file</param>
        private void Initialize(CDataContainer context, DatabasePrototype database, FileType type)
        {
            this.originalState = new FileData(database, type);


            this.fileExists = false;
            this.removed = false;
            this.database = database;

            GetDefaultValues(context);
            GetDefaultAutoGrowthValues(context);

            if (FileType.Data == type)
            {
                InitializeDataFile(context);
            }
            else if (FileType.Log == type)
            {
                InitializeLogFile(context);
            }

            this.currentState = this.originalState.Clone();
        }

        /// <summary>
        /// Get the default folder for log and data files
        /// </summary>
        private void GetDefaultValues(CDataContainer context)
        {
            Enumerator enumerator = new Enumerator();
            Request request = new Request();
            //object connectionInfo = context.ConnectionInfo;
            object connectionInfo = null;
            DataTable fileInfo = null;
            double size;

            if (null != context.Server)
            {
                connectionInfo = context.Server.ConnectionContext;
            }
            else
            {
                connectionInfo = context.ConnectionInfo;
            }

            // get default data file size
            request.Urn = "Server/Database[@Name='model']/FileGroup[@Name='PRIMARY']/File";
            request.Fields = new String[1] {"Size"};

            try
            {
                fileInfo = enumerator.Process(connectionInfo, request);
                size = Convert.ToDouble(fileInfo.Rows[0][0], System.Globalization.CultureInfo.InvariantCulture);

                // file size returned by the enumerator is in kilobytes but the dialog displays
                // file size in megabytes

                defaultDataFileSize = DatabaseFilePrototype.RoundUpToNearestMegabyte(size);
            }
            catch (Exception ex)
            {
                // user doesn't have access to model so we set the default size
                // to be 5 MB
                defaultDataFileSize = 5120.0;
            }

            // get default log file size
            request.Urn = "Server/Database[@Name='model']/LogFile";
            request.Fields = new String[1] {"Size"};

            try
            {
                fileInfo = enumerator.Process(connectionInfo, request);
                size = Convert.ToDouble(fileInfo.Rows[0][0], System.Globalization.CultureInfo.InvariantCulture);

                defaultLogFileSize = DatabaseFilePrototype.RoundUpToNearestMegabyte(size);
            }
            catch (Exception ex)
            {
                // user doesn't have access to model so we set the default size
                // to be 1MB
                defaultLogFileSize = 1024.0;
            }

            // get default data and log folders
            request.Urn = "Server/Setting";
            request.Fields = new String[] {"DefaultFile", "DefaultLog"};

            try
            {
                fileInfo = enumerator.Process(connectionInfo, request);
                defaultDataFolder = fileInfo.Rows[0]["DefaultFile"].ToString();
                defaultLogFolder = fileInfo.Rows[0]["DefaultLog"].ToString();

                if (defaultDataFolder.Length == 0 || defaultLogFolder.Length == 0)
                {
                    request.Urn = "Server/Information";
                    request.Fields = new string[] {"MasterDBPath", "MasterDBLogPath"};
                    fileInfo = enumerator.Process(connectionInfo, request);

                    if (defaultDataFolder.Length == 0)
                    {
                        defaultDataFolder = fileInfo.Rows[0]["MasterDBPath"].ToString();
                    }

                    if (defaultLogFolder.Length == 0)
                    {
                        defaultLogFolder = fileInfo.Rows[0]["MasterDBLogPath"].ToString();
                    }
                }

                if ((3 <= defaultDataFolder.Length) && (defaultDataFolder[1] == ':') && (defaultDataFolder[2] != '\\'))
                {
                    string drivePart = defaultDataFolder.Substring(0, 2);
                    string rest = defaultDataFolder.Substring(2);

                    defaultDataFolder = String.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "{0}\\{1}",
                        drivePart,
                        rest);
                }


                if ((3 <= defaultLogFolder.Length) && (defaultLogFolder[1] == ':') && (defaultLogFolder[2] != '\\'))
                {
                    string drivePart = defaultLogFolder.Substring(0, 2);
                    string rest = defaultLogFolder.Substring(2);

                    defaultLogFolder = String.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "{0}\\{1}",
                        drivePart,
                        rest);
                }
            }
            catch (Exception ex)
            {
            }
        }

        /// <summary>
        /// Get the default Autogrowth values for log or data files
        /// </summary>
        /// <param name="context"></param>
        private void GetDefaultAutoGrowthValues(CDataContainer context)
        {
            // get autogrowth information
            defaultDataAutogrowth = new Autogrowth(this.database);

            // try to get defaults from model database
            try
            {
                // copy default data size and autogrowth from the model database
                Database model = context.Server.Databases["model"];
                FileGroup filegroup = model.FileGroups["PRIMARY"];
                DataFile datafile = filegroup.Files[0];

                defaultDataAutogrowth.IsEnabled = (datafile.GrowthType != FileGrowthType.None);

                if (defaultDataAutogrowth.IsEnabled)
                {
                    defaultDataAutogrowth.MaximumFileSizeInKilobytes = datafile.MaxSize;
                    defaultDataAutogrowth.IsGrowthRestricted = (0.0 < defaultDataAutogrowth.MaximumFileSizeInKilobytes);
                    defaultDataAutogrowth.IsGrowthInPercent = datafile.GrowthType == FileGrowthType.Percent;

                    if (!defaultDataAutogrowth.IsGrowthRestricted)
                    {
                        // we need to set a default maximum size for the file in the
                        // event the user changes to restricted growth.  0.0 is not
                        // a valid maximum file size.

                        defaultDataAutogrowth.MaximumFileSizeInKilobytes = 102400.0;
                    }

                    if (defaultDataAutogrowth.IsGrowthInPercent)
                    {
                        defaultDataAutogrowth.GrowthInPercent = (int) datafile.Growth;
                        defaultDataAutogrowth.GrowthInMegabytes = 10;
                    }
                    else
                    {
                        defaultDataAutogrowth.GrowthInKilobytes =
                            DatabaseFilePrototype.RoundUpToNearestMegabyte(datafile.Growth);
                        defaultDataAutogrowth.GrowthInPercent = 10;
                    }
                }
            }
            catch (Exception)
            {
                // there was an error getting information about the model database,
                // so just set the default sizes to default values
                defaultDataAutogrowth.Reset();
            }
            // get default autogrowth information
            defaultLogAutogrowth = new Autogrowth(this.database);

            // try to get defaults from model database
            try
            {
                // copy default data size and autogrowth from the model database
                Database model = context.Server.Databases["model"];
                LogFile logfile = model.LogFiles[0];

                defaultLogAutogrowth.IsEnabled = (logfile.GrowthType != FileGrowthType.None);

                if (defaultLogAutogrowth.IsEnabled)
                {
                    defaultLogAutogrowth.MaximumFileSizeInKilobytes = logfile.MaxSize;
                    defaultLogAutogrowth.IsGrowthRestricted = (0.0 < defaultLogAutogrowth.MaximumFileSizeInKilobytes);
                    defaultLogAutogrowth.IsGrowthInPercent = logfile.GrowthType == FileGrowthType.Percent;

                    if (!defaultLogAutogrowth.IsGrowthRestricted)
                    {
                        // we need to set a default maximum size for the file in the
                        // event the user changes to restricted growth.  0.0 is not
                        // a valid maximum file size.

                        defaultLogAutogrowth.MaximumFileSizeInKilobytes = 102400.0;
                    }

                    if (defaultLogAutogrowth.IsGrowthInPercent)
                    {
                        defaultLogAutogrowth.GrowthInPercent = (int) logfile.Growth;
                        defaultLogAutogrowth.GrowthInMegabytes = 10;
                    }
                    else
                    {
                        defaultLogAutogrowth.GrowthInKilobytes =
                            DatabaseFilePrototype.RoundUpToNearestMegabyte(logfile.Growth);
                        defaultLogAutogrowth.GrowthInPercent = 10;
                    }
                }
            }
            catch (Exception)
            {
                // there was an error getting information about the model database,
                // so just set the default sizes to default values
                defaultLogAutogrowth.Reset();
            }
        }

        /// <summary>
        /// Initialize a data file, called during construction
        /// </summary>
        /// <param name="context"></param>
        private void InitializeDataFile(CDataContainer context)
        {
            // set our state
            this.originalState.folder = this.defaultDataFolder;
            this.usingDefaultFolder = true;
            this.originalState.autogrowth = defaultDataAutogrowth;
            this.originalState.initialSize = this.defaultDataFileSize;
            this.originalState.filegroup = this.database.DefaultFilegroup;

            this.database.DefaultFilegroup.OnFileGroupDeletedHandler +=
                new FileGroupDeletedEventHandler(OnFilegroupDeleted);
        }

        /// <summary>
        /// Initialize a new log file prototype, called during construction
        /// </summary>
        /// <param name="context"></param>
        private void InitializeLogFile(CDataContainer context)
        {
            // set our state
            this.originalState.folder = this.defaultLogFolder;
            this.usingDefaultFolder = true;
            this.originalState.filegroup = null;
            this.originalState.autogrowth = defaultLogAutogrowth;
            this.originalState.initialSize = this.defaultLogFileSize;
        }

        /// <summary>
        /// Handle deleted events from the filegroup that contains the file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnFilegroupDeleted(object sender, FilegroupDeletedEventArgs e)
        {           
            e.DeletedFilegroup.OnFileGroupDeletedHandler -= new FileGroupDeletedEventHandler(OnFilegroupDeleted);

            // SQL Server deletes all the files in a filegroup when the filegroup is removed
            if (this.Exists)
            {
                this.database.Remove(this);
            }
            else
            {
                this.FileGroup = e.DefaultFilegroup;
            }
        }

        /// <summary>
        /// Check whether a proposed file name is valid.  An exception is thrown if the check fails.
        /// </summary>
        /// <param name="fileName">The proposed file name to check</param>
        private void CheckFileName(string fileName)
        {
            char[] badFileCharacters = new char[] {'\\', '/', ':', '*', '?', '\"', '<', '>', '|'};

            bool isAllWhitespace = (fileName.Trim(null).Length == 0);

            if (isAllWhitespace || (0 == fileName.Length) || (-1 != fileName.IndexOfAny(badFileCharacters)))
            {
                ResourceManager resourceManager =
                    new ResourceManager("Microsoft.SqlServer.Management.SqlManagerUI.CreateDatabaseStrings",
                        this.GetType().GetAssembly());

                string message = String.Empty;

                if (0 == fileName.Length)
                {
                    message = resourceManager.GetString("error.emptyFileName");
                }
                else if (isAllWhitespace)
                {
                    message = resourceManager.GetString("error.whitespaceDatabaseName");
                }
                else
                {
                    int i = fileName.IndexOfAny(badFileCharacters);

                    message = String.Format(System.Globalization.CultureInfo.CurrentCulture,
                        resourceManager.GetString("error.fileNameContainsIllegalCharacter"), fileName, fileName[i]);
                }

                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// Get the number of megabytes equivalent to an input number of kilobytes, rounding up.
        /// </summary>
        /// <param name="kilobytes">The number of kilobytes to convert</param>
        /// <returns>The equivalent number of megabytes</returns>
        internal static int KilobytesToMegabytes(double kilobytes)
        {
            return (int) Math.Ceiling(kilobytes/kilobytesPerMegabyte);
        }

        /// <summary>
        /// Get the number of kilobytes equivalent to an input number of megabytes.
        /// </summary>
        /// <param name="megabytes">The number of megabytes to convert</param>
        /// <returns>The equivalent number of kilobytes</returns>
        internal static double MegabytesToKilobytes(int megabytes)
        {
            return (((double) megabytes)*kilobytesPerMegabyte);
        }

        /// <summary>
        /// Get the number of kilobytes that is a round number of megabytes
        /// larger than the input number of kilobytes.  e.g. 1600 kb -> 2048 kb
        /// </summary>
        /// <param name="kilobytes">The number of kilobytes to round up</param>
        /// <returns>The number of kb in the next larger mb</returns>
        internal static double RoundUpToNearestMegabyte(double kilobytes)
        {
            double megabytes = Math.Ceiling(kilobytes/kilobytesPerMegabyte);
            return (megabytes*kilobytesPerMegabyte);
        }

        /// <summary>
        /// Create the Physical file name for the data file to be created on disk
        /// from user preferred logical and physical Name. This also verifies the
        /// logical file name provided to the data file.
        /// If a valid physical name is provided then that is returned with proper extension.
        /// Else logical name is used.
        /// 
        /// MakeDiskFileName returns the actual physical filename that could be used for the T-SQL query
        /// It makes sure:
        /// 1. provide a way to provide different physical and logical file names to a database file.
        /// 2. Its optional if preferred physical name is blank then logical name is used to generate the physical file name. (logical name and
        ///    physical filename are then same. (except that the physical filename has a proper extension)
        /// 3. since logical file name check is not that stringent in t-sql; this check is relaxed only when a physical name is explicitly provided.
        /// 4. we don't enable the physical file name input control for filestream files since the filename passed to t-sql in this case is simply ignored. Internally
        ///    we evaluate the final file name in case of filestreams also in the same manner for consistency (and if in future t-sql starts using the physical filename
        ///    apart from the path.)
        /// 5. Also if the preferred physical filename is without a file extension then default extension is appended.
        ///    Otherwise the user defined extension is used.
        /// </summary>
        /// <param name="logicalName">Logical name of the data file. Cannot be blank. Its verified.</param>
        /// <param name="preferredPhysicalName">User Preferred Physical name of the file. Can be blank.</param>
        /// <param name="suffix">Preferred suffix of the file. can be String.Empty. If provided physical name doesn't
        /// have an extension or is empty then this is used.</param>
        /// <returns>full filename for the disk. The filename
        /// is prefixed with this.Folder path to generate the full name.</returns>
        /// <exception cref="InvalidOperationException">If logical name is empty, or physical name is invalid.</exception>
        private string MakeDiskFileName(string logicalName, string preferredPhysicalName, string suffix)
        {
            ResourceManager resourceManager =
                new ResourceManager("Microsoft.SqlServer.Management.SqlManagerUI.CreateDatabaseStrings",
                    this.GetType().GetAssembly());

            string filePath = String.Empty; // returned to the caller.
            if (String.IsNullOrEmpty(preferredPhysicalName))
            {
                // make the file name using the logical name and suffix provided.
                this.CheckFileName(logicalName);
                filePath = PathWrapper.Combine(this.Folder, logicalName) + suffix;
            }
            else
            {
                // do sanity check on name since logical name must exist always.
                // The check for logical name is not as stringent as that for physical file name. This is because
                // transact sql allows special characters to be present in logical filename but don't allow them
                // as physical filename. e.g. '?' can be a valid logical name but not a valid physical name. [anchals]
                if (logicalName.Length == 0)
                {
                    string message = String.Empty;

                    message = resourceManager.GetString("error.emptyFileName");
                    throw new InvalidOperationException(message);
                }

                // validate provided physical name and if it does not have an extension
                // append the suffix to it.
                this.CheckFileName(preferredPhysicalName);
                filePath = PathWrapper.Combine(this.Folder, preferredPhysicalName);
            }
            return filePath;
        }
    }

    /// <summary>
    /// Information regarding a name-change event
    /// </summary>
    public class NameChangedEventArgs : EventArgs
    {
        private string oldName;
        private string newName;

        /// <summary>
        /// The name before the change
        /// </summary>
        public string OldName
        {
            get { return oldName; }
        }

        /// <summary>
        /// The name after the change
        /// </summary>
        public string NewName
        {
            get { return newName; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="oldName">The name before the change</param>
        /// <param name="newName">The name after the change</param>
        public NameChangedEventArgs(string oldName, string newName)
        {
            this.oldName = oldName;
            this.newName = newName;
        }
    }

    /// <summary>
    /// Information regarding a changes to boolean property values
    /// </summary>
    public class BooleanValueChangedEventArgs : EventArgs
    {
        private bool oldValue;
        private bool newValue;

        /// <summary>
        /// The value before the change
        /// </summary>
        public bool OldValue
        {
            get { return oldValue; }
        }

        /// <summary>
        /// The value after the change
        /// </summary>
        public bool NewValue
        {
            get { return newValue; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="oldValue">The value before the change</param>
        /// <param name="newValue">The value after the change</param>
        public BooleanValueChangedEventArgs(bool oldValue, bool newValue)
        {
            this.oldValue = oldValue;
            this.newValue = newValue;
        }
    }

    /// <summary>
    /// Information regarding the deletion of a filegroup
    /// </summary>
    public class FilegroupDeletedEventArgs : EventArgs
    {
        private FilegroupPrototype defaultFilegroup;
        private FilegroupPrototype deletedFilegroup;

        /// <summary>
        /// The default filegroup for the database
        /// </summary>
        public FilegroupPrototype DefaultFilegroup
        {
            get { return defaultFilegroup; }
        }

        /// <summary>
        /// The filegroup that was deleted
        /// </summary>
        public FilegroupPrototype DeletedFilegroup
        {
            get { return deletedFilegroup; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="deletedFilegroup">The filegroup that was deleted</param>
        /// <param name="defaultFilegroup">The default filegroup for the database</param>
        public FilegroupDeletedEventArgs(FilegroupPrototype deletedFilegroup, FilegroupPrototype defaultFilegroup)
        {
            this.deletedFilegroup = deletedFilegroup;
            this.defaultFilegroup = defaultFilegroup;
        }
    }

    public delegate void FileGroupNameChangedEventHandler(object sender, NameChangedEventArgs e);

    public delegate void FileGroupDeletedEventHandler(object sender, FilegroupDeletedEventArgs e);

    public delegate void FileGroupDefaultChangedEventHandler(object sender, BooleanValueChangedEventArgs e);

    internal class DatabaseAlreadyExistsException : Exception
    {
        private static string format;

        static DatabaseAlreadyExistsException()
        {
            ResourceManager resourceManager =
                new ResourceManager("Microsoft.SqlServer.Management.SqlManagerUI.CreateDatabaseStrings",
                    typeof (DatabaseAlreadyExistsException).GetAssembly());
            format = resourceManager.GetString("error.databaseAlreadyExists");
        }

        public DatabaseAlreadyExistsException(string databaseName)
            : base(String.Format(System.Globalization.CultureInfo.CurrentCulture, format, databaseName))
        {
        }
    }

    internal class DefaultCursorTypes : StringConverter
    {
        /// <summary>
        /// This method returns a list of default cursor Types
        /// which will be populated as a drop down list.
        /// </summary>
        /// <param name="context"></param>
        /// <returns>List of DefaultCursor Types </returns>
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            ResourceManager manager =
                new ResourceManager("Microsoft.SqlServer.Management.SqlManagerUI.CreateDatabaseStrings",
                    typeof (DatabasePrototype).GetAssembly());
            List<string> standardValues = null;
            TypeConverter.StandardValuesCollection result = null;

            if (
                string.Compare(context.PropertyDescriptor.Name, "DefaultCursorDisplay",
                    StringComparison.OrdinalIgnoreCase) == 0)
            {
                standardValues = new List<string>();
                standardValues.Add(manager.GetString("prototype.db.prop.defaultCursor.value.local"));
                standardValues.Add(manager.GetString("prototype.db.prop.defaultCursor.value.global"));
            }
            if (standardValues != null)
            {
                result = new TypeConverter.StandardValuesCollection(standardValues);
            }

            return result;
        }

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }
    }

    internal class ParameterizationTypes : StringConverter
    {
        /// <summary>
        /// This method returns a list of parameterization Types
        /// which will be populated as a drop down list.
        /// </summary>
        /// <param name="context"></param>
        /// <returns>List of Parameterization Types </returns>
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            ResourceManager manager =
                new ResourceManager("Microsoft.SqlServer.Management.SqlManagerUI.CreateDatabaseStrings",
                    typeof (DatabasePrototype90).GetAssembly());
            List<string> standardValues = new List<string>();
            TypeConverter.StandardValuesCollection result = null;

            if (
                string.Compare(context.PropertyDescriptor.Name, "Parameterization", StringComparison.OrdinalIgnoreCase) ==
                0)
            {
                standardValues.Add(manager.GetString("prototype.db.prop.parameterization.value.forced"));
                standardValues.Add(manager.GetString("prototype.db.prop.parameterization.value.simple"));
            }
            if (standardValues.Count > 0)
            {
                result = new TypeConverter.StandardValuesCollection(standardValues);
            }

            return result;
        }

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }
    }

    internal class PageVerifyTypes80 : StringConverter
    {
        /// <summary>
        /// This method returns a list of pageverify Types
        /// which will be populated as a drop down list.
        /// </summary>
        /// <param name="context"></param>
        /// <returns>List of Page Verify Types </returns>
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            ResourceManager manager =
                new ResourceManager("Microsoft.SqlServer.Management.SqlManagerUI.CreateDatabaseStrings",
                    typeof (DatabasePrototype80).GetAssembly());
            List<string> standardValues = new List<string>();
            TypeConverter.StandardValuesCollection result = null;

            if (
                string.Compare(context.PropertyDescriptor.Name, "PageVerifyDisplay", StringComparison.OrdinalIgnoreCase) ==
                0)
            {
                standardValues.Add(manager.GetString("prototype.db.prop.pageVerify.value.tornPageDetection"));
                standardValues.Add(manager.GetString("prototype.db.prop.pageVerify.value.none"));
            }

            if (standardValues.Count > 0)
            {
                result = new TypeConverter.StandardValuesCollection(standardValues);
            }

            return result;
        }

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }
    }


    internal class PageVerifyTypes90 : StringConverter
    {
        /// <summary>
        /// This method returns a list of pageverify Types
        /// which will be populated as a drop down list.
        /// </summary>
        /// <param name="context"></param>
        /// <returns>List of Page Verify Types </returns>
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            ResourceManager manager =
                new ResourceManager("Microsoft.SqlServer.Management.SqlManagerUI.CreateDatabaseStrings",
                    typeof (DatabasePrototype80).GetAssembly());
            List<string> standardValues = new List<string>();
            TypeConverter.StandardValuesCollection result = null;

            if (
                string.Compare(context.PropertyDescriptor.Name, "PageVerifyDisplay", StringComparison.OrdinalIgnoreCase) ==
                0)
            {
                standardValues.Add(manager.GetString("prototype.db.prop.pageVerify.value.checksum"));
                standardValues.Add(manager.GetString("prototype.db.prop.pageVerify.value.tornPageDetection"));
                standardValues.Add(manager.GetString("prototype.db.prop.pageVerify.value.none"));
            }

            if (standardValues.Count > 0)
            {
                result = new TypeConverter.StandardValuesCollection(standardValues);
            }

            return result;
        }

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }
    }

    internal class RestrictAccessTypes : StringConverter
    {
        /// <summary>
        /// This method returns a list of Access Types
        /// which will be populated as a drop down list.
        /// </summary>
        /// <param name="context"></param>
        /// <returns>List of Restrict Access Types </returns>
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            ResourceManager manager =
                new ResourceManager("Microsoft.SqlServer.Management.SqlManagerUI.CreateDatabaseStrings",
                    typeof (DatabasePrototype).GetAssembly());
            List<string> standardValues = new List<string>();
            TypeConverter.StandardValuesCollection result = null;

            if (string.Compare(context.PropertyDescriptor.Name, "RestrictAccess", StringComparison.OrdinalIgnoreCase) ==
                0)
            {
                standardValues.Add(manager.GetString("prototype.db.prop.restrictAccess.value.multiple"));
                standardValues.Add(manager.GetString("prototype.db.prop.restrictAccess.value.single"));
                standardValues.Add(manager.GetString("prototype.db.prop.restrictAccess.value.restricted"));
            }
            if (standardValues.Count > 0)
            {
                result = new TypeConverter.StandardValuesCollection(standardValues);
            }

            return result;
        }

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }
    }

    internal class DatabaseStatusTypes : StringConverter
    {
        /// <summary>
        /// This method returns a list of database status Types
        /// which will be populated as a drop down list.
        /// </summary>
        /// <param name="context"></param>
        /// <returns>List of Database Status Types </returns>
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            ResourceManager manager =
                new ResourceManager("Microsoft.SqlServer.Management.SqlManagerUI.CreateDatabaseStrings",
                    typeof (DatabasePrototype).GetAssembly());
            List<string> standardValues = new List<string>();
            TypeConverter.StandardValuesCollection result = null;

            if (
                string.Compare(context.PropertyDescriptor.Name, "DatabaseStatusDisplay",
                    StringComparison.OrdinalIgnoreCase) == 0)
            {
                standardValues.Add(manager.GetString("prototype.db.prop.databaseState.value.normal"));
                standardValues.Add(manager.GetString("prototype.db.prop.databaseState.value.restoring"));
                standardValues.Add(manager.GetString("prototype.db.prop.databaseState.value.recoveryPending"));
                standardValues.Add(manager.GetString("prototype.db.prop.databaseState.value.recovering"));
                standardValues.Add(manager.GetString("prototype.db.prop.databaseState.value.suspect"));
                standardValues.Add(manager.GetString("prototype.db.prop.databaseState.value.offline"));
                standardValues.Add(manager.GetString("prototype.db.prop.databaseState.value.inaccessible"));
                standardValues.Add(manager.GetString("prototype.db.prop.databaseState.value.standby"));
                standardValues.Add(manager.GetString("prototype.db.prop.databaseState.value.shutdown"));
                standardValues.Add(manager.GetString("prototype.db.prop.databaseState.value.emergency"));
                standardValues.Add(manager.GetString("prototype.db.prop.databaseState.value.autoClosed"));
            }
            if (standardValues.Count > 0)
            {
                result = new TypeConverter.StandardValuesCollection(standardValues);
            }

            return result;
        }

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }
    }

    ///// <summary>
    ///// Helper class to provide standard values for populating drop down boxes on 
    ///// properties displayed in the Properties Grid
    ///// </summary>
    //internal class DynamicValuesConverter : StringConverter
    //{
    //    /// <summary>
    //    /// This method returns a list of dynamic values
    //    /// for various Properties in this class. 
    //    /// </summary>
    //    /// <param name="context"></param>
    //    /// <returns>List of Database Status Types </returns>
    //    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
    //    {
    //        var standardValues = new List<object>();
    //        StandardValuesCollection result = null;
    
    //        //Handle ServiceLevelObjective values
    //        if (context.PropertyDescriptor != null &&
    //            string.Compare(context.PropertyDescriptor.Name, "CurrentServiceLevelObjective",
    //                StringComparison.OrdinalIgnoreCase) == 0)
    //        {             
    //            var designableObject = context.Instance as DesignableObject;
    //            if (designableObject != null)
    //            {
    //                var prototype = designableObject.ObjectDesigned as DatabasePrototypeAzure;
    //                if (prototype != null)
    //                {
    //                    KeyValuePair<int, string[]> pair;
    //                    if (AzureSqlDbHelper.TryGetServiceObjectiveInfo(prototype.AzureEdition, out pair))
    //                    {
    //                        standardValues.AddRange(pair.Value);
    //                    }
    //                }                   
    //            }
    //        }
    //        //Handle AzureEditionDisplay values
    //        else if (context.PropertyDescriptor != null &&
    //                 string.Compare(context.PropertyDescriptor.Name, "AzureEditionDisplay",
    //                     StringComparison.OrdinalIgnoreCase) == 0)
    //        {
    //            var designableObject = context.Instance as DesignableObject;

    //            if (designableObject != null)
    //            {
    //                var prototype = designableObject.ObjectDesigned as DatabasePrototype;
    //                if (prototype != null)
    //                {
    //                    foreach (
    //                        AzureEdition edition in
    //                            AzureSqlDbHelper.GetValidAzureEditionOptions(prototype.ServerVersion))
    //                    {
    //                        // We don't yet support creating DW with the UI 
    //                        if (prototype.Exists || edition != AzureEdition.DataWarehouse)
    //                        {
    //                            standardValues.Add(AzureSqlDbHelper.GetAzureEditionDisplayName(edition));
    //                        }
    //                    }
    //                }
    //                else
    //                {
    //                    STrace.Assert(false,
    //                        "DesignableObject ObjectDesigned isn't a DatabasePrototype for AzureEditionDisplay StandardValues");
    //                }
    //            }
    //            else
    //            {
    //                STrace.Assert(designableObject != null,
    //                    "Context instance isn't a DesignableObject for AzureEditionDisplay StandardValues");
    //            }
    //        }
    //        //Handle MaxSize values
    //        else if (context.PropertyDescriptor != null &&
    //                 string.Compare(context.PropertyDescriptor.Name, "MaxSize", StringComparison.OrdinalIgnoreCase) == 0)
    //        {
    //            var designableObject = context.Instance as DesignableObject;
    //            if (designableObject != null)
    //            {

    //                var prototype = designableObject.ObjectDesigned as DatabasePrototypeAzure;
    //                if (prototype != null)
    //                {
    //                    KeyValuePair<int, DbSize[]> pair;
    //                    if (AzureSqlDbHelper.TryGetDatabaseSizeInfo(prototype.AzureEdition, out pair))
    //                    {
    //                        standardValues.AddRange(pair.Value);
    //                    }
    //                }
    //            }
               
    //        }

    //        if (standardValues.Count > 0)
    //        {
    //            result = new StandardValuesCollection(standardValues);
    //        }

    //        return result;
    //    }

    //    public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
    //    {
    //        //Tells the grid that we'll support the values to display in a drop down
    //        return true;
    //    }

    //    public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
    //    {
    //        //The values are exclusive (populated in a drop-down list instead of combo box)
    //        return true;
    //    }
    //}

    ///// <summary>
    ///// Helper class to provide standard values for populating drop down boxes on 
    ///// database scoped configuration properties displayed in the Properties Grid
    ///// </summary>
    //internal class DatabaseScopedConfigurationOnOffTypes : StringConverter
    //{
    //    /// <summary>
    //    /// This method returns a list of database scoped configuration on off values
    //    /// which will be populated as a drop down list.
    //    /// </summary>
    //    /// <param name="context"></param>
    //    /// <returns>Database scoped configurations which will populate the drop down list.</returns>
    //    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
    //    {
    //        ResourceManager manager =
    //            new ResourceManager("Microsoft.SqlServer.Management.SqlManagerUI.CreateDatabaseStrings",
    //                typeof (DatabasePrototype).GetAssembly());
    //        List<string> standardValues = new List<string>();
    //        TypeConverter.StandardValuesCollection result = null;

    //        if (
    //            string.Compare(context.PropertyDescriptor.Name, "LegacyCardinalityEstimationDisplay",
    //                StringComparison.OrdinalIgnoreCase) == 0 ||
    //            string.Compare(context.PropertyDescriptor.Name, "ParameterSniffingDisplay",
    //                StringComparison.OrdinalIgnoreCase) == 0 ||
    //            string.Compare(context.PropertyDescriptor.Name, "QueryOptimizerHotfixesDisplay",
    //                StringComparison.OrdinalIgnoreCase) == 0)
    //        {
    //            standardValues.Add(manager.GetString("prototype.db.prop.databasescopedconfig.value.off"));
    //            standardValues.Add(manager.GetString("prototype.db.prop.databasescopedconfig.value.on"));
    //        }
    //        else
    //        {
    //            standardValues.Add(manager.GetString("prototype.db.prop.databasescopedconfig.value.off"));
    //            standardValues.Add(manager.GetString("prototype.db.prop.databasescopedconfig.value.on"));
    //            standardValues.Add(manager.GetString("prototype.db.prop.databasescopedconfig.value.primary"));
    //        }

    //        if (standardValues.Count > 0)
    //        {
    //            result = new TypeConverter.StandardValuesCollection(standardValues);
    //        }

    //        return result;
    //    }

    //    public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
    //    {
    //        //Tells the grid that we'll support the values to display in a drop down
    //        return true;
    //    }

    //    public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
    //    {
    //        //The values are exclusive (populated in a drop-down list instead of combo box)
    //        return true;
    //    }
    //}
}