﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

namespace Microsoft.SqlTools.ServiceLayer.SqlContext
{
    /// <summary>
    /// Contract for receiving object explorer settings as part of workspace settings
    /// </summary>
    public class ObjectExplorerSettings
    {
        public static int DefaultCreateSessionTimeout = 45;
        public static int DefaultExpandTimeout = 45;
        public static bool DefaultGroupBySchema = false;

        public ObjectExplorerSettings()
        {
            CreateSessionTimeout = DefaultCreateSessionTimeout;
            ExpandTimeout = DefaultExpandTimeout;
            GroupBySchema = DefaultGroupBySchema;
        }

        /// <summary>
        /// Number of seconds to wait before fail create session request with timeout error
        /// </summary>
        public int CreateSessionTimeout { get; set; }

        /// <summary>
        /// Number of seconds to wait before fail expand request with timeout error
        /// </summary>
        public int ExpandTimeout { get; set; }

        /// <summary>
        /// Moves Schema to the top level of OE and then move schema-bound nodes under it.
        /// </summary>
        /// <value></value>
        public bool GroupBySchema { get; set; }
    }
}
