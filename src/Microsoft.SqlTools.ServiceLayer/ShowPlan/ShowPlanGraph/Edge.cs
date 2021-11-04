//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.ComponentModel;
using System.Globalization;

namespace Microsoft.SqlTools.ServiceLayer.ShowPlan.ShowPlanGraph
{
    public class Edge
	{
		#region Constructor

        public Node FromNode;
        public Node ToNode;

		public Edge(Node fromNode, Node toNode)
		{
            Initialize(toNode as Node);
        }

		#endregion

        #region Public properties and methods

        /// <summary>
        /// Gets Edge properties.
        /// </summary>
        public PropertyDescriptorCollection Properties
        {
            get { return this.properties; }
        }

        /// <summary>
        /// Gets or sets node property value.
        /// </summary>
        public object this[string propertyName]
        {
            get
            {
                PropertyValue property = this.properties[propertyName] as PropertyValue;
                return property != null ? property.Value : null;
            }

            set
            {
                PropertyValue property = this.properties[propertyName] as PropertyValue;
                if (property != null)
                {
                    // Overwrite existing property value
                    property.Value = value;
                }
                else
                {
                    // Add new property
                    this.properties.Add(PropertyFactory.CreateProperty(propertyName, value));
                }
            }
        }

        public double RowSize
        {
            get
            {
                object propertyValue = this["AvgRowSize"];
                return propertyValue != null ? Convert.ToDouble(propertyValue, CultureInfo.CurrentCulture) : 0;
            }
        }

        public double RowCount
        {
            get
            {
                if(this["ActualRowsRead"] ==  null && this["ActualRows"] == null)
                {
                    // If Actual Row count and ActualRowsRead are not set, default to estimated row count
                    return EstimatedRowCount;
                }
                else
                {
                    // at least one of ActualRowsRead and ActualRows is set
                    double actualRowsReadValue = 0;
                    double actualRowsValue = 0;
                    if (this["ActualRowsRead"] != null)
                    {
                        actualRowsReadValue = Convert.ToDouble(this["ActualRowsRead"].ToString(), CultureInfo.CurrentCulture);
                    }
                    if (this["ActualRows"] != null)
                    {
                        actualRowsValue = Convert.ToDouble(this["ActualRows"].ToString(), CultureInfo.CurrentCulture);
                    }
                    return actualRowsReadValue > actualRowsValue ? actualRowsReadValue : actualRowsValue;
                }
            }
        }

        public double EstimatedRowCount
        {
            get
            {
                object propertyValue = this["EstimateRows"];
                if (propertyValue == null)
                {
                    propertyValue = this["StatementEstRows"];
                }

                return propertyValue != null ? Convert.ToDouble(propertyValue, CultureInfo.CurrentCulture) : 0;
            }
        }

        public double EstimatedDataSize
        {
            get
            {
                object propertyValue = this["EstimatedDataSize"];
                return propertyValue != null ? Convert.ToDouble(propertyValue, CultureInfo.CurrentCulture) : 0;
            }

            private set
            {
                this["EstimatedDataSize"] = value;
            }
        }

        #endregion

		#region Implementation details

        /// <summary>
        /// Copy some of edge properties from the node connected through this edge.
        /// </summary>
        /// <param name="node">The node connected on the right side of the edge.</param>
        private void Initialize(Node node)
        {
            this.properties = new PropertyDescriptorCollection(new PropertyDescriptor[] {});

            string[] propertyNames = new string[]
            {
                "ActualRows",
                "ActualRowsRead",
                "AvgRowSize",
                "EstimateRows",
                "EstimateRowsAllExecs",
                "StatementEstRows" 
            };

            // Copy properties
            foreach (string propertyName in propertyNames)
            {
                object value = node[propertyName];
                if (value != null)
                {
                    this[propertyName] = value;
                }
            }

            this.EstimatedDataSize = this.RowSize * this.EstimatedRowCount;
        }

		#endregion

        #region Private variables

        private PropertyDescriptorCollection properties;

        #endregion
    }
}
