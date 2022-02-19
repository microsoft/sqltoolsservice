//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Moq;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.QueryExecution.DataStorage
{
    [TestFixture]
    public class SaveAsFileStreamFactoryTests
    {
        [Test]
        public void CsvConstructor_InvalidParameters()
        {
            ConstructorTest(new SaveResultsAsCsvRequestParams(), (qes, sp, fsFunc) => new SaveAsCsvFileStreamFactory(qes, sp, fsFunc));
        }

        [Test]
        public void ExcelConstructor_InvalidParameters()
        {
            ConstructorTest(new SaveResultsAsExcelRequestParams(), (qes, sp, fsFunc) => new SaveAsExcelFileStreamFactory(qes, sp, fsFunc));
        }

        [Test]
        public void JsonConstructor_InvalidParameters()
        {
            ConstructorTest(new SaveResultsAsJsonRequestParams(), (qes, sp, fsFunc) => new SaveAsJsonFileStreamFactory(qes, sp, fsFunc));
        }

        [Test]
        public void XmlConstructor_InvalidParameters()
        {
            ConstructorTest(new SaveResultsAsXmlRequestParams(), (qes, sp, fsFunc) => new SaveAsXmlFileStreamFactory(qes, sp, fsFunc));
        }

        private void ConstructorTest<TParams>(
            TParams parameters,
            Func<QueryExecutionSettings, TParams, Func<string, FileMode, FileAccess, FileShare, Stream>, ISaveAsFileStreamFactory> ctor)
        where TParams : class
        {
            // Arrange
            var mockFileStreamFunc = new Mock<Func<string, FileMode, FileAccess, FileShare, Stream>>();

            // Act / Assert
            Assert.Throws<ArgumentNullException>(() => ctor(null, null, mockFileStreamFunc.Object));
            Assert.Throws<ArgumentNullException>(() => ctor(null, parameters, null));
        }

        [TestCaseSource(nameof(GetReader_Cases))]
        public void GetReader(Func<Func<string, FileMode, FileAccess, FileShare, Stream>, ISaveAsFileStreamFactory> ctor)
        {
            // Arrange
            using var mockStream = new MemoryStream();
            var mockFileStreamFunc = new Mock<Func<string, FileMode, FileAccess, FileShare, Stream>>();
            mockFileStreamFunc.Setup(m => m(It.IsAny<string>(), It.IsAny<FileMode>(), It.IsAny<FileAccess>(), It.IsAny<FileShare>()))
                .Returns(mockStream);
            var factory = ctor(mockFileStreamFunc.Object);

            // Act
            using var reader = factory.GetReader("foobar");

            // Assert - Always returns service buffer reader
            Assert.IsNotNull(reader);
            Assert.IsInstanceOf<ServiceBufferFileStreamReader>(reader);
            Assert.AreSame(mockStream, ((ServiceBufferFileStreamReader)reader).fileStream);

            mockFileStreamFunc.Verify(m => m("foobar", FileMode.Open, FileAccess.Read, FileShare.ReadWrite), Times.Once());
            mockFileStreamFunc.VerifyNoOtherCalls();
        }

        public static IEnumerable<object[]> GetReader_Cases
        {
            get
            {
                yield return new object[] { CsvFactoryTestCtor };
                yield return new object[] { ExcelFactoryTestCtor };
                yield return new object[] { JsonFactoryTestCtor };
                yield return new object[] { XmlFactoryTestCtor };
            }
        }

        [TestCaseSource(nameof(GetWriter_Cases))]
        public void GetWriter(
            Func<Func<string, FileMode, FileAccess, FileShare, Stream>, ISaveAsFileStreamFactory> ctor,
            Type expectedWriterType)
        {
            // Arrange
            using var mockStream = new MemoryStream();
            var mockFileStreamFunc = new Mock<Func<string, FileMode, FileAccess, FileShare, Stream>>();
            mockFileStreamFunc.Setup(m => m(It.IsAny<string>(), It.IsAny<FileMode>(), It.IsAny<FileAccess>(), It.IsAny<FileShare>()))
                .Returns(mockStream);
            var factory = ctor(mockFileStreamFunc.Object);
            var columns = Array.Empty<DbColumnWrapper>();

            // Act
            using var writer = factory.GetWriter("foobar", columns);

            // Assert - Returns the expected writer type
            Assert.IsNotNull(writer);
            Assert.IsInstanceOf(expectedWriterType, writer);

            mockFileStreamFunc.Verify(m => m("foobar", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite), Times.Once());
            mockFileStreamFunc.VerifyNoOtherCalls();
        }

        public static IEnumerable<object[]> GetWriter_Cases
        {
            get
            {
                yield return new object[] { CsvFactoryTestCtor, typeof(SaveAsCsvFileStreamWriter) };
                yield return new object[] { ExcelFactoryTestCtor, typeof(SaveAsExcelFileStreamWriter) };
                yield return new object[] { JsonFactoryTestCtor, typeof(SaveAsJsonFileStreamWriter) };
                yield return new object[] { XmlFactoryTestCtor, typeof(SaveAsXmlFileStreamWriter) };
            }
        }

        // GetWriter - Returns writer of specified type

        private static ISaveAsFileStreamFactory CsvFactoryTestCtor(Func<string, FileMode, FileAccess, FileShare, Stream> fsFunc) =>
            new SaveAsCsvFileStreamFactory(new QueryExecutionSettings(), new SaveResultsAsCsvRequestParams(), fsFunc);
        private static ISaveAsFileStreamFactory ExcelFactoryTestCtor(Func<string, FileMode, FileAccess, FileShare, Stream> fsFunc) =>
            new SaveAsExcelFileStreamFactory(new QueryExecutionSettings(), new SaveResultsAsExcelRequestParams(), fsFunc);
        private static ISaveAsFileStreamFactory JsonFactoryTestCtor(Func<string, FileMode, FileAccess, FileShare, Stream> fsFunc) =>
            new SaveAsJsonFileStreamFactory(new QueryExecutionSettings(), new SaveResultsAsJsonRequestParams(), fsFunc);
        private static ISaveAsFileStreamFactory XmlFactoryTestCtor(Func<string, FileMode, FileAccess, FileShare, Stream> fsFunc) =>
            new SaveAsXmlFileStreamFactory(new QueryExecutionSettings(), new SaveResultsAsXmlRequestParams(), fsFunc);
    }
}