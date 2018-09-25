//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Globalization;
using System.IO;

namespace ScriptGenerator
{
    /// <summary>
    /// The command-line options helper class.
    /// </summary>
    public class CommandOptions
    {
        private const string DefaultFilePrefix = "AdventureWorks";
        /// <summary>
        /// Construct and parse command line options from the arguments array
        /// </summary>
        public CommandOptions(string[] args)
        {
            ErrorMessage = string.Empty;
            try
            {
                for (int i = 0; i < args.Length; ++i)
                {
                    string arg = args[i];
                    if (arg.StartsWith("--") || arg.StartsWith("-"))
                    {
                        // Extracting arguments and properties
                        arg = arg.Substring(1).ToLowerInvariant();
                        string argName = arg;
                        switch (argName)
                        {
                            case "-filepathprefix":
                            case "fp":
                               FilePathPrefix = args[++i];
                               break;
                            case "-numberofdatabases":
                            case "-numberofdbs":
                            case "ndb":
                            {
                                if (int.TryParse(args[++i], out int n))
                                {
                                    Databases = n;
                                }
                                else
                                {
                                    ErrorMessage += $@"Argument for NumberOfDatabases:'{args[i]}' is not a valid integer";
                                }
                                break;
                            }
                            case "-tablesmultiplier":
                            case "tm":
                            {
                                if (int.TryParse(args[++i], out int n))
                                {
                                    TablesMultiplier = n;
                                }
                                else
                                {
                                    ErrorMessage += $@"Argument for NumberOfTables:'{args[i]}' is not a valid integer";
                                }
                                break;
                            }
                            case "-storedproceduresmultiplier":
                            case "spm":
                            {
                                if (int.TryParse(args[++i], out int n))
                                {
                                    StoredProceduresMultiplier = n;
                                }
                                else
                                {
                                    ErrorMessage += $@"Argument for NumberOfStoredProcedures:'{args[i]}' is not a valid integer";
                                }
                                break;
                            }
                            case "-viewsmultiplier":
                            case "vm":
                            {
                                if (int.TryParse(args[++i], out int n))
                                {
                                    ViewsMultiplier = n;
                                }
                                else
                                {
                                    ErrorMessage += $@"Argument for NumberOfTables:'{args[i]}' is not a valid integer";
                                }

                                break;
                            }
                            case "h":
                            case "-help":
                                ShouldExit = true;
                                return;
                            default:
                                ErrorMessage += string.Format("Unknown argument \"{0}\"" + Environment.NewLine, argName);
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage += ex.ToString();
                return;
            }
            finally
            {
                if (!string.IsNullOrEmpty(ErrorMessage) || ShouldExit)
                {
                    Console.WriteLine(Usage);
                    ShouldExit = true;
                }
            }
        }

        /// <summary>
        /// Contains any error messages during execution
        /// </summary>
        public string ErrorMessage { get; private set; }

        /// <summary>
        /// Whether the program should exit immediately. Set to true when the usage is printed.
        /// </summary>
        public bool ShouldExit { get; private set; }

        /// <summary>
        /// Get the usage string describing command-line arguments for the program
        /// </summary>
        public string Usage
        {
            get
            {
                var str = $@"{ErrorMessage}" + Environment.NewLine +
                    "   Options:" + Environment.NewLine +
                  $@"        [--filepathprefix **]  (default {DefaultFilePrefix})" + Environment.NewLine +
                  $@"        [-fp **]  (default {DefaultFilePrefix})" + Environment.NewLine +
                    "        [--NumberOfDatabases **]  (default 1)" + Environment.NewLine +
                    "        [-ndb **]  (default 1)" + Environment.NewLine +
                    "        [--TablesMultiplier **]  (default 1)" + Environment.NewLine +
                    "        [-tm **]  (default 1)" + Environment.NewLine +
                    "        [--ViewsMultiplier **]  (default 1)" + Environment.NewLine +
                    "        [-vm **]  (default 1)" + Environment.NewLine +
                    "        [--StoreProceduresMultiplier **]  (default 1)" + Environment.NewLine +
                    "        [-spm **]  (default 1)" + Environment.NewLine +
                    "        [--help]" + Environment.NewLine;
                return str;
            }
        }

        public int Databases { get; private set; } = 1;
        public int TablesMultiplier { get; private set; } = 1;
        public int StoredProceduresMultiplier { get; private set; } = 1;
        public int ViewsMultiplier { get; private set; } = 1;
        public string FilePathPrefix { get; private set; } = DefaultFilePrefix;
    }
}
