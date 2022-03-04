//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Profiler.Contracts
{
    public class ProfilerSessionCreatedParams
    {
        public string OwnerUri { get; set; }

        public string SessionName { get; set; }

        public string TemplateName { get; set; }
    }

    public class ProfilerSessionCreatedNotification
    {
        public static readonly
            EventType<ProfilerSessionCreatedParams> Type =
            EventType<ProfilerSessionCreatedParams>.Create("profiler/sessioncreated");
    }
}
