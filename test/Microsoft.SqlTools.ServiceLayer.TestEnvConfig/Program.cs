//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.IO;
using System.Xml.Linq;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.TestEnvConfig
{
    sealed class Program
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0210:Convert to top-level statements", Justification = "Structure retained for readability.")]
        static void Main(string[] args)
        {
            if (args.Length == 1)
            {
                string arg = args[0];

                if (arg.Equals("-?", StringComparison.OrdinalIgnoreCase) ||
                    arg.Equals("/?", StringComparison.OrdinalIgnoreCase) ||
                    arg.Equals("-help", StringComparison.OrdinalIgnoreCase) ||
                    arg.Equals("/help", StringComparison.OrdinalIgnoreCase))
                {
                    ShowUsage();
                }
                else if (!File.Exists(arg))
                {
                    Console.WriteLine("setting file {0} does not exist.", arg);
                }
                else
                {
                    try
                    {
                        SaveSettings(arg);
                        Console.WriteLine("Completed saving the settings");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error encountered: {0}", ex.Message);
                    }
                }
            }
            else
            {
                ShowUsage();
            }
        }

        private static void ShowUsage()
        {
            Console.WriteLine(@"Usage:
    TestEnvConfig
        Show this help message

    TestEnvConfig -?
        Show this help message

    TestEnvConfig setting_file
        Run the program as a command line application
        The program reads the test configurations from the setting_file and 
        saves them locally. the passwords will be stored in the credential store

        The following is an example of a setting_file: 

<Configuration>
    <Instance Name=""sqlOnPrem"">
       <DataSource>SQL On-Prem servername</DataSource>
        <BackupMethod>RemoteShare</BackupMethod>
        <RemoteShare>SQL remote share</RemoteShare>
    </Instance>
    <Instance Name=""sqlAzure"">
        <DataSource>SQL Azure servername</DataSource>
        <BackupMethod>RemoteShare</BackupMethod>
        <RemoteShare>SQLAzure remote share</RemoteShare>
        <UserId>user id</UserId>
        <Password>password</Password>
    </Instance>
</Configuration>
");
        }

        private static void SaveSettings(string settingFile)
        {
            Console.WriteLine($"settings file content: {File.ReadAllText(settingFile)}");
            var xdoc = XDocument.Load(settingFile);
            List<InstanceInfo> settings = new List<InstanceInfo>();
            foreach (var setting in xdoc.Descendants("Instance"))
            {
                var passwordEnvVariableValue = Environment.GetEnvironmentVariable((setting.Attribute("VersionKey").Value + "_password"));

                settings.Add(new InstanceInfo(setting.Attribute("VersionKey").Value)
                {
                    ServerName = setting.Element("DataSource").Value, // DataSource is required
                    ConnectTimeoutAsString = (string)setting.Element("ConnectTimeout"), //ConnectTimeout is optional
                    User = (string)setting.Element("UserId"), // UserID is optional
                    Password = string.IsNullOrEmpty(passwordEnvVariableValue) ? (string)setting.Element("Password") : passwordEnvVariableValue,
                    RemoteSharePath = (string)setting.Element("RemoteShare"), // RemoteShare is optional
                    AuthenticationType = string.IsNullOrEmpty((string)setting.Element("UserId")) ? AuthenticationType.Integrated : AuthenticationType.SqlLogin
                });
            }

            TestConfigPersistenceHelper.Write(settings);
        }
    }
}
