//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.LanguageServer
{
    public class InteractionMetricsTest
    {
        [Test]
        public void MetricsShouldGetSortedGivenUnSortedArray()
        {
            int[] metrics = new int[] { 4, 8, 1, 11, 3 };
            int[] expected = new int[] { 1, 3, 4, 8, 11 };
            InteractionMetrics<int> interactionMetrics = new InteractionMetrics<int>(metrics);

            Assert.AreEqual(interactionMetrics.Metrics, expected);
        }

        [Test]
        public void MetricsShouldThrowExceptionGivenNullInput()
        {
            int[] metrics = null;
            Assert.Throws<ArgumentNullException>(() => new InteractionMetrics<int>(metrics));
        }

        [Test]
        public void MetricsShouldThrowExceptionGivenEmptyInput()
        {
            int[] metrics = new int[] { };
            Assert.Throws<ArgumentOutOfRangeException>(() => new InteractionMetrics<int>(metrics));
        }

        [Test]
        public void MetricsShouldNotChangeGivenSortedArray()
        {
            int[] metrics = new int[] { 1, 3, 4, 8, 11 };
            int[] expected = new int[] { 1, 3, 4, 8, 11 };
            InteractionMetrics<int> interactionMetrics = new InteractionMetrics<int>(metrics);

            Assert.AreEqual(interactionMetrics.Metrics, expected);
        }

        [Test]
        public void MetricsShouldNotChangeGivenArrayWithOneItem()
        {
            int[] metrics = new int[] { 11 };
            int[] expected = new int[] { 11 };
            InteractionMetrics<int> interactionMetrics = new InteractionMetrics<int>(metrics);

            Assert.AreEqual(interactionMetrics.Metrics, expected);
        }

        [Test]
        public void MetricsCalculateQuantileCorrectlyGivenSeveralUpdates()
        {
            int[] metrics = new int[] { 50, 100, 300, 500, 1000, 2000 };
            Func<string, double, double> updateValueFactory = (k, current) => current + 1;
            InteractionMetrics<double> interactionMetrics = new InteractionMetrics<double>(metrics);
            interactionMetrics.UpdateMetrics(54.4, 1, updateValueFactory);
            interactionMetrics.UpdateMetrics(345, 1, updateValueFactory);
            interactionMetrics.UpdateMetrics(23, 1, updateValueFactory);
            interactionMetrics.UpdateMetrics(51, 1, updateValueFactory);
            interactionMetrics.UpdateMetrics(500, 1, updateValueFactory);
            interactionMetrics.UpdateMetrics(4005, 1, updateValueFactory);
            interactionMetrics.UpdateMetrics(2500, 1, updateValueFactory);
            interactionMetrics.UpdateMetrics(123, 1, updateValueFactory);

            Dictionary<string, double> quantile = interactionMetrics.Quantile;
            Assert.NotNull(quantile);
            Assert.AreEqual(5, quantile.Count);
            Assert.AreEqual(1, quantile["50"]);
            Assert.AreEqual(2, quantile["100"]);
            Assert.AreEqual(1, quantile["300"]);
            Assert.AreEqual(2, quantile["500"]);
            Assert.AreEqual(2, quantile["2000"]);

        }
    }
}
