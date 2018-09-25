//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace Microsoft.SqlTools.Hosting.Utility
{
    /// <summary>
    /// The command-line options helper class.
    /// </summary>
    public class CommandOptions
    {
        //set default log directory
        internal readonly string DefaultLogRoot = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        /// <summary>
        /// Construct and parse command line options from the arguments array
        /// </summary>
        public CommandOptions(string[] args, string serviceName)
        {
            ServiceName = serviceName;
            ErrorMessage = string.Empty;
            Locale = string.Empty;

            //set default log directory
            LoggingDirectory = DefaultLogRoot;
            if (!string.IsNullOrWhiteSpace(ServiceName))
            {
                LoggingDirectory = Path.Combine(LoggingDirectory, ServiceName);
            }

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
                            case "-enable-logging":
                                break; //ignore this old option for now for backward compat with older opsstudio code - to be removed in a future checkin
                            case "-tracing-level":
                                TracingLevel = args[++i];
                                break;
                            case "-log-file":
                                LogFilePath = args[++i];
                                break;
                            case "-log-dir":
                                SetLoggingDirectory(args[++i]);
                                break;
                            case "-locale":
                                SetLocale(args[++i]);
                                break;
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
        /// Gets the directory where log files are output.
        /// </summary>
        public string LoggingDirectory { get; private set; }

        /// <summary>
        /// Whether the program should exit immediately. Set to true when the usage is printed.
        /// </summary>
        public bool ShouldExit { get; private set; }

        /// <summary>
        /// The locale our we should instantiate this service in 
        /// </summary>
        public string Locale { get; private set; }

        /// <summary>
        /// Name of service that is receiving command options
        /// </summary>
        public string ServiceName { get; private set; }

        /// <summary>
        /// Get the usage string describing command-line arguments for the program
        /// </summary>
        public string Usage
        {
            get
            {
                var str = string.Format("{0}" + Environment.NewLine +
                    ServiceName + " " + Environment.NewLine +
                    "   Options:" + Environment.NewLine +
                    "        [--enable-logging ] (noop present for backward compat)" + Environment.NewLine +
                    "        [--tracing-level **] (** can be any of: All, Off, Critical, Error, Warning, Information, Verbose)" + Environment.NewLine +
                    "        [--log-file **]" + Environment.NewLine +
                    "        [--log-dir **] (default: %APPDATA%\\<service name>)" + Environment.NewLine +
                    "        [--locale **] (default: 'en')" + Environment.NewLine,
                    "        [--help]" + Environment.NewLine +
                    ErrorMessage);
                return str;
            }
        }

        public string TracingLevel { get; private set; }

        public string LogFilePath { get; private set; }

        private void SetLoggingDirectory(string loggingDirectory)
        {
            if (string.IsNullOrWhiteSpace(loggingDirectory))
            {
                return;
            }

            this.LoggingDirectory = Path.GetFullPath(loggingDirectory);
        }

        public virtual void SetLocale(string locale)
        {
            try
            {
                LocaleSetter(locale);
            }
            catch (CultureNotFoundException)
            {
                // Ignore CultureNotFoundException since it only is thrown before Windows 10.  Windows 10,
                // along with macOS and Linux, pick up the default culture if an invalid locale is passed
                // into the CultureInfo constructor.
            }
        }

        /// <summary>
        /// Sets the Locale field used for testing and also sets the global CultureInfo used for
        /// culture-specific messages
        /// </summary>
        /// <param name="locale"></param>
        internal void LocaleSetter(string locale)
        {
            // Creating cultureInfo from our given locale
            CultureInfo language = new CultureInfo(locale);
            Locale = locale;

            // Setting our language globally 
            CultureInfo.CurrentCulture = language;
            CultureInfo.CurrentUICulture = language;
        }
    }
}
