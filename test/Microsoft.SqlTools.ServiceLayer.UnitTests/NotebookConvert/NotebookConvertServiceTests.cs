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
        [Test]
        public void ConvertSqlToNotebook()
        {
            var sql = @"
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

            var expectedNotebook = @"{
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

            var notebook = NotebookConvertService.ConvertSqlToNotebook(sql);
            var notebookString = JsonConvert.SerializeObject(notebook, Formatting.Indented);
            Assert.AreEqual(expectedNotebook, notebookString);
        }

    }
}
