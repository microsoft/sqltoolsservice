//------------------------------------------------------------------------------
//         Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------------------------
using System;

namespace Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection
{
    /// <summary>
    /// This interface controls the lifetime of settings created as part of the
    /// top-of-stack API.  Changes made to this context's AmbientData instance will
    /// flow to lower in the stack while this object is not disposed.
    /// </summary>
    internal interface IStackSettingsContext : IDisposable
    {
        AmbientSettings.AmbientData Settings { get; }
    }
}
