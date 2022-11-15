//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using ScriptGenerator.Properties;
using System.IO;
using System.Text.RegularExpressions;

namespace ScriptGenerator
{
    public partial class Program
    {
        internal const string DefaultFileExtension = "Sql";
        static readonly Regex ExtractDbName = CreateDbRegex();
        static readonly Regex CreateTableRegex = CreateTableSyntaxRegex();
        static readonly Regex AlterTableConstraintRegex = AlterTableSyntaxRegex();
        static readonly Regex CreateViewRegex = CreateViewSyntaxRegex();
        static readonly Regex CreateProcedureRegex = CreateProcedureSyntaxRegex(); 
        static void Main(string[] args)
        {
            var options = new CommandOptions(args);
            for (int d = 1; d <= options.Databases; d++)
            {
                using (var writer = new StreamWriter(Path.ChangeExtension($@"{options.FilePathPrefix}{d}", DefaultFileExtension)))
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

        [GeneratedRegex("CREATE\\s+DATABASE\\s+\\[(?<dbName>\\w+)\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-CA")]
        private static partial Regex CreateDbRegex();
        [GeneratedRegex("(?<begin>CREATE\\s+TABLE\\s+.*)(?<middle>\\](?![\\.])[\\s\\S]*?CONSTRAINT\\s+\\[\\w+?)(?<end>\\](?![\\.]))", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-CA")]
        private static partial Regex CreateTableSyntaxRegex();
        [GeneratedRegex("(?<begin>ALTER\\s+TABLE\\s+.*?)(?<middle>\\](?![\\.])\\s*\\b(?:ADD|WITH|CHECK)\\b[\\s\\S]*?CONSTRAINT\\s+\\[\\w+?)(?<end>\\](?![\\.]))", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-CA")]
        private static partial Regex AlterTableSyntaxRegex();
        [GeneratedRegex("(?<begin>CREATE\\s+VIEW\\s+.*?)(?<end>\\](?![\\.])[\\s\\S]*?\\bAS\\b)", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-CA")]
        private static partial Regex CreateViewSyntaxRegex();
        [GeneratedRegex("(?<begin>CREATE\\s+PROCEDURE\\s+.*?)(?<end>\\](?![\\.])[\\s\\S]*?(?:@|WITH|AS))", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-CA")]
        private static partial Regex CreateProcedureSyntaxRegex();
    }
}
