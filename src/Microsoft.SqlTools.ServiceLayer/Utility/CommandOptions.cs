//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;

namespace Microsoft.SqlTools.ServiceLayer.Utility
{
    /// <summary>
    /// The command-line options helper class.
    /// </summary>
    internal class CommandOptions
    {
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
                    if (arg[0] == '-' || arg[0] == '/')
                    {
                        arg = arg.Substring(1).ToLowerInvariant();
                        switch (arg)
                        {
                            case "-enable-logging":
                                EnableLogging = true;
                                break;
                            default:
                                ErrorMessage += String.Format("Unknown argument \"{0}\"\r\n", arg);
                                break;
                        }
                    }
                }
                PrintOptionValues();
            }
            catch (Exception ex)
            {
                ErrorMessage += ex.ToString();
                return;
            }
        }

        /// <summary>
        /// Print all command-line option values
        /// </summary>
        public void PrintOptionValues()
        {
            Console.WriteLine("EnableLogging: " + this.EnableLogging);
        }

        /// <summary>
        /// Get the usage string describing command-line arguments for the program
        /// </summary>
        public string Usage()
        {
            var str = string.Format("{0}\r\n" +
                "Microsoft.SqlTools.ServiceLayer.exe \r\n" +
                "   Options:\r\n" +
                "        [--enable-logging]\r\n",
                ErrorMessage);
            return str;
        }

        private bool enableLogging = false;

        /// <summary>
        /// Whether diagnostic logging is enabled
        /// </summary>
        public bool EnableLogging
        {
            get { return enableLogging; }
            private set { enableLogging = value; }
        }

        internal string ErrorMessage { get; private set; }
    }
}
