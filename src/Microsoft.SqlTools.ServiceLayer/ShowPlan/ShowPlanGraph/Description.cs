//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
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
                this.title = value.Trim().Replace(Environment.NewLine, " ");
            }
        }

        public string QueryText
        {
            get { return this.queryText; }
            set
            {
                string text = value.Trim();
                this.queryText = text.Replace(Environment.NewLine, " ");
            }
        }

        public string ClusteredMode
        {
            get { return this.clusteredMode; }
            set
            {
                this.clusteredMode = value.Trim().Replace(Environment.NewLine, " ");
            }
        }

        public bool IsClusteredMode
        {
            get { return this.isClusteredMode; }
            set
            {
                this.isClusteredMode = value;
            }
        }

        public List<MissingIndex> MissingIndices { get; set; }

        #endregion

        #region Member variables

        private string title = string.Empty;
        private string queryText = string.Empty;
        private string toolTipQueryText = string.Empty;
        private string clusteredMode = string.Empty;
        private bool isClusteredMode = false;
        #endregion
    }

    public class MissingIndex
    {
        public string MissingIndexCaption { get; set; }
        public string MissingIndexQueryText { get; set; }
        public string MissingIndexImpact { get; set; }
        public string MissingIndexDatabase { get; set; }
    }
}