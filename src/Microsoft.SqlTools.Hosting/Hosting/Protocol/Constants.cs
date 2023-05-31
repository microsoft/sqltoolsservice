﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.SqlTools.Hosting.Protocol
{
    public static class Constants
    {
        public const string ContentLengthFormatString = "Content-Length: {0}\r\n\r\n";
        public static readonly JsonSerializerSettings JsonSerializerSettings;

        public static readonly string DefaultApplicationName = "azdata";
        public static readonly string SqlLoginAuthenticationType = "SqlLogin";

        // Feature names
        public static readonly string LanguageServiceFeature = "languageService";

        static Constants()
        {
            JsonSerializerSettings = new JsonSerializerSettings();
            JsonSerializerSettings.TypeNameHandling = TypeNameHandling.None;

            // Camel case all object properties
            JsonSerializerSettings.ContractResolver =
                new CamelCasePropertyNamesContractResolver();
        }
    }
}
