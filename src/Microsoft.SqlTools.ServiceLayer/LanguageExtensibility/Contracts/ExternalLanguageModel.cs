//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.SqlTools.ServiceLayer.LanguageExtensibility.Contracts
{

    public enum ExternalLanguagePlatform
    {
        None,
        Windows,
        Linux
    }

    /// <summary>
    /// Language metadata
    /// </summary>
    public class ExternalLanguage
    {

        /// <summary>
        /// Language Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Language Owner
        /// </summary>
        public string Owner { get; set; }

        public List<ExternalLanguageContent> Contents { get; set; }

        /// <summary>
        /// Created Date
        /// </summary>
        public string CreatedDate { get; set; }

    }

    public class ExternalLanguageContent
    {
        public bool IsLocalFile { get; set; }

        /// <summary>
        /// Path to extension file
        /// </summary>
        public string PathToExtension { get; set; }

        public object Content
        {
            get
            {
                if (IsLocalFile)
                {
                    return ExtensionFileBytes;
                }
                else
                {
                    return PathToExtension;
                }
            }
        }

        public byte[] ExtensionFileBytes
        {
            get
            {
                if (IsLocalFile)
                {
                    using (var stream = new FileStream(PathToExtension, FileMode.Open, FileAccess.Read))
                    {
                        using (var reader = new BinaryReader(stream))
                        {
                            return reader.ReadBytes((int)stream.Length);
                        }
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Extension file name
        /// </summary>
        public string ExtensionFileName { get; set; }

        /// <summary>
        /// Platform name
        /// </summary>
        public ExternalLanguagePlatform PlatformId
        {
            get
            {
                return string.IsNullOrWhiteSpace(Platform) ? ExternalLanguagePlatform.None : (ExternalLanguagePlatform)Enum.Parse(typeof(ExternalLanguagePlatform), Platform, true);
            }
        }

        public string Platform { get; set; }

        /// <summary>
        /// Extension parameters
        /// </summary>
        public string Parameters { get; set; }

        /// <summary>
        /// Environment variables
        /// </summary>
        public string EnvironmentVariables { get; set; }

    }
}
