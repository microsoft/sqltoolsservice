﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;

namespace Microsoft.SqlTools.ServiceLayer.FileBrowser
{
    /// <summary>
    /// Exception raised from file browser operation
    /// </summary>
    internal sealed class FileBrowserException : Exception
    {
        internal FileBrowserException(string m) : base(m)
        {
        }
    }
}