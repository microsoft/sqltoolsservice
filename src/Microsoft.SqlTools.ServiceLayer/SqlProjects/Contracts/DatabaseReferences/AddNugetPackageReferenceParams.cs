//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts
{
    /// <summary>
    /// Parameters for adding a NuGet package reference to a SQL project
    /// </summary>
    public class AddNugetPackageReferenceParams : AddUserDatabaseReferenceParams
    {
        /// <summary>
        /// NuGet package name
        /// </summary>
        public string PackageName { get; set; }

        /// <summary>
        /// NuGet package version
        /// </summary>
        public string PackageVersion { get; set; }
    }

    /// <summary>
    /// Add a NuGet package reference to a project
    /// </summary>
    public class AddNugetPackageReferenceRequest
    {
        public static readonly RequestType<AddNugetPackageReferenceParams, ResultStatus> Type = RequestType<AddNugetPackageReferenceParams, ResultStatus>.Create("sqlprojects/addNugetPackageReference");
    }
}