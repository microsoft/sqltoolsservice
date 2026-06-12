//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.Sts2.Contracts
{
    /// <summary>Wire-level constants pinned by docs/sts2/SPEC.md §7.</summary>
    public static class Sts2WireConstants
    {
        /// <summary>The STS2 spec version returned by <c>v2/initialize</c> and <c>v2/diagnostics.ping</c>.</summary>
        public const string SpecVersion = "2.0.0-preview.1";

        /// <summary>Prefix that routes a JSON-RPC method to STS2 (SPEC §6.2).</summary>
        public const string MethodPrefix = "v2/";
    }
}
