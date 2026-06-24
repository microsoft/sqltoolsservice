//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

namespace Microsoft.SqlTools.LanguageService.LanguageServices
{
    /// <summary>
    /// Abstraction over a connection's identity and intellisense metrics as needed by the
    /// language service binding queue and completion service. Implemented by the connection
    /// information type owned by the hosting service layer.
    /// </summary>
    public interface IConnectionInfo
    {
        /// <summary>
        /// Gets the URI identifying the document/owner of the connection.
        /// </summary>
        string OwnerUri { get; }

        /// <summary>
        /// Gets a unique key describing the connection, used to look up the binding context.
        /// </summary>
        string ConnectionContextKey { get; }

        /// <summary>
        /// Gets the intellisense interaction metrics for the connection.
        /// </summary>
        InteractionMetrics<double> IntellisenseMetrics { get; }
    }
}
