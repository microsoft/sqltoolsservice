//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.ShowPlan.ShowPlanGraph
{
    public class Description
    {
        #region Properties

        public string Title
        {
            get { return this.title; }
            set
            {
                this.title = value.Trim().Replace(NewLine, " ");
            }
        }

        public string QueryText
        {
            get { return this.queryText; }
            set
            {
                string text = value.Trim();
                this.queryText = text.Replace(NewLine, " ");
            }
        }

        public string ClusteredMode
        {
            get { return this.clusteredMode; }
            set
            {
                this.clusteredMode = value.Trim().Replace(NewLine, " ");
            }
        }

        public bool IsClusteredMode
        {
            set
            {
                this.isClusteredMode = value;
            }
        }

        public bool HasMissingIndex
        {
            get { return this.hasMissingIndex; }
            set { this.hasMissingIndex = value; }
        }

        public List<MissingIndex> MissingIndices
        {
            get { return this.missingIndexes; }
            set 
            {
                this.missingIndexes = value;
            }
        }

        #endregion

        #region Member variables

        private string title = string.Empty;
        private string queryText = string.Empty;
        private string toolTipQueryText = string.Empty;
        private string clusteredMode = string.Empty;
        private bool isClusteredMode = false;
        private bool hasMissingIndex = false;
        private List<MissingIndex> missingIndexes = new List<MissingIndex>();
        private const string NewLine = "\r\n";

        #endregion
    }

    public class MissingIndex
    {
        public string MissingIndexCaption
        {
            get { return this.missingIndexCaption; }
            set
            {
                this.missingIndexCaption = value;
            }
        }
        public string MissingIndexQueryText
        {
            get { return this.missingIndexQueryText; }
            set
            {
                this.missingIndexQueryText = value;
            }
        }

        public string MissingIndexImpact
        {
            get { return this.missingIndexImpact; }
            set
            {
                this.missingIndexImpact = value;
            }
        }

        public string MissingIndexDatabase
        {
            get { return this.missingIndexDatabase; }
            set
            {
                this.missingIndexDatabase = value;
            }
        }

        private string missingIndexCaption = string.Empty;    // actual caption text that will be displayed on the screen
        private string missingIndexQueryText = string.Empty;  // create index query
        private string missingIndexImpact = string.Empty;     // impact
        private string missingIndexDatabase = string.Empty;   // database context
    }
}