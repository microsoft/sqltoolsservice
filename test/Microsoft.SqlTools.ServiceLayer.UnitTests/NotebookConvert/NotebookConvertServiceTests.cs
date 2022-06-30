//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using NUnit.Framework;
using Microsoft.SqlTools.ServiceLayer.NotebookConvert;
using Newtonsoft.Json;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.NotebookConvert
{

    [TestFixture]
    public class NotebookConvertServiceTests
    {
        private const string sampleSqlQuery = @"
/* 
 * Initial multiline comment
 */ 

-- Comment before batch
SELECT * FROM sys.databases

-- Compare Row Counts in Tables From Two Different Databases With the Same Schema

SELECT * -- inline single line comment
/* inline multiline
 * comment
 */
FROM sys.databases

-- ending single line comment
/**
 * Ending multiline
 * comment
 */
";

        private const string sampleNotebook = @"{
  ""metadata"": {
    ""kernelspec"": {
      ""name"": ""SQL"",
      ""display_name"": ""SQL"",
      ""language"": ""sql""
    },
    ""language_info"": {
      ""name"": ""sql"",
      ""version"": """"
    }
  },
  ""nbformat_minor"": 2,
  ""nbformat"": 4,
  ""cells"": [
    {
      ""cell_type"": ""markdown"",
      ""source"": [
        ""* Initial multiline comment""
      ]
    },
    {
      ""cell_type"": ""markdown"",
      ""source"": [
        ""Comment before batch""
      ]
    },
    {
      ""cell_type"": ""code"",
      ""source"": [
        ""SELECT * FROM sys.databases\n"",
        ""\n"",
        ""-- Compare Row Counts in Tables From Two Different Databases With the Same Schema\n"",
        ""\n"",
        ""SELECT * -- inline single line comment\n"",
        ""/* inline multiline\n"",
        "" * comment\n"",
        "" */\n"",
        ""FROM sys.databases""
      ]
    },
    {
      ""cell_type"": ""markdown"",
      ""source"": [
        ""ending single line comment""
      ]
    },
    {
      ""cell_type"": ""markdown"",
      ""source"": [
        ""* Ending multiline  \n"",
        "" * comment""
      ]
    }
  ]
}";
        [Test]
        public void ConvertSqlToNotebook()
        {
            var notebook = NotebookConvertService.ConvertSqlToNotebook(sampleSqlQuery);
            var notebookString = JsonConvert.SerializeObject(notebook, Formatting.Indented);
            Assert.AreEqual(sampleNotebook, notebookString);
        }

        [Test]
        public void ConvertNullSqlToNotebook()
        {
            var emptyNotebook = @"{
  ""metadata"": {
    ""kernelspec"": {
      ""name"": ""SQL"",
      ""display_name"": ""SQL"",
      ""language"": ""sql""
    },
    ""language_info"": {
      ""name"": ""sql"",
      ""version"": """"
    }
  },
  ""nbformat_minor"": 2,
  ""nbformat"": 4,
  ""cells"": []
}";
            var notebook = NotebookConvertService.ConvertSqlToNotebook(null);
            var notebookString = JsonConvert.SerializeObject(notebook, Formatting.Indented);
            Assert.AreEqual(emptyNotebook, notebookString);
        }

        [Test]
        public void ConvertNotebookToSql()
        {
            var expectedSqlQuery = @"/*
* Initial multiline comment
*/

/*
Comment before batch
*/

SELECT * FROM sys.databases

-- Compare Row Counts in Tables From Two Different Databases With the Same Schema

SELECT * -- inline single line comment
/* inline multiline
 * comment
 */
FROM sys.databases

/*
ending single line comment
*/

/*
* Ending multiline  

 * comment
*/";
            var notebook = JsonConvert.DeserializeObject<NotebookDocument>(sampleNotebook);
            var query = NotebookConvertService.ConvertNotebookDocToSql(notebook);
            Assert.AreEqual(expectedSqlQuery, query);
        }

        [Test]
        public void ConvertNullNotebookToSql()
        {
            var query = NotebookConvertService.ConvertNotebookDocToSql(null);
            Assert.AreEqual(string.Empty, query);
        }
    }
}
