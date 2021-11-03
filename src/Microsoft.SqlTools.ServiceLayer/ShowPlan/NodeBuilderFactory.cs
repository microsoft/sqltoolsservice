//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data;
using System.Diagnostics;

namespace Microsoft.SqlTools.ServiceLayer.ShowPlan
{
    /// <summary>
    /// Class that creates concrete INodeBuilder instances.
    /// </summary>
#if DEBUG || EXPOSE_MANAGED_INTERNALS
    public
#else
    internal
#endif
    static class NodeBuilderFactory
	{
        /// <summary>
        /// Instantiates a concrete node builder based on dataSource type
        /// </summary>
        /// <param name="dataSource">data</param>
        /// <returns></returns>
        public static INodeBuilder Create(object dataSource, ShowPlanType type)
        {
            if (dataSource is String || dataSource is byte[] || dataSource is ShowPlanXML)
            {
                // REVIEW: add the code that looks inside the XML
                // and validates the root node and namespace
                // REVIEW: consider using XmlTextReader
                return new XmlPlanNodeBuilder(type);
            }
            else if (dataSource is IDataReader)
            {
                // REVIEW: for now the assumption is that this is
                // a Shiloh Row set, either actual or estimated
                if (type == ShowPlanType.Actual)
                {
                    return new ActualPlanDataReaderNodeBuilder();
                }
                else if (type == ShowPlanType.Estimated)
                {
                    return new EstimatedPlanDataReaderNodeBuilder();
                }
                // else if (type == ShowPlanType.Live)
                // {
                //     return new LivePlanDataReaderNodeBuilder();
                // }
                else
                {
                    Debug.Assert(false, "Unexpected ShowPlan type");
                }
            }

            Debug.Assert(false, "Unexpected ShowPlan source = " + dataSource.ToString());
            throw new ArgumentException(SR.Keys.UnknownShowPlanSource);
        }
    }
}
