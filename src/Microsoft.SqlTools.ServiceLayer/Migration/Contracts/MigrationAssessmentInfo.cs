//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.Migration.Contracts
{
    /// <summary>
    /// Describes an item returned by SQL Assessment RPC methods
    /// </summary>
    public class MigrationAssessmentInfo
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
        /// Gets or sets assessment ruleset name
        /// </summary>
        public string RuleId { get; set; }

        /// <summary>
        /// Gets or sets the assessed object's name. 
        /// </summary>
        public string TargetType { get; set; }

        /// <summary>
        /// Gets or sets the database name.
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// Gets or sets the server name.
        /// </summary>
        public string ServerName { get; set; }

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

        public string Message { get; set; }

        public string AppliesToMigrationTargetPlatform { get; set; }

        public string IssueCategory { get; set; }

        public ImpactedObjectInfo[] ImpactedObjects { get; set; }
        
        /// <summary>
        /// This flag is set if the assessment result is a blocker for migration to Target Platform.
        /// </summary>
        public bool DatabaseRestoreFails { get; set; }
    }
}
