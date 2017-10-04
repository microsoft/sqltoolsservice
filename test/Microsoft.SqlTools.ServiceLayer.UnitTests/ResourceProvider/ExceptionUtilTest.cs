//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Common;
using Microsoft.SqlTools.ResourceProvider.Core;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ResourceProvider
{
    /// <summary>
    /// Tests for ExceptionUtil to verify the helper and extension methods
    /// </summary>
    public class ExceptionUtilTest
    {
        [Fact]
        public void IsSqlExceptionShouldReturnFalseGivenNullException()
        {
            Exception exception = null;
            bool expected = false;
            bool actual = exception.IsDbException();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void IsSqlExceptionShouldReturnFalseGivenNonSqlException()
        {
            Exception exception = new ApplicationException();
            bool expected = false;
            bool actual = exception.IsDbException();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void IsSqlExceptionShouldReturnFalseGivenNonSqlExceptionWithInternalException()
        {
            Exception exception = new ApplicationException("Exception message", new ServiceFailedException());
            bool expected = false;
            bool actual = exception.IsDbException();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void IsSqlExceptionShouldReturnTrueGivenSqlException()
        {
            Exception exception = CreateDbException();
            Assert.NotNull(exception);

            bool expected = true;
            bool actual = exception.IsDbException();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void IsSqlExceptionShouldReturnTrueGivenExceptionWithInnerSqlException()
        {
            Exception exception = new ApplicationException("", CreateDbException());
            Assert.NotNull(exception);

            bool expected = true;
            bool actual = exception.IsDbException();
            Assert.Equal(expected, actual);
        }

        private Exception CreateDbException()
        {
            return new Mock<DbException>().Object;
        }
    }
}
