//------------------------------------------------------------------------------
// <copyright file="SubscribeContextCallback.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;

namespace Microsoft.Data.Tools.Design.Core.Context
{

    /// <summary>
    /// Defines a callback method that will be invoked when a context item
    /// changes.
    /// </summary>
    /// <param name="item">The context item that has changed.</param>
    public delegate void SubscribeContextCallback(ContextItem item);

    /// <summary>
    /// Defines a callback method that will be invoked when a context item
    /// changes.
    /// </summary>
    /// <typeparam name="ContextItemType">The type of context item this subscription is for.</typeparam>
    /// <param name="item">The context item that has changed.</param>
    public delegate void SubscribeContextCallback<TContextItemType>(
        TContextItemType item) where TContextItemType : ContextItem;

}
