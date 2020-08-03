using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.NotebookConvert
{
    /// <summary>
    /// Basic schema wrapper for parsing a Notebook document
    /// </summary>
    public class NotebookDocument
    {
        [JsonProperty("metadata")]
        public NotebookMetadata NotebookMetadata;
        [JsonProperty("nbformat_minor")]
        public int NotebookFormatMinor = 2;
        [JsonProperty("nbformat")]
        public int NotebookFormat = 4;
        [JsonProperty("cells")]
        public IList<NotebookCell> Cells = new List<NotebookCell>();
    }

    public class NotebookMetadata
    {
        [JsonProperty("kernelspec")]
        public NotebookKernelSpec KernelSpec;
        [JsonProperty("language_info")]
        public NotebookLanguageInfo LanguageInfo;
    }

    public class NotebookKernelSpec
    {
        [JsonProperty("name")]
        public string Name;
        [JsonProperty("display_name")]
        public string DisplayName;
        [JsonProperty("language")]
        public string Language;
    }

    public class NotebookLanguageInfo
    {
        [JsonProperty("name")]
        public string Name;
        [JsonProperty("version")]
        public string Version;
    }

    /// <summary>
    /// Cell of a Notebook document
    /// </summary>
    public class NotebookCell
    {
        public NotebookCell(string cellType, IList<String> source)
        {
            this.CellType = cellType;
            this.Source = source;
        }

        [JsonProperty("cell_type")]
        public string CellType;

        [JsonProperty("source")]
        public IList<string> Source;
    }
}
