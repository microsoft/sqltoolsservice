//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using Microsoft.SqlTools.ServiceLayer.Formatter;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.Formatter
{
    
    public class InsertFormatterTests : FormatterUnitTestsBase
    {
        [Fact]
        public void Insert_DefaultValues()
        {
            LoadAndFormatAndCompare("Insert_DefaultValues", GetInputFile("Insert_DefaultValues.sql"), 
                GetBaselineFile("Insert_DefaultValues.sql"), new FormatOptions(), true);
        }

        [Fact]
        public void Insert_OpenQuery()
        {
            LoadAndFormatAndCompare("Insert_OpenQuery", GetInputFile("Insert_OpenQuery.sql"), 
                GetBaselineFile("Insert_OpenQuery.sql"), new FormatOptions(), true);
        }

        [Fact]
        public void Insert_OutputInto()
        {
            LoadAndFormatAndCompare("Insert_OutputInto", GetInputFile("Insert_OutputInto.sql"),
                GetBaselineFile("Insert_OutputInto.sql"), new FormatOptions(), true);
        }

        [Fact]
        public void Insert_OutputStatement()
        {
            LoadAndFormatAndCompare("Insert_OutputStatement", GetInputFile("Insert_OutputStatement.sql"), 
                GetBaselineFile("Insert_OutputStatement.sql"), new FormatOptions(), true);
        }

        [Fact]
        public void Insert_Select()
        {
            FormatOptions options = new FormatOptions();
            options.PlaceEachReferenceOnNewLineInQueryStatements = true;
            LoadAndFormatAndCompare("Insert_Select", GetInputFile("Insert_Select.sql"), 
                GetBaselineFile("Insert_Select.sql"), options, true);
        }

        [Fact]
        public void Insert_SelectSource()
        {
            FormatOptions options = new FormatOptions();
            options.PlaceEachReferenceOnNewLineInQueryStatements = true;
            LoadAndFormatAndCompare("Insert_SelectSource", GetInputFile("Insert_SelectSource.sql"), 
                GetBaselineFile("Insert_SelectSource.sql"), options, true);
        }

        [Fact]
        public void Insert_TopSpecification()
        {
            LoadAndFormatAndCompare("Insert_TopSpecification", GetInputFile("Insert_TopSpecification.sql"), 
                GetBaselineFile("Insert_TopSpecification.sql"), new FormatOptions(), true);
        }

        [Fact]
        public void Insert_TopWithComments()
        {
            LoadAndFormatAndCompare("Insert_TopWithComments", GetInputFile("Insert_TopWithComments.sql"), 
                GetBaselineFile("Insert_TopWithComments.sql"), new FormatOptions(), true);
        }

        [Fact]
        public void Insert_Full()
        {
            LoadAndFormatAndCompare("Insert_Full", GetInputFile("Insert_Full.sql"),
                GetBaselineFile("Insert_Full.sql"), new FormatOptions(), true);
        }
    }
}
