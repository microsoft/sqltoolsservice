//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("SqlTools Hosting Library")]
[assembly: AssemblyDescription("Provides hosting services for SqlTools applications.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Microsoft")]
[assembly: AssemblyProduct("SqlTools Hosting Library")]
[assembly: AssemblyCopyright("� Microsoft Corporation. All rights reserved.")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("6ECAFE73-131A-4221-AA13-C9BDE07FD92B")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("1.0.0.0")]

[assembly: InternalsVisibleTo("Microsoft.SqlTools.ServiceLayer.UnitTests")]
[assembly: InternalsVisibleTo("Microsoft.SqlTools.ServiceLayer.IntegrationTests")]
[assembly: InternalsVisibleTo("Microsoft.SqlTools.ServiceLayer.Test.Common")]

// Temporary work around for SRGen protection issues
[assembly: InternalsVisibleTo("MicrosoftSqlToolsServiceLayer")]
[assembly: InternalsVisibleTo("MicrosoftKustoServiceLayer")]
[assembly: InternalsVisibleTo("MicrosoftSqlToolsCredentials")]
