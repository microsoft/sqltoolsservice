using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    public class ServiceBufferFileStreamFactory : IFileStreamFactory
    {
        public IFileStreamReader GetReader(string fileName)
        {
            return new ServiceBufferFileStreamReader(fileName);
        }

        public IFileStreamWriter GetWriter(string fileName)
        {
            return new ServiceBufferFileStreamWriter(fileName);
        }

    }
}
