//
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

        public static readonly string SqlLoginAuthenticationType = "SqlLogin";

        static Constants()
        {
            JsonSerializerSettings = new JsonSerializerSettings();

            // Camel case TracingLevel object properties
            JsonSerializerSettings.ContractResolver =
                new CamelCasePropertyNamesContractResolver();
        }
    }
}
