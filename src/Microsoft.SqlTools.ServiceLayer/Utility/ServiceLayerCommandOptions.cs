//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Microsoft.SqlTools.Hosting.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Utility
{
    class ServiceLayerCommandOptions : CommandOptions
    {
        private static string ServiceLayerServiceName = "MicrosoftSqlToolsServiceLayer.exe";
        public ServiceLayerCommandOptions(string[] args) : base(args, ServiceLayerServiceName)
        {
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
