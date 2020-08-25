using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Kusto.ServiceLayer.DataSource.Metadata;
using Microsoft.Kusto.ServiceLayer.Metadata.Contracts;
using NUnit.Framework;

namespace Microsoft.Kusto.ServiceLayer.UnitTests.DataSource.Metadata
{
    public class MetadataFactoryTests
    {
        [Test]
        public void CreateClusterMetadata_ThrowsNullException_For_NullClusterName()
        {
            Assert.Throws<ArgumentNullException>(() => MetadataFactory.CreateClusterMetadata(null));
        }
        
        [Test]
        [TestCase("")]
        [TestCase(null)]
        [TestCase(" ")]
        public void CreateDatabaseMetadata_ThrowsNullException_For_InvalidDatabaseName(string databaseName)
        {
            var testMetadata = new DataSourceObjectMetadata
            {
                MetadataType = DataSourceMetadataType.Cluster
            };
            
            Assert.Throws<ArgumentNullException>(() => MetadataFactory.CreateDatabaseMetadata(testMetadata, databaseName));
        }
        
        [Test]
        public void CreateDatabaseMetadata_ThrowsNullException_For_InvalidMetadataType()
        {
            var testMetadata = new DataSourceObjectMetadata
            {
                MetadataType = DataSourceMetadataType.Database
            };
            
            Assert.Throws<ArgumentException>(() => MetadataFactory.CreateDatabaseMetadata(testMetadata, "FakeDatabaseName"));
        }
        
        [Test]
        public void CreateFolderMetadata_ThrowsNullException_For_NullMetadata()
        {
            Assert.Throws<InvalidOperationException>(() => MetadataFactory.CreateFolderMetadata(null, "", ""));
        }

        [Test]
        public void CreateClusterMetadata_Returns_DataSourceObjectMetadata()
        {
            string clusterName = "FakeClusterName";
            
            var objectMetadata = MetadataFactory.CreateClusterMetadata(clusterName);
            
            Assert.AreEqual(DataSourceMetadataType.Cluster, objectMetadata.MetadataType);
            Assert.AreEqual(DataSourceMetadataType.Cluster.ToString(), objectMetadata.MetadataTypeName);
            Assert.AreEqual(clusterName, objectMetadata.Name);
            Assert.AreEqual(clusterName, objectMetadata.PrettyName);
            Assert.AreEqual(clusterName, objectMetadata.Urn);
        }

        [Test]
        public void CreateDatabaseMetadata_Returns_DataSourceObjectMetadata()
        {
            string databaseName = "FakeDatabaseName";
            var clusterMetadata = new DataSourceObjectMetadata
            {
                MetadataType = DataSourceMetadataType.Cluster,
                Name = "FakeClusterName",
                Urn = "FakeClusterName"
            };
            
            
            var objectMetadata = MetadataFactory.CreateDatabaseMetadata(clusterMetadata, databaseName);
            
            Assert.AreEqual(DataSourceMetadataType.Database, objectMetadata.MetadataType);
            Assert.AreEqual(DataSourceMetadataType.Database.ToString(), objectMetadata.MetadataTypeName);
            Assert.AreEqual(databaseName, objectMetadata.Name);
            Assert.AreEqual(databaseName, objectMetadata.PrettyName);
            Assert.AreEqual($"{clusterMetadata.Urn}.{databaseName}", objectMetadata.Urn);
        }

        [Test]
        public void CreateFolderMetadata_Returns_FolderMetadata()
        {
            string path = "FakeCluster.FakeDatabase.FakeFolder";
            string name = "FakeFolderName";
            var parentMetadata = new DataSourceObjectMetadata();
            
            var objectMetadata = MetadataFactory.CreateFolderMetadata(parentMetadata, path, name);
            
            Assert.AreEqual(DataSourceMetadataType.Folder, objectMetadata.MetadataType);
            Assert.AreEqual(DataSourceMetadataType.Folder.ToString(), objectMetadata.MetadataTypeName);
            Assert.AreEqual(name, objectMetadata.Name);
            Assert.AreEqual(name, objectMetadata.PrettyName);
            Assert.AreEqual($"{path}.{name}", objectMetadata.Urn);
            Assert.AreEqual(parentMetadata, objectMetadata.ParentMetadata);
        }

        [Test]
        public void ConvertToDatabaseInfo_Returns_EmptyList_For_NonDatabaseMetadata()
        {
            var inputList = new List<DataSourceObjectMetadata>
            {
                new DataSourceObjectMetadata
                {
                    Name = "FakeClusterName"
                }
            };
            
            var databaseInfos = MetadataFactory.ConvertToDatabaseInfo(inputList);
            
            Assert.AreEqual(0, databaseInfos.Count);
        }

        [Test]
        public void ConvertToDatabaseInfo_Returns_DatabaseInfoList()
        {
            var databaseMetadata = new DatabaseMetadata
            {
                Name = "FakeDatabaseName",
                SizeInMB = "50000"
            };
            
            var inputList = new List<DatabaseMetadata>
            {
                databaseMetadata
            };
            
            var databaseInfos = MetadataFactory.ConvertToDatabaseInfo(inputList);
            
            Assert.AreEqual(1, databaseInfos.Count);
            
            var databaseInfo = databaseInfos.Single();
            Assert.AreEqual(databaseMetadata.Name, databaseInfo.Options["name"]);
            // TODO Review SizeInMB. Current logic in function needs to be reevaluated
            //Assert.AreEqual(databaseMetadata.SizeInMB, databaseInfo.Options["sizeInMB"]);
        }

        [Test]
        public void ConvertToObjectMetadata_Returns_ListObjectMetadata()
        {
            var databaseMetadata = new DataSourceObjectMetadata
            {
                PrettyName = "FakeDatabaseName",
                MetadataTypeName = "Table"
            };
            
            var inputList = new List<DataSourceObjectMetadata>
            {
                databaseMetadata
            };
            
            var objectMetadatas = MetadataFactory.ConvertToObjectMetadata(inputList);
            
            Assert.AreEqual(1, objectMetadatas.Count);
            var objectMetadata = objectMetadatas.Single();
            
            Assert.AreEqual(databaseMetadata.PrettyName, objectMetadata.Name);
            Assert.AreEqual(databaseMetadata.MetadataTypeName, objectMetadata.MetadataTypeName);
            Assert.AreEqual(MetadataType.Table, objectMetadata.MetadataType);
        }
    }
}