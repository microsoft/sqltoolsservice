//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.Dmp.Hosting.Extensibility
{
    /// <summary>
    /// A Service that expects to lookup other services. Using this interface on an exported service
    /// will ensure the <see cref="SetServiceProvider(IMultiServiceProvider)"/> method is called during
    /// service initialization
    /// </summary>
    public interface IComposableService
    {
        /// <summary>
        /// Supports settings the service provider being used to initialize the service.
        /// This is useful to look up other services and use them in your own service.
        /// </summary>
        void SetServiceProvider(IMultiServiceProvider provider);
    }
}
