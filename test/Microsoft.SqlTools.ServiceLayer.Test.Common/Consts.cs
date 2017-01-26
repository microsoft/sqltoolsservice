using System;

namespace Microsoft.SqlTools.ServiceLayer.Test.Commons
{
    public sealed class Consts
    {
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

        public const string TestFileLocation = @"C:\projects\sqltoolsservice";

        public const string BVTLocalRoot = "BVT_LOCALROOT";

        public const string DBIMode = "DBI_MODE";
     
    }
}