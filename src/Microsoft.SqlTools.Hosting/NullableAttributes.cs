//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#if !NETCOREAPP
namespace System.Diagnostics.CodeAnalysis
{
    [global::System.AttributeUsage(global::System.AttributeTargets.Field | global::System.AttributeTargets.Parameter | global::System.AttributeTargets.Property | global::System.AttributeTargets.ReturnValue, Inherited = false)]
    internal sealed class MaybeNullAttribute : global::System.Attribute
    {
    }
}
#endif
