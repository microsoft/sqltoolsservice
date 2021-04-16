//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Common;
using Microsoft.SqlTools.ResourceProvider.Core;
using Moq;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ResourceProvider
{
    /// <summary>
    /// Tests for ExceptionUtil to verify the helper and extension methods
    /// </summary>
    public class ExceptionUtilTest
    {
        [Test]
        public void IsSqlExceptionShouldReturnFalseGivenNullException()
        {
            Exception exception = null;
            bool expected = false;
            bool actual = exception.IsDbException();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void IsSqlExceptionShouldReturnFalseGivenNonSqlException()
        {
            Exception exception = new ApplicationException();
            bool expected = false;
            bool actual = exception.IsDbException();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void IsSqlExceptionShouldReturnFalseGivenNonSqlExceptionWithInternalException()
        {
            Exception exception = new ApplicationException("Exception message", new ServiceFailedException());
            bool expected = false;
            bool actual = exception.IsDbException();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void IsSqlExceptionShouldReturnTrueGivenSqlException()
        {
            Exception exception = CreateDbException();
            Assert.NotNull(exception);

            bool expected = true;
            bool actual = exception.IsDbException();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void IsSqlExceptionShouldReturnTrueGivenExceptionWithInnerSqlException()
        {
            Exception exception = new ApplicationException("", CreateDbException());
            Assert.NotNull(exception);

            bool expected = true;
            bool actual = exception.IsDbException();
            Assert.AreEqual(expected, actual);
        }

        private Exception CreateDbException()
        {
            return new Mock<DbException>().Object;
        }
    }
}
