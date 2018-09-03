//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("Microsoft.SqlTools.Hosting.UnitTests")]
[assembly: InternalsVisibleTo("Microsoft.SqlTools.Services.UnitTests")]

// Allowing internals visible access to Moq library to help testing
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

