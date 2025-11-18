//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Globalization;

namespace Microsoft.SqlTools.Utility
{
    /// <summary>
    /// The command-line options helper class.
    /// </summary>
    public class CommandOptions
    {
        // set default log directory
        // refer to https://jimrich.sk/environment-specialfolder-on-windows-linux-and-os-x/ && https://stackoverflow.com/questions/895723/environment-getfolderpath-commonapplicationdata-is-still-returning-c-docum
        // for cross platform locations
        internal readonly string DefaultLogRoot = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        /// <summary>
        /// Construct and parse command line options from the arguments array
        /// </summary>
        /// <param name="args">The args to parse</param>
        /// <param name="serviceName">Name of the service to display</param>
        public CommandOptions(string[] args, string serviceName)
        {
            ServiceName = serviceName;
            ErrorMessage = string.Empty;
            Locale = string.Empty;
            ApplicationName = string.Empty;

            try
            {
                for (int i = 0; i < args.Length; ++i)
                {
                    string arg = args[i];
                    if (arg != null && (arg.StartsWith("--") || arg.StartsWith("-")))
                    {
                        // Extracting arguments and properties
                        arg = arg.Substring(1).ToLowerInvariant();
                        string argName = arg;

                        switch (argName)
                        {
                            case "-application-name":
                                ApplicationName = args[++i];
                                break;
                            case "-data-path":
                                ApplicationPath = args[++i];
                                break;
                            case "-autoflush-log":
                                AutoFlushLog = true;
                                break;
                            case "-tracing-level":
                                TracingLevel = args[++i];
                                break;
                            case "-pii-logging":
                                PiiLogging = true;
                                break;
                            case "-log-file":
                                LogFilePath = args[++i];
                                break;
                            case "-locale":
                                SetLocale(args[++i]);
                                break;
                            case "-enable-logging":
                                break;
                            case "h":
                            case "-help":
                                ShouldExit = true;
                                return;
                            case "-http-proxy-url":
                                HttpProxyUrl = args[++i];
                                break;
                            case "-http-proxy-strict-ssl":
                                HttpProxyStrictSSL = true;
                                break;
                            case "-service-name":
                                ServiceName = args[++i];
                                break;
                            case "-parallel-message-processing":
                                ParallelMessageProcessing = true;
                                break;
                            case "-parallel-message-processing-limit":
                                string limit = args[++i];
                                if (Int32.TryParse(limit, out int limitValue))
                                {
                                    ParallelMessageProcessingLimit = limitValue;
                                }
                                break;
                            case "-enable-sql-authentication-provider":
                                EnableSqlAuthenticationProvider = true;
                                break;
                            case "-enable-connection-pooling":
                                EnableConnectionPooling = true;
                                break;
                            case "-parent-pid":
                                string pid = args[++i];
                                if (Int32.TryParse(pid, out int pidValue))
                                {
                                    ParentProcessId = pidValue;
                                }
                                break;
                            case "-vscode-debug-launch":
                                break;
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
        /// Name of application that is sending command options
        /// </summary>
        public string ApplicationName { get; private set; }

        /// <summary>
        /// Path of application home directory
        /// </summary>
        public string ApplicationPath { get; private set; }

        /// <summary>
        /// Contains any error messages during execution
        /// </summary>
        public string ErrorMessage { get; private set; }

        /// <summary>
        /// Whether the program should exit immediately. Set to true when the usage is printed.
        /// </summary>
        public bool ShouldExit { get; protected set; }

        /// <summary>
        /// The locale our we should instantiate this service in 
        /// </summary>
        public string Locale { get; private set; }

        /// <summary>
        /// Custom Http Proxy URL as specified in Azure Data Studio
        /// </summary>
        public string? HttpProxyUrl { get; private set; }

        /// <summary>
        /// Specifies whether the proxy server certificate should be verified against the list of supplied CAs.
        /// </summary>
        public bool HttpProxyStrictSSL { get; private set; }

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
                    "        [--autoflush-log] (If passed in auto flushing of log files is enabled., Verbose. Default is to not auto-flush log files)" + Environment.NewLine +
                    "        [--locale **] (default: 'en')" + Environment.NewLine,
                    "        [--log-file **]" + Environment.NewLine +
                    "        [--tracing-level **] (** can be any of: All, Off, Critical, Error, Warning, Information, Verbose. Default is Critical)" + Environment.NewLine +
                    "        [--help]" + Environment.NewLine +
                    ErrorMessage);
                return str;
            }
        }

        public string TracingLevel { get; private set; }

        public bool PiiLogging { get; private set; }

        public string LogFilePath { get; private set; }

        public bool AutoFlushLog { get; private set; } = false;

        /// <summary>
        /// Enables parallel message processing when queueing tasks from dispatcher.
        /// This option is enabled by default during initialization.
        /// NOTE: Keep the value 'false' by default, as this option is only 'enabled' during initialization, not 'disabled'.
        /// </summary>
        public bool ParallelMessageProcessing { get; private set; } = false;

        /// <summary>
        /// The maximum number of parallel operations that can be queued without blocking the main thread.
        /// Defaults to 100. This should be optimal to maintain a healthy application runtime state.
        /// If users need more parallel operations depending on if their systems support the same, they can always increase the limit.
        /// </summary>
        public int ParallelMessageProcessingLimit { get; private set; } = 100;

        /// <summary>
        /// Enables configured 'Sql Authentication Provider' for 'Active Directory Interactive' authentication mode to be used 
        /// when user chooses 'Azure MFA'. This setting enables MSAL.NET to acquire token with SqlClient integration.
        /// This option is enabled by default during initialization.
        /// NOTE: Keep the value 'false' by default, as this option is only 'enabled' during initialization, not 'disabled'.
        /// </summary>
        public bool EnableSqlAuthenticationProvider { get; private set; } = false;

        /// <summary>
        /// Enables connection pooling for all SQL connections, removing feature name identifier from application name to prevent unwanted connection pools.
        /// This option is enabled by default during initialization.
        /// NOTE: Keep the value 'false' by default, as this option is only 'enabled' during initialization, not 'disabled'.
        /// </summary>
        public bool EnableConnectionPooling { get; private set; } = false;

        /// <summary>
        /// The ID of the process that started this service. This is used to check when the parent
        /// process exits so that the service process can exit at the same time.
        /// </summary>
        public int? ParentProcessId { get; private set; }

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
        public void LocaleSetter(string locale)
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
