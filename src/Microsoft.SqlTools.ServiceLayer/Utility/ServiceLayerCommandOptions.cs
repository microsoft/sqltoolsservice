//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Linq;
using Microsoft.SqlTools.Hosting.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Utility
{
    class ServiceLayerCommandOptions : CommandOptions
    {
        internal const string ServiceLayerServiceName = "MicrosoftSqlToolsServiceLayer.exe";

        private static readonly string[] serviceLayerCommandArgs = { "-d", "--developers" };

        /**
         * List of contributors to this project, used as part of the onboarding process.
         */
        private readonly string[] contributors = new string[] {
            // Put your Github username here!
            "Charles-Gagnon"
            };

        public ServiceLayerCommandOptions(string[] args) : base(args.Where(arg => !serviceLayerCommandArgs.Contains(arg)).ToArray(), ServiceLayerServiceName)
        {
            for (int i = 0; i < args.Length; ++i)
            {
                string arg = args[i].ToLowerInvariant();

                switch (arg)
                {
                    case "-d":
                    case "--developers":
                        Console.WriteLine();
                        Console.WriteLine("**********************************************************************************");
                        Console.WriteLine("These are some of the developers who have contributed to this project - thank you!");
                        Console.WriteLine("**********************************************************************************");
                        Console.WriteLine();
                        Console.WriteLine(string.Join(Environment.NewLine, contributors.Select(contributor => $"\t{contributor}")));
                        this.ShouldExit = true;
                        break;
                }
            }
        }

        public override void SetLocale(string locale)
        {
            try
            {
                LocaleSetter(locale);

                // Setting our internal SR culture to our global culture
                SR.Culture = CultureInfo.CurrentCulture;
            }
            catch (CultureNotFoundException)
            {
                // Ignore CultureNotFoundException since it only is thrown before Windows 10.  Windows 10,
                // along with macOS and Linux, pick up the default culture if an invalid locale is passed
                // into the CultureInfo constructor.
            }
        }

    }
}
