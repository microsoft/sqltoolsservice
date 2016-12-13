//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
namespace Microsoft.SqlTools.ServiceLayer.LanguageServices
{
    /// <summary>
    /// /// Result object for PeekDefinition
    /// </summary>
    public class DefinitionResult
    {
        /// <summary>
        /// True, if definition error occured
        /// </summary>
        public bool IsErrorResult;

        /// <summary>
        /// Error message, if any
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Location object representing the definition script file
        /// </summary>
        public Location[] Locations;
    }
}