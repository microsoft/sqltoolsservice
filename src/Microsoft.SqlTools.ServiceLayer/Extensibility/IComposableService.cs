//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


namespace Microsoft.SqlTools.ServiceLayer.Extensibility
{
    /// <summary>
    /// A Service that expects to lookup other services. 
    /// </summary>
    interface IComposableService
    {
        void SetServiceProvider(IMultiServiceProvider provider);
    }
}
