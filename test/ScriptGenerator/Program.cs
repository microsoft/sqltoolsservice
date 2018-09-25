using ScriptGenerator.Properties;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace ScriptGenerator
{

    class Program
    {
        internal const string DefaultFileExtension = "Sql";
        static readonly Regex ExtractDbName = new Regex(@"CREATE\s+DATABASE\s+\[(?<dbName>\w+)\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex CreateTableRegex = new Regex(@"(?<begin>CREATE\s+TABLE\s+.*)(?<middle>\](?![\.])[\s\S]*?CONSTRAINT\s+\[\w+?)(?<end>\](?![\.]))", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex AlterTableConstraintRegex = new Regex(@"(?<begin>ALTER\s+TABLE\s+.*?)(?<middle>\](?![\.])\s*\b(?:ADD|WITH|CHECK)\b[\s\S]*?CONSTRAINT\s+\[\w+?)(?<end>\](?![\.]))", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex CreateViewRegex = new Regex(@"(?<begin>CREATE\s+VIEW\s+.*?)(?<end>\](?![\.])[\s\S]*?\bAS\b)", RegexOptions.Compiled | RegexOptions.IgnoreCase );
        static readonly Regex CreateProcedureRegex = new Regex(@"(?<begin>CREATE\s+PROCEDURE\s+.*?)(?<end>\](?![\.])[\s\S]*?(?:@|WITH|AS))", RegexOptions.Compiled | RegexOptions.IgnoreCase); 
        static void Main(string[] args)
        {
            CommandOptions options = new CommandOptions(args);
            for (int d = 1; d <= options.Databases; d++)
            {
                using (StreamWriter writer = new StreamWriter(Path.ChangeExtension($@"{options.FilePathPrefix}{d}", DefaultFileExtension)))
                {
                    string oldDbName = ExtractDbName.Match(Resources.AdventureWorks).Groups["dbName"].Value;
                    string newDbName = $@"{oldDbName}{d}";
                    //Turn Off Strict Clr Mode
                    writer.WriteLine(Resources.TurnOffClrStrictSecurityMode);

                    //Put all original objects in the 'newDbName' database
                    writer.WriteLine(Resources.AdventureWorks.Replace(oldDbName, newDbName));

                    // Multiple copies of Create Table statements
                    for (int t = 1; t <= options.TablesMultiplier; t++)
                    {
                        string tablesScript = Resources.AdventureWorksTablesCreate.Replace(oldDbName, newDbName);
                        tablesScript = CreateTableRegex.Replace(tablesScript, "${begin}" + t + "${middle}" + t + "${end}");
                        writer.WriteLine(AlterTableConstraintRegex.Replace(tablesScript, "${begin}"+ t + "${middle}" + t + "${end}"));
                    }
                    // Multiple copies of Create View statements
                    for (int v = 1; v <= options.ViewsMultiplier; v++)
                    {
                        string viewScript = Resources.AdventureWorksViewsCreate.Replace(oldDbName, newDbName);
                        writer.WriteLine(CreateViewRegex.Replace(viewScript, "${begin}" + v + "${end}"));
                    }
                    // Multiple copies of Create Procedure statements
                    for (int s = 1; s <= options.StoredProceduresMultiplier; s++)
                    {
                        string spScript = Resources.AdventureWorksStoredProceduresCreate.Replace(oldDbName, newDbName);
                        writer.WriteLine(CreateProcedureRegex.Replace(spScript, "${begin}" + s + "${end}"));
                    }
                    //Turn On Strict Clr Mode
                    writer.WriteLine(Resources.TurnOnClrStrictSecurityMode);
                }
            }
        }
    }
}
