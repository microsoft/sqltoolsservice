using System;
using System.Collections.Concurrent;
using System.IO;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Moq;

namespace Microsoft.SqlTools.ServiceLayer.Test.Common
{
    public class MemoryFileSystem
    {

        public static IServiceBufferFileStreamFactory GetServiceBufferFileStreamFactory(int sizeFactor=1)
        {
            return GetServiceBufferFileStreamFactory(new ConcurrentDictionary<string, byte[]>(), sizeFactor);
        }

        public static IServiceBufferFileStreamFactory GetServiceBufferFileStreamFactory(ConcurrentDictionary<string, byte[]> storage, int sizeFactor=1)
        {
            Mock<IServiceBufferFileStreamFactory> mock = new Mock<IServiceBufferFileStreamFactory>();
            mock.Setup(fsf => fsf.CreateFile())
                .Returns(() =>
                {
                    string fileName = Guid.NewGuid().ToString();
                    storage.TryAdd(fileName, new byte[8192 * sizeFactor]);
                    return fileName;
                });
            mock.Setup(fsf => fsf.GetReader(It.IsAny<string>()))
                .Returns<string>(output => new ServiceBufferFileStreamReader(new MemoryStream(storage[output]), new QueryExecutionSettings()));
            mock.Setup(fsf => fsf.GetWriter(It.IsAny<string>()))
                .Returns<string>(output => new ServiceBufferFileStreamWriter(new MemoryStream(storage[output]), new QueryExecutionSettings()));

            return mock.Object;
        }

    }
}
