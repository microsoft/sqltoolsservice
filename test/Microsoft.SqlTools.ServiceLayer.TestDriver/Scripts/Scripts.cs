//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Reflection;

namespace Microsoft.SqlTools.ServiceLayer.TestDriver.Scripts
{
    public class Scripts
    {

        public const string SimpleQuery = "SELECT * FROM sys.all_columns";

        public const string DelayQuery = "WAITFOR DELAY '00:01:00'";

        private static readonly Lazy<string> ComplexQueryInstance = new Lazy<string>(() =>
        {
            string fileContent = string.Empty;
            try
            {
                using (Stream stream = typeof(Scripts).GetTypeInfo().Assembly.GetManifestResourceStream("Microsoft.SqlTools.ServiceLayer.TestDriver.Scripts.AdventureWorks.sql"))
                {
                    using(StreamReader reader = new StreamReader(stream))
                    {
                        fileContent = reader.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load the sql script. error: {ex.Message}");
            }
            return fileContent;
        });

        public static string ComplexQuery { get { return ComplexQueryInstance.Value; } }
    }
}
