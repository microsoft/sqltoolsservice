//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.Utility;

namespace Microsoft.Kusto.ServiceLayer
{
    /// <summary>
    /// A class to calculate the value for the metrics using the given bucket
    /// </summary>
    public class InteractionMetrics<T>
    {
        /// <summary>
        /// Creates new instance given a bucket of metrics
        /// </summary>
        public InteractionMetrics(int[] metrics)
        {
            Validate.IsNotNull("metrics", metrics);
            if(metrics.Length == 0)
            {
                throw new ArgumentOutOfRangeException("metrics");
            }

            Counters = new ConcurrentDictionary<string, T>();
            if (!IsSorted(metrics))
            {
                Array.Sort(metrics);
            }
            Metrics = metrics;
        }

        private ConcurrentDictionary<string, T> Counters { get; }

        private object perfCountersLock = new object();

        /// <summary>
        /// The metrics bucket
        /// </summary>
        public int[] Metrics { get; private set; }

        /// <summary>
        /// Returns true if the given list is sorted
        /// </summary>
        private bool IsSorted(int[] metrics)
        {
            if (metrics.Length > 1)
            {
                int previous = metrics[0];
                for (int i = 1; i < metrics.Length; i++)
                {
                    if(metrics[i] < previous)
                    {
                        return false;
                    }
                    previous = metrics[i];
                }
            }
            return true;
        }

        /// <summary>
        /// Update metric value given new number
        /// </summary>
        public void UpdateMetrics(double duration, T newValue, Func<string, T, T> updateValueFactory)
        {
            int metric = Metrics[Metrics.Length - 1];
            for (int i = 0; i < Metrics.Length; i++)
            {
                if (duration <= Metrics[i])
                {
                    metric = Metrics[i];
                    break;
                }
            }
            string key = metric.ToString();
            Counters.AddOrUpdate(key, newValue, updateValueFactory);
        }

        /// <summary>
        /// Returns the quantile
        /// </summary>
        public Dictionary<string, T> Quantile
        {
            get
            {
                return Counters.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }
        }
    }
}

