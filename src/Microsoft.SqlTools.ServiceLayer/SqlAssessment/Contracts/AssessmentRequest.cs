//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;

using Microsoft.SqlServer.Management.Assessment;

namespace Microsoft.SqlTools.ServiceLayer.SqlAssessment.Contracts
{
    /// <summary>
    /// Parameters for executing a query from a provided string
    /// </summary>
    public class AssessmentParams
    {
        /// <summary>
        /// Gets or sets the owner uri to get connection from
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Gets or sets the target type
        /// </summary>
        public SqlObjectType TargetType { get; set; }
    }
    
    /// <summary>
    /// Describes an item returned by SQL Assessment RPC methods
    /// </summary>
    public class AssessmentItemInfo
    {
        /// <summary>
        /// Gets or sets assessment ruleset version.
        /// </summary>
        public string RulesetVersion { get; set; }

        /// <summary>
        /// Gets or sets assessment ruleset name
        /// </summary>
        public string RulesetName { get; set; }

        /// <summary>
        /// Gets or sets assessed target's type.
        /// Supported values: 1 - server, 2 - database.
        /// </summary>
        public SqlObjectType TargetType { get; set; }

        /// <summary>
        /// Gets or sets the assessed object's name. 
        /// </summary>
        public string TargetName { get; set; }

        /// <summary>
        /// Gets or sets check's ID.
        /// </summary>
        public string CheckId { get; set; }

        /// <summary>
        /// Gets or sets tags assigned to this item.
        /// </summary>
        public string[] Tags { get; set; }

        /// <summary>
        /// Gets or sets a display name for this item.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets a brief description of the item's purpose.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="string"/> containing
        /// an link to a page providing detailed explanation
        /// of the best practice.
        /// </summary>
        public string HelpLink { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="string"/> indicating
        /// severity level assigned to this items.
        /// Values are: "Information", "Warning", "Critical".
        /// </summary>
        public string Level { get; set; }
    }

    /// <summary>
    /// Generic SQL Assessment Result
    /// </summary>
    /// <typeparam name="T">
    /// Result item's type derived from <see cref="AssessmentItemInfo"/>
    /// </typeparam>
    public class AssessmentResultData<T>
        where T : AssessmentItemInfo
    {
        /// <summary>
        /// Gets the collection of assessment results.
        /// </summary>
        public List<T> Items { get; } = new List<T>();

        /// <summary>
        /// Gets or sets SQL Assessment API version.
        /// </summary>
        public string ApiVersion { get; set; }
    }

    /// <summary>
    /// Generic SQL Assessment Result
    /// </summary>
    /// <typeparam name="T">
    /// Result item's type derived from <see cref="AssessmentItemInfo"/>
    /// </typeparam>
    public class AssessmentResult<T> : AssessmentResultData<T>
        where T : AssessmentItemInfo
    {
        /// <summary>
        /// Gets or sets a value indicating
        /// if assessment operation was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets an status message for the operation.
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}
