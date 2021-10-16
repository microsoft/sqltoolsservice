//------------------------------------------------------------------------------
// <copyright file="PublishServiceCallback.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;

namespace Microsoft.Data.Tools.Design.Core.Context
{

    /// <summary>
    /// A delegate that is called back when an object should publish an instance of a
    /// service.
    /// </summary>
    /// <param name="serviceType">The type of service to be published.</param>
    /// <returns>An instance of serviceType.</returns>
    public delegate object PublishServiceCallback(Type serviceType);

    /// <summary>
    /// A generic delegate that is called back when an object should publish an 
    /// instance of a service.
    /// </summary>
    /// <typeparam name="ServiceType">The type of service to be published.</typeparam>
    /// <returns>An instance of ServiceType.</returns>
    public delegate TServiceType PublishServiceCallback<TServiceType>();
}
