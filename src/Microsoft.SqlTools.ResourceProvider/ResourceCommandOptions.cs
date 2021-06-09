//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Microsoft.SqlTools.Hosting.Utility;

namespace Microsoft.SqlTools.ResourceProvider
{
    class ResourceCommandOptions : CommandOptions
    {
        internal const string ResourceServiceName = "SqlToolsResourceProviderService.exe";

        public ResourceCommandOptions(string[] args) : base(args, ResourceServiceName)
        {
        }

        public override void SetLocale(string locale)
        {
            try
            {
                LocaleSetter(locale);

                // Setting our internal SR culture to our global culture
                SR.Culture = CultureInfo.CurrentCulture;

                // Setting Core SR culture to our global culture
                Microsoft.SqlTools.ResourceProvider.Core.SR.Culture = CultureInfo.CurrentCulture;

                 // Setting DefaultImpl SR culture to our global culture
                Microsoft.SqlTools.ResourceProvider.DefaultImpl.SR.Culture = CultureInfo.CurrentCulture;
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
