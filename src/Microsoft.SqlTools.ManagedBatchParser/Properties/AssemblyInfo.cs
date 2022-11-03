//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Reflection;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Microsoft.SqlTools.ManagedBatchParser")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("Microsoft.SqlTools.ManagedBatchParser")]
[assembly: AssemblyCopyright("Copyright © 2022")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("82dd9738-2ad3-4eb3-9f80-18b594e03621")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
[assembly: AssemblyVersion("3.0.0.0")]
[assembly: AssemblyFileVersion("3.0.0.0")]
#if NET6_0_OR_GREATER
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Microsoft.SqlTools.ServiceLayer.UnitTests")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Microsoft.SqlTools.ServiceLayer.IntegrationTests")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Microsoft.SqlTools.ServiceLayer.Test.Common")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("MicrosoftSqlToolsServiceLayer")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("MicrosoftKustoServiceLayer")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Microsoft.SqlTools.ManagedBatchParser.IntegrationTests")]
#endif