//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.SqlTools.ServiceLayer.Management;
using static Microsoft.SqlTools.ServiceLayer.Management.DbSize;

namespace Microsoft.SqlTools.ServiceLayer.Admin
{
    public static class AzureSqlDbHelper
    {
        /// <summary>
        /// Contains the various editions available for an Azure Database
        /// The implementation is opaque to consumers
        /// </summary>
        [DebuggerDisplay("{Name,nq}")]
        public class AzureEdition
        {
            public static readonly AzureEdition Basic = new AzureEdition("Basic", "SR.BasicAzureEdition");
            public static readonly AzureEdition Standard = new AzureEdition("Standard", "SR.StandardAzureEdition");
            public static readonly AzureEdition Premium = new AzureEdition("Premium", "SR.PremiumAzureEdition");
            public static readonly AzureEdition DataWarehouse = new AzureEdition("DataWarehouse", "SR.DataWarehouseAzureEdition");
            public static readonly AzureEdition GeneralPurpose = new AzureEdition("GeneralPurpose", "SR.GeneralPurposeAzureEdition");
            public static readonly AzureEdition BusinessCritical = new AzureEdition("BusinessCritical", "SR.BusinessCriticalAzureEdition");

            public static readonly AzureEdition Hyperscale = new AzureEdition("Hyperscale", "SR.HyperscaleAzureEdition");
            // Free does not offer DatabaseSize >=1GB, hence it's not "supported".
            //public static readonly AzureEdition Free = new AzureEdition("Free", SR.FreeAzureEdition);
            // Stretch and system do not seem to be applicable, so I'm commenting them out
            //public static readonly AzureEdition Stretch = new AzureEdition("Stretch", SR.StretchAzureEdition);
            //public static readonly AzureEdition System = new AzureEdition("System", SR.SystemAzureEdition);

            internal string Name { get; private set; }
            internal string DisplayName { get; private set; }

            internal AzureEdition(string name, string displayName)
            {
                Name = name;
                DisplayName = displayName;
            }

            public override int GetHashCode()
            {
                return Name.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                return obj is AzureEdition && ((AzureEdition)obj).Name.Equals(Name);
            }

            public static bool operator ==(AzureEdition left, AzureEdition right)
            {
                return ReferenceEquals(left, right) || ((object)left != null && left.Equals(right));
            }

            public static bool operator !=(AzureEdition left, AzureEdition right)
            {
                return !(left == right);
            }

            public override string ToString()
            {
                return Name;
            }
        }

        /// <summary>
        /// Given a string, returns the matching AzureEdition instance.
        /// </summary>
        /// <param name="edition"></param>
        /// <returns></returns>
        public static AzureEdition AzureEditionFromString(string edition)
        {
            var azureEdition =
                AzureServiceObjectiveInfo.Keys.FirstOrDefault(
                    key => key.Name.ToLowerInvariant().Equals(edition.ToLowerInvariant()));
            if (azureEdition != null)
            {
                return azureEdition;
            }
            if (edition.Contains('\''))
            {
                throw new ArgumentException("ErrorInvalidEdition");
            }
            // we don't know what it is but Azure lets you send any value you want
            // including an empty string
            return new AzureEdition(edition.ToLowerInvariant(), edition);

        }

        /// <summary>
        /// Provides a mapping of Azure DB Editions to their respective size options
        /// </summary>
        /// Values below are taken from http://msdn.microsoft.com/en-us/library/dn268335.aspx
        private static readonly Dictionary<AzureEdition, KeyValuePair<int, DbSize[]>> AzureEditionDatabaseSizeMappings = new Dictionary
            <AzureEdition, KeyValuePair<int, DbSize[]>>
        {
            {
                AzureEdition.Basic,
                new KeyValuePair<int, DbSize[]>(
                    4, //2GB
                    new[]
                    {
                        new DbSize(100, SizeUnits.MB),
                        new DbSize(250, SizeUnits.MB),
                        new DbSize(500, SizeUnits.MB),
                        new DbSize(1, SizeUnits.GB),
                        new DbSize(2, SizeUnits.GB),
                    })
            },
            {
                AzureEdition.Standard,
                new KeyValuePair<int, DbSize[]>(
                    14, //250GB
                    new[]
                    {
                        new DbSize(100, SizeUnits.MB),
                        new DbSize(250, SizeUnits.MB),
                        new DbSize(500, SizeUnits.MB),
                        new DbSize(1, SizeUnits.GB),
                        new DbSize(2, SizeUnits.GB),
                        new DbSize(5, SizeUnits.GB),
                        new DbSize(10, SizeUnits.GB),
                        new DbSize(20, SizeUnits.GB),
                        new DbSize(30, SizeUnits.GB),
                        new DbSize(40, SizeUnits.GB),
                        new DbSize(50, SizeUnits.GB),
                        new DbSize(100, SizeUnits.GB),
                        new DbSize(150, SizeUnits.GB),
                        new DbSize(200, SizeUnits.GB),
                        new DbSize(250, SizeUnits.GB), //Default
                        new DbSize(300, SizeUnits.GB),
                        new DbSize(400, SizeUnits.GB),
                        new DbSize(500, SizeUnits.GB),
                        new DbSize(750, SizeUnits.GB),
                        new DbSize(1024, SizeUnits.GB),
                    })
            },
            {
                AzureEdition.Premium,
                new KeyValuePair<int, DbSize[]>(
                    17, //500GB
                    new[]
                    {
                        new DbSize(100, SizeUnits.MB),
                        new DbSize(250, SizeUnits.MB),
                        new DbSize(500, SizeUnits.MB),
                        new DbSize(1, SizeUnits.GB),
                        new DbSize(2, SizeUnits.GB),
                        new DbSize(5, SizeUnits.GB),
                        new DbSize(10, SizeUnits.GB),
                        new DbSize(20, SizeUnits.GB),
                        new DbSize(30, SizeUnits.GB),
                        new DbSize(40, SizeUnits.GB),
                        new DbSize(50, SizeUnits.GB),
                        new DbSize(100, SizeUnits.GB),
                        new DbSize(150, SizeUnits.GB),
                        new DbSize(200, SizeUnits.GB),
                        new DbSize(250, SizeUnits.GB),
                        new DbSize(300, SizeUnits.GB),
                        new DbSize(400, SizeUnits.GB),
                        new DbSize(500, SizeUnits.GB), //Default
                        new DbSize(750, SizeUnits.GB),
                        new DbSize(1024, SizeUnits.GB) //Following portal to display this as GB instead of 1TB
                    })
            },

            {
                AzureEdition.DataWarehouse,
                new KeyValuePair<int, DbSize[]>(
                    5, //10240GB
                    new[]
                    {
                        new DbSize(250, SizeUnits.GB),
                        new DbSize(500, SizeUnits.GB),
                        new DbSize(750, SizeUnits.GB),
                        new DbSize(1024, SizeUnits.GB),
                        new DbSize(5120, SizeUnits.GB),
                        new DbSize(10240, SizeUnits.GB),
                        new DbSize(20480, SizeUnits.GB),
                        new DbSize(30720, SizeUnits.GB),
                        new DbSize(40960, SizeUnits.GB),
                        new DbSize(51200, SizeUnits.GB),
                        new DbSize(61440, SizeUnits.GB),
                        new DbSize(71680, SizeUnits.GB),
                        new DbSize(81920, SizeUnits.GB),
                        new DbSize(92160, SizeUnits.GB),
                        new DbSize(102400, SizeUnits.GB),
                        new DbSize(153600, SizeUnits.GB),
                        new DbSize(204800, SizeUnits.GB),
                        new DbSize(245760, SizeUnits.GB),

                    })
            },
            {
                AzureEdition.GeneralPurpose,
                new KeyValuePair<int, DbSize[]>(
                    0, //32GB
                    new[]
                    {
                        new DbSize(32, SizeUnits.GB),
                        new DbSize(40, SizeUnits.GB),
                        new DbSize(50, SizeUnits.GB),
                        new DbSize(100, SizeUnits.GB),
                        new DbSize(150, SizeUnits.GB),
                        new DbSize(200, SizeUnits.GB),
                        new DbSize(250, SizeUnits.GB),
                        new DbSize(300, SizeUnits.GB),
                        new DbSize(400, SizeUnits.GB),
                        new DbSize(500, SizeUnits.GB),
                        new DbSize(750, SizeUnits.GB),
                        new DbSize(1024, SizeUnits.GB), //Following portal to display this as GB instead of 1TB
                        new DbSize(1536, SizeUnits.GB),
                        new DbSize(3072, SizeUnits.GB),
                        new DbSize(4096, SizeUnits.GB), 
                    })
            },
            {
                AzureEdition.BusinessCritical,
                new KeyValuePair<int, DbSize[]>(
                    0, //32GB
                    new[]
                    {
                        new DbSize(32, SizeUnits.GB),
                        new DbSize(40, SizeUnits.GB),
                        new DbSize(50, SizeUnits.GB),
                        new DbSize(100, SizeUnits.GB),
                        new DbSize(150, SizeUnits.GB),
                        new DbSize(200, SizeUnits.GB),
                        new DbSize(250, SizeUnits.GB),
                        new DbSize(300, SizeUnits.GB),
                        new DbSize(400, SizeUnits.GB),
                        new DbSize(500, SizeUnits.GB),
                        new DbSize(750, SizeUnits.GB),
                        new DbSize(1024, SizeUnits.GB), //Following portal to display this as GB instead of 1TB
                        new DbSize(1536, SizeUnits.GB),
                        new DbSize(2048, SizeUnits.GB),
                        new DbSize(4096, SizeUnits.GB)
                    })
            },

            {
                AzureEdition.Hyperscale,
                new KeyValuePair<int, DbSize[]>(0, new[] { new DbSize(0, SizeUnits.MB) })
            },
        };

        /// <summary>
        /// Maps Azure DB Editions to their corresponding Service Objective (Performance Level) options. These values are the default but
        /// can be overridden in the UI.
        /// 
        /// The key is the index of the default value for the list
        /// </summary>
        /// <remarks>Try to keep this data structure (particularly the default values for each SLO) in sync with
        /// the heuristic in TryGetAzureServiceLevelObjective() in %SDXROOT%\sql\ssms\core\sqlmanagerui\src\azureservicelevelobjectiveprovider.cs
        /// </remarks>
        private static readonly Dictionary<AzureEdition, KeyValuePair<int, string[]>> AzureServiceObjectiveInfo = new Dictionary
            <AzureEdition, KeyValuePair<int, string[]>>
        {
            {AzureEdition.Basic, new KeyValuePair<int, string[]>(0, new string[] {"Basic"})},
            {
                AzureEdition.Standard,
                new KeyValuePair<int, string[]>(0, new[] {"S0", "S1", "S2", "S3", "S4", "S6", "S7", "S9", "S12"})
            },
            {AzureEdition.Premium, new KeyValuePair<int, string[]>(0, new[] {"P1", "P2", "P4", "P6", "P11", "P15"})},
            {
                AzureEdition.DataWarehouse,
                new KeyValuePair<int, string[]>(3,
                    new[]
                    {
                        "DW100", "DW200", "DW300", "DW400", "DW500", "DW600", "DW1000", "DW1200", "DW1500", "DW2000",
                        "DW3000", "DW6000", "DW1000c","DW1500c","DW2000c",
                        "DW2500c","DW3000c","DW5000c","DW6000c","DW7500c",
                        "DW10000c","DW15000c","DW30000c"
                    })
            },
            {
                // Added missing Vcore sku's
                // Reference:https://docs.microsoft.com/en-us/azure/sql-database/sql-database-vcore-resource-limits-single-databases
                AzureEdition.GeneralPurpose,
                new KeyValuePair<int, string[]>(6 /* Default = GP_Gen5_2 */,
                    new[] 
                    {
                        "GP_Gen4_1", "GP_Gen4_2", "GP_Gen4_4", "GP_Gen4_8", "GP_Gen4_16","GP_Gen4_24",
                        "GP_Gen5_2","GP_Gen5_4","GP_Gen5_8","GP_Gen5_16","GP_Gen5_24","GP_Gen5_32","GP_Gen5_40","GP_Gen5_80"

                    })
            },
            {
                // Added missing Vcore sku's
                // Reference:https://docs.microsoft.com/en-us/azure/sql-database/sql-database-vcore-resource-limits-single-databases
                AzureEdition.BusinessCritical,
                new KeyValuePair<int, string[]>(6 /* Default = BC_Gen5_2 */,
                    new[] 
                    {   "BC_Gen4_1", "BC_Gen4_2", "BC_Gen4_4", "BC_Gen4_8", "BC_Gen4_16","BC_Gen4_24",
                        "BC_Gen5_2","BC_Gen5_4","BC_Gen5_8","BC_Gen5_16","BC_Gen5_24", "BC_Gen5_32", "BC_Gen5_40","BC_Gen5_80"
                    })
            },
            {
                // HS_Gen5_2 is the default since, as of 2/25/2020, customers, unless on an allowed list, are already prevented from choosing Gen4.
                AzureEdition.Hyperscale,
                new KeyValuePair<int, string[]>(11, new[] { 
                    "HS_Gen4_1",  "HS_Gen4_2", "HS_Gen4_3", "HS_Gen4_4", "HS_Gen4_5", "HS_Gen4_6", "HS_Gen4_7", "HS_Gen4_8", "HS_Gen4_9", "HS_Gen4_10",
                    "HS_Gen4_24", "HS_Gen5_2", "HS_Gen5_4", "HS_Gen5_6", "HS_Gen5_8", "HS_Gen5_10", "HS_Gen5_14", "HS_Gen5_16", "HS_Gen5_18", "HS_Gen5_20",
                    "HS_Gen5_24", "HS_Gen5_32", "HS_Gen5_40", "HS_Gen5_80"
                })
            }
        };

        //Supported BackupStorageRedundancy doc link:https://docs.microsoft.com/en-us/sql/t-sql/statements/create-database-transact-sql?view=azuresqldb-current&tabs=sqlpool 
        private static readonly Dictionary<string, string> bsrAPIToUIValueMapping = new Dictionary<string, string>()
        {
            { "GRS", "Geo" },
            { "LRS", "Local" },
            { "ZRS", "Zone" }
        };

        //KeyValuePair contains the BackupStorageRedundancy values for all azure editions.
        private static readonly KeyValuePair<int, string[]> keyValuePair = new KeyValuePair<int, string[]>(0, bsrAPIToUIValueMapping.Values.ToArray());
        private static readonly Dictionary<AzureEdition, KeyValuePair<int, string[]>> AzureBackupStorageRedundancy = new Dictionary
            <AzureEdition, KeyValuePair<int, string[]>>
            {
                {
                    AzureEdition.Basic, keyValuePair
                },
                {
                    AzureEdition.Standard, keyValuePair
                },
                {
                    AzureEdition.Premium, keyValuePair
                },
                {
                    AzureEdition.DataWarehouse, keyValuePair
                },
                {
                    AzureEdition.GeneralPurpose, keyValuePair
                },
                {
                    AzureEdition.BusinessCritical, keyValuePair
                },    
                {
                    AzureEdition.Hyperscale, keyValuePair
                }
            };

        /// <summary>
        /// Get the storageAccount Type string value from the dictionary backupStorageTypes.
        /// </summary>
        /// <param name="storageAccountType">Current StorageAccountType</param>
        /// <returns>StorageAccountType string value for the current storageType</returns>
        public static string GetStorageAccountTypeFromString(string storageAccountType)
        {
            if (bsrAPIToUIValueMapping.ContainsKey(storageAccountType))
            {
                return bsrAPIToUIValueMapping[storageAccountType];
            }
            return storageAccountType;
        }

        /// <summary>
        /// Gets the list of databases sizes applicable for the specified Azure DB edition (if such
        /// a mapping exists) as well as the index of the default size for that edition.
        /// 
        /// Outputs an empty array with an index of -1 if no such mapping exists
        /// </summary>
        /// <param name="edition"></param>
        /// <param name="databaseSizeInfo"></param>
        /// <returns>TRUE if a mapping exists, FALSE if it does not</returns>
        public static bool TryGetDatabaseSizeInfo(AzureEdition edition, out KeyValuePair<int, DbSize[]> databaseSizeInfo)
        {
            if (AzureEditionDatabaseSizeMappings.TryGetValue(edition, out databaseSizeInfo))
            {
                return true;
            }

            databaseSizeInfo = new KeyValuePair<int, DbSize[]>(-1, new DbSize[0]);

            return false;
        }

        /// <summary>
        /// Gets a KeyValuePair containing a list of the ServiceObjective names mapped to a particular Azure DB Edition
        /// (if such a mapping exists) as well as the index of the default Service Objective for that edition.
        /// Outputs an empty array with a default index of -1 if no such mapping exists. 
        /// </summary>
        /// <param name="edition"></param>
        /// <param name="serviceObjectiveInfo"></param>
        /// <returns>TRUE if a mapping exists, FALSE if it did not</returns>
        public static bool TryGetServiceObjectiveInfo(AzureEdition edition,
            out KeyValuePair<int, string[]> serviceObjectiveInfo)
        {
            if (AzureServiceObjectiveInfo.TryGetValue(edition, out serviceObjectiveInfo))
            {
                return true;
            }

            serviceObjectiveInfo = new KeyValuePair<int, string[]>(-1, new string[0]);

            return false;
        }

        /// <summary>
        /// Get the backupStorageRedundancy value for the given azure edition.
        /// </summary>
        /// <param name="edition">Azure Edition</param>
        /// <param name="backupStorageRedundancy">Supported BackupStorageRedundancy value</param>
        /// <returns>backupStorageRedundancy value for the given azure edition</returns>
        public static bool TryGetBackupStorageRedundancy(AzureEdition edition,
            out KeyValuePair<int, string[]> backupStorageRedundancy)
        {
            if (AzureBackupStorageRedundancy.TryGetValue(edition, out backupStorageRedundancy))
            {
                return true;
            }

            backupStorageRedundancy = new KeyValuePair<int, string[]>(-1, new string[0]);

            return false;
        }

        /// <summary>
        /// Gets the default database size for a specified Azure Edition
        /// </summary>
        /// <param name="edition"></param>
        /// <returns>The default size, or NULL if no default exists</returns>
        public static DbSize GetDatabaseDefaultSize(AzureEdition edition)
        {
            DbSize defaultSize = null;

            KeyValuePair<int, DbSize[]> pair;

            if (AzureEditionDatabaseSizeMappings.TryGetValue(edition, out pair))
            {
                defaultSize = pair.Value[pair.Key];
            }

            return defaultSize;
        }

        /// <summary>
        /// Gets the default Service Objective name for a particular Azure DB edition
        /// </summary>
        /// <param name="edition"></param>
        /// <returns></returns>
        public static string GetDefaultServiceObjective(AzureEdition edition)
        {
            string defaultServiceObjective = "";

            KeyValuePair<int, string[]> pair;

            if (TryGetServiceObjectiveInfo(edition, out pair))
            {
                //Bounds check since this value can be entered by users
                if (pair.Key >= 0 && pair.Key < pair.Value.Length)
                {
                    defaultServiceObjective = pair.Value[pair.Key];
                }
            }

            return defaultServiceObjective;
        }

        public static string GetDefaultBackupStorageRedundancy(AzureEdition edition)
        {
            string defaultBackupStorageRedundancy = "";

            KeyValuePair<int, string[]> pair;

            if (TryGetBackupStorageRedundancy(edition, out pair))
            {
                //Bounds check since this value can be entered by users
                if (pair.Key >= 0 && pair.Key < pair.Value.Length)
                {
                    defaultBackupStorageRedundancy = pair.Value[pair.Key];
                }
            }

            return defaultBackupStorageRedundancy;
        }

        /// <summary>
        /// Gets the localized Azure Edition display name
        /// </summary>
        /// <param name="edition"></param>
        /// <returns></returns>
        public static string GetAzureEditionDisplayName(AzureEdition edition)
        {
            return edition.DisplayName;
        }

        /// <summary>
        /// Parses a display name back into its corresponding AzureEdition.
        /// If it doesn't match a known edition, returns one whose Name is a lowercase version of the 
        /// given displayName
        /// </summary>
        /// <param name="displayName"></param>
        /// <param name="edition"></param>
        /// <returns>TRUE if the conversion succeeded, FALSE if it did not. </returns>
        public static bool TryGetAzureEditionFromDisplayName(string displayName, out AzureEdition edition)
        {
            edition = AzureServiceObjectiveInfo.Keys.FirstOrDefault(key => key.DisplayName.Equals(displayName)) ??
                      AzureEditionFromString(displayName);
            return true;
        }

        /// <summary>
        /// Returns a list of AzureEditions that are valid values for the EDITION option
        /// when creating a database.
        /// </summary>
        /// We do this so that the AzureEdition enum can have values such as NONE or DEFAULT added
        /// without requiring clients to explicitly filter out those values themselves each time. 
        /// <returns></returns>
        public static IEnumerable<AzureEdition> GetValidAzureEditionOptions(object unused)
        {
            yield return AzureEdition.Basic;
            yield return AzureEdition.Standard;
            yield return AzureEdition.Premium;
            yield return AzureEdition.DataWarehouse;
            yield return AzureEdition.BusinessCritical;
            yield return AzureEdition.GeneralPurpose;
            //yield return AzureEdition.Free;
            yield return AzureEdition.Hyperscale;
            //yield return AzureEdition.Stretch;
            //yield return AzureEdition.System;
        }
    }
}
