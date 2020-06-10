//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using Microsoft.Kusto.ServiceLayer.Formatter;
using Xunit;

namespace Microsoft.Kusto.ServiceLayer.UnitTests.Formatter
{
    
    public class CreateViewFormatterTests : FormatterUnitTestsBase
    {
        [Fact]
        public void CreateView_Full()
        {
            LoadAndFormatAndCompare("CreateView_Full", GetInputFile("CreateView_Full.sql"), 
                GetBaselineFile("CreateView_Full.sql"), new FormatOptions(), true);
        }

        [Fact]
        public void CreateView_FullWithComments()
        {
            LoadAndFormatAndCompare("CreateView_FullWithComments", GetInputFile("CreateView_FullWithComments.sql"), GetBaselineFile("CreateView_FullWithComments.sql"), new FormatOptions(), true);
        }

        [Fact]
        public void CreateView_MultipleColumns()
        {
            LoadAndFormatAndCompare("CreateView_MultipleColumns", GetInputFile("CreateView_MultipleColumns.sql"), 
                GetBaselineFile("CreateView_MultipleColumns.sql"), new FormatOptions(), true);
        }

        [Fact]
        public void CreateView_MultipleOptions()
        {
            LoadAndFormatAndCompare("CreateView_MultipleOptions", GetInputFile("CreateView_MultipleOptions.sql"), 
                GetBaselineFile("CreateView_MultipleOptions.sql"), new FormatOptions(), true);
        }

        [Fact]
        public void CreateView_OneColumn()
        {
            LoadAndFormatAndCompare("CreateView_OneColumn", GetInputFile("CreateView_OneColumn.sql"), 
                GetBaselineFile("CreateView_OneColumn.sql"), new FormatOptions(), true);
        }

        [Fact]
        public void CreateView_OneColumnOneOption()
        {
            LoadAndFormatAndCompare("CreateView_OneColumnOneOption", GetInputFile("CreateView_OneColumnOneOption.sql"), 
                GetBaselineFile("CreateView_OneColumnOneOption.sql"), new FormatOptions(), true);
        }

        [Fact]
        public void CreateView_OneOption()
        {
            LoadAndFormatAndCompare("CreateView_OneOption", GetInputFile("CreateView_OneOption.sql"), 
                GetBaselineFile("CreateView_OneOption.sql"), new FormatOptions(), true);
        }

        [Fact]
        public void CreateView_Simple()
        {
            LoadAndFormatAndCompare("CreateView_Simple", GetInputFile("CreateView_Simple.sql"), 
                GetBaselineFile("CreateView_Simple.sql"), new FormatOptions(), true);
        }
    }
}
