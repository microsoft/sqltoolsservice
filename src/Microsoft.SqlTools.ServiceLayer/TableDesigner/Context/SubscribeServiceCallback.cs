//------------------------------------------------------------------------------
// <copyright file="SubscribeServiceCallback.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;

namespace Microsoft.Data.Tools.Design.Core.Context
{

    /// <summary>
    /// A delegate that is a callback for service subscriptions.
    /// </summary>
    /// <param name="serviceType">The type of service that has just been published.</param>
    /// <param name="serviceInstance">The instance of the service.</param>
    public delegate void SubscribeServiceCallback(Type serviceType, object serviceInstance);

    /// <summary>
    /// A generic delegate that is a callback for service subscriptions
    /// </summary>
    /// <typeparam name="ServiceType">The type of service to listen to.</typeparam>
    /// <param name="serviceInstance">The instance of the service.</param>
    public delegate void SubscribeServiceCallback<TServiceType>(TServiceType serviceInstance);
}
