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
            try
            {
                string assemblyLocation = typeof(Scripts).GetTypeInfo().Assembly.Location;
                string folderName = Path.GetDirectoryName(assemblyLocation);
                string filePath = Path.Combine(folderName, "Scripts/AdventureWorks.sql");
                return File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load the sql script. error: {ex.Message}");
                return string.Empty;
            }
        });

        public static string ComplexQuery { get { return ComplexQueryInstance.Value; } }
    }
}
