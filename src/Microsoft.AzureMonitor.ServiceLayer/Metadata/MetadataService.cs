using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AzureMonitor.ServiceLayer.Connection;
using Microsoft.SqlTools.Hosting.DataContracts.Metadata;
using Microsoft.SqlTools.Hosting.DataContracts.Metadata.Models;
using Microsoft.SqlTools.Hosting.DataContracts.ObjectExplorer.Models;
using Microsoft.SqlTools.Hosting.Protocol;

namespace Microsoft.AzureMonitor.ServiceLayer.Metadata
{
    public class MetadataService
    {
        private static readonly Lazy<MetadataService> LazyInstance = new Lazy<MetadataService>();
        public static MetadataService Instance => LazyInstance.Value;
        private static ConnectionService _connectionService;

        public void InitializeService(IProtocolEndpoint serviceHost, ConnectionService connectionService)
        {
            _connectionService = connectionService;

            serviceHost.SetRequestHandler(MetadataListRequest.Type, HandleMetadataListRequest);
        }

        private async Task HandleMetadataListRequest(MetadataQueryParams metadataParams, RequestContext<MetadataQueryResult> requestContext)
        {
            try
            {
                var metadata = new List<ObjectMetadata>();
                Parallel.Invoke(() => metadata = LoadMetadata(metadataParams));

                await requestContext.SendResult(new MetadataQueryResult
                {
                    Metadata = metadata.ToArray()
                });
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
        }

        private List<ObjectMetadata> LoadMetadata(MetadataQueryParams metadataParams)
        {
            var datasource = _connectionService.GetDataSource(metadataParams.OwnerUri);

            var metadata = new List<ObjectMetadata>();
            if (datasource != null)
            {
                metadata.AddRange(datasource.Expand("/").Select(x => x.Metadata));
            }

            return metadata;
        }
    }
}