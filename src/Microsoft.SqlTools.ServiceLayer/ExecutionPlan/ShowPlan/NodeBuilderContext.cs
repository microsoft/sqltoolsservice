//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.ShowPlan
{
    public class NodeBuilderContext
	{
        public NodeBuilderContext(ShowPlanGraph graph, ShowPlanType type, object context)
        {
            this.graph = graph;
            this.showPlanType = type;
            this.context = context;
        }

        /// <summary>
        /// Gets currently processing Graph
        /// </summary>
        public ShowPlanGraph Graph
        {
            get { return this.graph; }
        }

        /// <summary>
        /// Gets current ShowPlan type. 
        /// </summary>
        public ShowPlanType ShowPlanType
        {
            get { return this.showPlanType; }
        }

        /// <summary>
        /// Misc context object.
        /// </summary>
        public object Context
        {
            get { return this.context; }
        }

        private ShowPlanGraph graph;
        private ShowPlanType showPlanType;
        private object context;
    }
}
