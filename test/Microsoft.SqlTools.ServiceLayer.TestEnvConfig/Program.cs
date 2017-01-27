//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.SqlTools.ServiceLayer.Test.Common;

namespace Microsoft.SqlTools.ServiceLayer.TestEnvConfig
{
    class Program
    {
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
                else if (File.Exists(arg) == false)
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
    <Instance Name=""defaultSql2005"">
        <DataSource>SQL2005 servername</DataSource>
        <BackupMethod>RemoteShare</BackupMethod>
        <RemoteShare>SQL 2005 remote share</RemoteShare>
    </Instance>
    <Instance Name=""defaultSql2008"">
       <DataSource>SQL2008 servername</DataSource>
        <BackupMethod>RemoteShare</BackupMethod>
        <RemoteShare>SQL 2008 remote share</RemoteShare>
    </Instance>
    <Instance Name=""defaultSql2011"">
       <DataSource>SQL2011 servername</DataSource>
        <BackupMethod>RemoteShare</BackupMethod>
        <RemoteShare>SQL 20011 remote share</RemoteShare>
    </Instance>
    <Instance Name=""defaultSqlAzureV12"">
        <DataSource>SQLAzure servername</DataSource>
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
            
            var xdoc = XDocument.Load(settingFile);
            var settings =
                from setting in xdoc.Descendants("Instance")
                select new InstanceInfo(setting.Attribute("VersionKey").Value) // VersionKey is required
                {
                    ServerName = setting.Element("DataSource").Value, // DataSource is required
                    ConnectTimeoutAsString = (string)setting.Element("ConnectTimeout"), //ConnectTimeout is optional
                    User = (string)setting.Element("UserId"), // UserID is optional
                    Password = (string)setting.Element("Password"),
                    RemoteSharePath = (string)setting.Element("RemoteShare"), // RemoteShare is optional
                    AuthenticationType = string.IsNullOrEmpty((string)setting.Element("UserId")) ? AuthenticationType.Integrated : AuthenticationType.SqlLogin
                };

            TestConfigPersistenceHelper.Write(settings);
            
        }
    }
}
