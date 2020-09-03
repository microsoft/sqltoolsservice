using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Kusto.ServiceLayer.DataSource;
using Microsoft.Kusto.ServiceLayer.DataSource.Metadata;
using NUnit.Framework;

namespace Microsoft.Kusto.ServiceLayer.UnitTests.DataSource
{
    public class KustoQueryUtilsTests
    {
        [TestCase("[@Table With Spaces", "[@Table With Spaces")]
        [TestCase("*", "*")]
        [TestCase("TableName", "TableName")]
        [TestCase("and", "[@\"and\"]")]
        [TestCase("anomalychart", "[@\"anomalychart\"]")]
        [TestCase("areachart", "[@\"areachart\"]")]
        [TestCase("asc", "[@\"asc\"]")]
        [TestCase("barchart", "[@\"barchart\"]")]
        [TestCase("between", "[@\"between\"]")]
        [TestCase("bool", "[@\"bool\"]")]
        [TestCase("boolean", "[@\"boolean\"]")]
        [TestCase("by", "[@\"by\"]")]
        [TestCase("columnchart", "[@\"columnchart\"]")]
        [TestCase("consume", "[@\"consume\"]")]
        [TestCase("contains", "[@\"contains\"]")]
        [TestCase("containscs", "[@\"containscs\"]")]
        [TestCase("count", "[@\"count\"]")]
        [TestCase("date", "[@\"date\"]")]
        [TestCase("datetime", "[@\"datetime\"]")]
        [TestCase("default", "[@\"default\"]")]
        [TestCase("desc", "[@\"desc\"]")]
        [TestCase("distinct", "[@\"distinct\"]")]
        [TestCase("double", "[@\"double\"]")]
        [TestCase("dynamic", "[@\"dynamic\"]")]
        [TestCase("endswith", "[@\"endswith\"]")]
        [TestCase("evaluate", "[@\"evaluate\"]")]
        [TestCase("extend", "[@\"extend\"]")]
        [TestCase("false", "[@\"false\"]")]
        [TestCase("filter", "[@\"filter\"]")]
        [TestCase("find", "[@\"find\"]")]
        [TestCase("first", "[@\"first\"]")]
        [TestCase("flags", "[@\"flags\"]")]
        [TestCase("float", "[@\"float\"]")]
        [TestCase("getschema", "[@\"getschema\"]")]
        [TestCase("has", "[@\"has\"]")]
        [TestCase("hasprefix", "[@\"hasprefix\"]")]
        [TestCase("hassuffix", "[@\"hassuffix\"]")]
        [TestCase("in", "[@\"in\"]")]
        [TestCase("int", "[@\"int\"]")]
        [TestCase("join", "[@\"join\"]")]
        [TestCase("journal", "[@\"journal\"]")]
        [TestCase("kind", "[@\"kind\"]")]
        [TestCase("ladderchart", "[@\"ladderchart\"]")]
        [TestCase("last", "[@\"last\"]")]
        [TestCase("like", "[@\"like\"]")]
        [TestCase("limit", "[@\"limit\"]")]
        [TestCase("linechart", "[@\"linechart\"]")]
        [TestCase("long", "[@\"long\"]")]
        [TestCase("materialize", "[@\"materialize\"]")]
        [TestCase("mvexpand", "[@\"mvexpand\"]")]
        [TestCase("notcontains", "[@\"notcontains\"]")]
        [TestCase("notlike", "[@\"notlike\"]")]
        [TestCase("of", "[@\"of\"]")]
        [TestCase("or", "[@\"or\"]")]
        [TestCase("order", "[@\"order\"]")]
        [TestCase("parse", "[@\"parse\"]")]
        [TestCase("piechart", "[@\"piechart\"]")]
        [TestCase("pivotchart", "[@\"pivotchart\"]")]
        [TestCase("print", "[@\"print\"]")]
        [TestCase("project", "[@\"project\"]")]
        [TestCase("queries", "[@\"queries\"]")]
        [TestCase("real", "[@\"real\"]")]
        [TestCase("regex", "[@\"regex\"]")]
        [TestCase("sample", "[@\"sample\"]")]
        [TestCase("scatterchart", "[@\"scatterchart\"]")]
        [TestCase("search", "[@\"search\"]")]
        [TestCase("set", "[@\"set\"]")]
        [TestCase("sort", "[@\"sort\"]")]
        [TestCase("stacked", "[@\"stacked\"]")]
        [TestCase("stacked100", "[@\"stacked100\"]")]
        [TestCase("stackedareachart", "[@\"stackedareachart\"]")]
        [TestCase("startswith", "[@\"startswith\"]")]
        [TestCase("string", "[@\"string\"]")]
        [TestCase("summarize", "[@\"summarize\"]")]
        [TestCase("take", "[@\"take\"]")]
        [TestCase("time", "[@\"time\"]")]
        [TestCase("timechart", "[@\"timechart\"]")]
        [TestCase("timeline", "[@\"timeline\"]")]
        [TestCase("timepivot", "[@\"timepivot\"]")]
        [TestCase("timespan", "[@\"timespan\"]")]
        [TestCase("to", "[@\"to\"]")]
        [TestCase("top", "[@\"top\"]")]
        [TestCase("toscalar", "[@\"toscalar\"]")]
        [TestCase("true", "[@\"true\"]")]
        [TestCase("union", "[@\"union\"]")]
        [TestCase("unstacked", "[@\"unstacked\"]")]
        [TestCase("viewers", "[@\"viewers\"]")]
        [TestCase("where", "[@\"where\"]")]
        [TestCase("withsource", "[@\"withsource\"]")]
        public void EscapeName_Returns_Name(string name, string expected)
        {
            var result = KustoQueryUtils.EscapeName(name);
            Assert.AreEqual(expected, result);
        }

        [TestCase(".show databases", true)]
        [TestCase(".show schema", true)]
        [TestCase(".show tables", false)]
        public void IsClusterLevelQuery_Returns_Result(string query, bool expected)
        {
            var result = KustoQueryUtils.IsClusterLevelQuery(query);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void SafeAdd_AddsRecord_ExistingKey()
        {
            string key = "FolderName";

            var dictionary = new Dictionary<string, Dictionary<string, DataSourceObjectMetadata>>
            {
                [key] = new Dictionary<string, DataSourceObjectMetadata>()
            };
            var existingRecord = new DataSourceObjectMetadata
            {
                Name = "Folder 1"
            };
            dictionary[key].Add(key, existingRecord);

            var newRecord = new DataSourceObjectMetadata
            {
                Name = "Folder 2"
            };
            dictionary.SafeAdd(key, newRecord);
            
            Assert.AreEqual(2, dictionary[key].Count);
        }

        [Test]
        public void SafeAdd_AddsRecord_NewKey()
        {
            string key = "FolderName";

            var dictionary = new Dictionary<string, Dictionary<string, DataSourceObjectMetadata>>
            {
                [key] = new Dictionary<string, DataSourceObjectMetadata>()
            };
            
            var newRecord = new DataSourceObjectMetadata
            {
                Name = "Folder 2"
            };
            dictionary.SafeAdd(key, newRecord);
            
            Assert.AreEqual(1, dictionary[key].Count);
        }

        [Test]
        public void AddRange_Keeps_Existing_Records_And_Order()
        {
            var key = "DatabaseName";
            
            var existingObjectMetadata = new DataSourceObjectMetadata
            {
                PrettyName = "Ball Table"
            };

            var dictionary = new ConcurrentDictionary<string, IEnumerable<DataSourceObjectMetadata>>
            {
                [key] = new List<DataSourceObjectMetadata> {existingObjectMetadata}
            };

            var newMetadata = new DataSourceObjectMetadata
            {
                PrettyName = "Apple Table"
            };

            dictionary.AddRange(key, new List<DataSourceObjectMetadata> {newMetadata});
            
            Assert.AreEqual(2, dictionary[key].Count());
            
            // ensure order by clause
            Assert.AreEqual(newMetadata.PrettyName, dictionary[key].First().PrettyName);
            Assert.AreEqual(existingObjectMetadata.PrettyName, dictionary[key].Last().PrettyName);
        }
    }
}