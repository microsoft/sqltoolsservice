//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.Test.Common
{
    public static class Constants
    {
        public const string SqlConectionSettingsEnvironmentVariable = "SettingsFileName";

        /// <summary>
        /// Environment variable used to get the TSDATA source directory root.
        /// K2 is under it.
        /// </summary>
        public const string SourceDirectoryEnvVariable = "Enlistment_Root";

        /// <summary>
        /// Environment variable used to get the build output directory.
        /// DTRun will set this automatically
        /// </summary>
        public const string BinariesDirectoryEnvVariable = "DacFxBuildOutputDir";

        public const string DDSuiteBuiltTarget = "DD_SuitesTarget";

        public const string DBBackupFileLocation = "DBBackupPath";

        public const string ProjectPath = "ProjectPath";

        public const string BVTLocalRoot = "BVT_LOCALROOT";

        public const string DBIMode = "DBI_MODE";
    }
}
