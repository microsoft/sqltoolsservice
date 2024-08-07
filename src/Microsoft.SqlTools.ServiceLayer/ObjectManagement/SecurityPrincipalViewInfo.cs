//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    public abstract class SecurityPrincipalViewInfo : SqlObjectViewInfo
    {
        public SecurableTypeMetadata[]? SupportedSecurableTypes { get; set; }
    }
}