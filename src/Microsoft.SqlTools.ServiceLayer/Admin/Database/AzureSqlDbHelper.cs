//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.SqlServer.Diagnostics.STrace;
using Microsoft.SqlServer.Management.Common;
using Microsoft.Win32;
using SizeUnits = Microsoft.SqlTools.ServiceLayer.Admin.DbSize.SizeUnits;

namespace Microsoft.SqlTools.ServiceLayer.Admin
{
    public static class AzureSqlDbHelper
    {
        private static readonly TraceContext TraceContext = TraceContext.GetTraceContext("AzureSqlDbUtils", typeof(AzureSqlDbHelper).Name);


        /// <summary>
        /// Registry sub key for the AzureServiceObjectives overrides
        /// </summary>
        private const string AzureServiceObjectivesRegSubKey = @"AzureServiceObjectives";

        /// <summary>
        /// Contains the various editions available for an Azure Database
        /// </summary>
        /// ****IMPORTANT**** - If updating this enum make sure that the other logic in this class is updated as well
        public enum AzureEdition
        {
            Web = 0,
            Business = 1,
            Basic = 2,
            Standard = 3,
            Premium = 4,
            DataWarehouse = 5,
            PremiumRS = 6
        }

        /// <summary>
        /// Provides a mapping of Azure DB Editions to their respective size options
        /// </summary>
        /// Values below are taken from http://msdn.microsoft.com/en-us/library/dn268335.aspx
        private static readonly Dictionary<AzureEdition, KeyValuePair<int, DbSize[]>> AzureEditionDatabaseSizeMappings = new Dictionary
            <AzureEdition, KeyValuePair<int, DbSize[]>>
        {
            {
                AzureEdition.Web, new KeyValuePair<int, DbSize[]>(
                    1, //1GB
                    new[]
                    {
                        new DbSize(100, SizeUnits.MB),
                        new DbSize(1, SizeUnits.GB), //Default
                        new DbSize(5, SizeUnits.GB)
                    })
            },
            {
                AzureEdition.Business, new KeyValuePair<int, DbSize[]>(
                    0, //10GB
                    new[]
                    {
                        new DbSize(10, SizeUnits.GB), //Default
                        new DbSize(20, SizeUnits.GB),
                        new DbSize(30, SizeUnits.GB),
                        new DbSize(40, SizeUnits.GB),
                        new DbSize(50, SizeUnits.GB),
                        new DbSize(100, SizeUnits.GB),
                        new DbSize(150, SizeUnits.GB)
                    })
            },
            {
                AzureEdition.Basic, new KeyValuePair<int, DbSize[]>(
                    3, //2GB
                    new[]
                    {
                        new DbSize(100, SizeUnits.MB),
                        new DbSize(500, SizeUnits.MB),
                        new DbSize(1, SizeUnits.GB),
                        new DbSize(2, SizeUnits.GB) //Default
                    })
            },
            {
                AzureEdition.Standard,
                new KeyValuePair<int, DbSize[]>(
                    13, //250GB
                    new[]
                    {
                        new DbSize(100, SizeUnits.MB),
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
                        new DbSize(250, SizeUnits.GB) //Default
                    })
            },
            {
                AzureEdition.Premium,
                new KeyValuePair<int, DbSize[]>(
                    16, //500GB
                    new[]
                    {
                        new DbSize(100, SizeUnits.MB),
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
                        new DbSize(51200, SizeUnits.GB)
                    })
            },
            {
                AzureEdition.PremiumRS,
                new KeyValuePair<int, DbSize[]>(
                    16, //500GB
                    new[]
                    {
                        new DbSize(100, SizeUnits.MB),
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
                    })
            },
        };

        /// <summary>
        /// Maps Azure DB Editions to their corresponding Service Objective (Performance Level) options. These values are the default but
        /// can be overridden by use of the ImportExportWizard registry key (see static initializer above).
        /// 
        /// The key is the index of the default value for the list
        /// </summary>
        private static readonly Dictionary<AzureEdition, KeyValuePair<int, string[]>> AzureServiceObjectiveInfo = new Dictionary
            <AzureEdition, KeyValuePair<int, string[]>>
        {
            {AzureEdition.Basic, new KeyValuePair<int, string[]>(0, new[] {"Basic"})},
            {AzureEdition.Standard, new KeyValuePair<int, string[]>(2, new[] {"S0", "S1", "S2", "S3"})},
            {AzureEdition.Premium, new KeyValuePair<int, string[]>(0, new[] {"P1", "P2", "P4", "P6", "P11", "P15"})},
            {AzureEdition.PremiumRS, new KeyValuePair<int, string[]>(0, new []{"PRS1", "PRS2", "PRS4", "PRS6"})},
            {AzureEdition.DataWarehouse, new KeyValuePair<int, string[]>(3, new[] {"DW100", "DW200", "DW300", "DW400", "DW500", "DW600", "DW1000", "DW1200", "DW1500", "DW2000", "DW3000", "DW6000"})}
        };

        /// <summary>
        /// Static initializer to read in the registry key values for the Service Objective mappings, which allows the user to override the defaults set for
        /// the service objective list. We allow them to do this as a temporary measure so that if we change the service objectives in the future we
        /// can tell people to use the registry key to use the new values until an updated SSMS can be released.
        /// </summary>
        static AzureSqlDbHelper()
        {
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

            if (AzureServiceObjectiveInfo.TryGetValue(edition, out pair))
            {
                //Bounds check since this value can be entered by users
                if (pair.Key >= 0 && pair.Key < pair.Value.Length)
                {
                    defaultServiceObjective = pair.Value[pair.Key];
                }
            }

            return defaultServiceObjective;
        }

        /// <summary>
        /// Gets the localized Azure Edition display name
        /// </summary>
        /// <param name="edition"></param>
        /// <returns></returns>
        public static string GetAzureEditionDisplayName(AzureEdition edition)
        {
            string result;
            switch (edition)
            {
                //case AzureEdition.Business:
                //    result = SR.BusinessAzureEdition;
                //    break;
                //case AzureEdition.Web:
                //    result = SR.WebAzureEdition;
                //    break;
                //case AzureEdition.Basic:
                //    result = SR.BasicAzureEdition;
                //    break;
                //case AzureEdition.Standard:
                //    result = SR.StandardAzureEdition;
                //    break;
                //case AzureEdition.Premium:
                //    result = SR.PremiumAzureEdition;
                //    break;
                //case AzureEdition.DataWarehouse:
                //    result = SR.DataWarehouseAzureEdition;
                //    break;
                //case AzureEdition.PremiumRS:
                //    result = SR.PremiumRsAzureEdition;
                //    break;
                default:                    
                    result = edition.ToString();
                    break;
            }

            return result;
        }

        /// <summary>
        /// Parses a display name back into its corresponding AzureEdition. 
        /// </summary>
        /// <param name="displayName"></param>
        /// <param name="edition"></param>
        /// <returns>TRUE if the conversion succeeded, FALSE if it did not. </returns>
        public static bool TryGetAzureEditionFromDisplayName(string displayName, out AzureEdition edition)
        {
            //if (string.Compare(displayName, SR.BusinessAzureEdition, CultureInfo.CurrentUICulture, CompareOptions.None) == 0)
            //{
            //    edition = AzureEdition.Business;
            //}
            //else if (string.Compare(displayName, SR.WebAzureEdition, CultureInfo.CurrentUICulture, CompareOptions.None) == 0)
            //{
            //    edition = AzureEdition.Web;
            //}
            //else if (string.Compare(displayName, SR.BasicAzureEdition, CultureInfo.CurrentUICulture, CompareOptions.None) == 0)
            //{
            //    edition = AzureEdition.Basic;
            //}
            //else if (string.Compare(displayName, SR.StandardAzureEdition, CultureInfo.CurrentUICulture, CompareOptions.None) == 0)
            //{
            //    edition = AzureEdition.Standard;
            //}
            //else if (string.Compare(displayName, SR.PremiumAzureEdition, CultureInfo.CurrentUICulture, CompareOptions.None) == 0)
            //{
            //    edition = AzureEdition.Premium;
            //}
            //else if (string.Compare(displayName, SR.DataWarehouseAzureEdition, CultureInfo.CurrentUICulture, CompareOptions.None) == 0)
            //{
            //    edition = AzureEdition.DataWarehouse;
            //}
            //else if (string.Compare(displayName, SR.PremiumRsAzureEdition, CultureInfo.CurrentUICulture, CompareOptions.None) == 0)
            //{
            //    edition = AzureEdition.PremiumRS;
            //}
            //else
            {                
                //"Default" edition is standard - but since we're returning false the user shouldn't look at this anyways
                edition = AzureEdition.Standard;
                return false;
            }

            // return true;
        }

        /// <summary>
        /// Returns a list of AzureEditions that are valid values for the EDITION option
        /// when creating a database.
        /// </summary>
        /// We do this so that the AzureEdition enum can have values such as NONE or DEFAULT added
        /// without requiring clients to explicitly filter out those values themselves each time. 
        /// <returns></returns>
        public static IEnumerable<AzureEdition> GetValidAzureEditionOptions(ServerVersion version)
        {
            //Azure v12 and above doesn't have the Web and Business tiers
            if (version.Major >= 12)
            {
                return new List<AzureEdition>()
                {
                    AzureEdition.Basic,
                    AzureEdition.Standard,
                    AzureEdition.Premium,
                    AzureEdition.PremiumRS,
                    AzureEdition.DataWarehouse
                };
            }

            //Default for now is to return all values since they're currently all valid
            return Enum.GetValues(typeof(AzureEdition)).Cast<AzureEdition>();
        }
    }
}
