//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.Hosting.Contracts.Internal
{
    /// <summary>
    /// Defines an event that is sent from the client to notify that
    /// the client is exiting and the server should as well.
    /// </summary>
    public class ExitNotification
    {
        public static readonly EventType<object> Type = EventType<object>.Create("exit");
    }
}