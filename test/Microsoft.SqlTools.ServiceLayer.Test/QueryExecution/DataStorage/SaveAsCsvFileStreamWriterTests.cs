using System.Text.RegularExpressions;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution.DataStorage
{
    public class SaveAsCsvFileStreamWriterTests
    {
        [Theory]
        [InlineData("Something\rElse")]
        [InlineData("Something\nElse")]
        [InlineData("Something\"Else")]
        [InlineData("Something,Else")]
        [InlineData("\tSomething")]
        [InlineData("Something\t")]
        [InlineData(" Something")]
        [InlineData("Something ")]
        [InlineData(" \t\r\n\",\r\n\"\r ")]
        public void EncodeCsvFieldShouldWrap(string field)
        {
            // If: I CSV encode a field that has forbidden characters in it
            string output = SaveAsCsvFileStreamWriter.EncodeCsvField(field);

            // Then: It should wrap it in quotes
            Assert.True(Regex.IsMatch(output, "^\".*")
                && Regex.IsMatch(output, ".*\"$"));
        }

        [Theory]
        [InlineData("Something")]
        [InlineData("Something valid.")]
        [InlineData("Something\tvalid")]
        public void EncodeCsvFieldShouldNotWrap(string field)
        {
            // If: I CSV encode a field that does not have forbidden characters in it
            string output = SaveAsCsvFileStreamWriter.EncodeCsvField(field);

            // Then: It should not wrap it in quotes
            Assert.False(Regex.IsMatch(output, "^\".*\"$"));
        }

        [Fact]
        public void EncodeCsvFieldReplace()
        {
            // If: I CSV encode a field that has a double quote in it,
            string output = SaveAsCsvFileStreamWriter.EncodeCsvField("Some\"thing");

            // Then: It should be replaced with double double quotes
            Assert.Equal("\"Some\"\"thing\"", output);
        }
        
    }
}
