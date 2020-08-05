//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Formatter;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Formatter
{

    public class CreateProcedureFormatterTests : FormatterUnitTestsBase
    {
        [SetUp]
        public void Init()
        {
            InitFormatterUnitTestsBase();
        }

        [Test]
        public void CreateProcedure_BackwardsCompatible()
        {
            LoadAndFormatAndCompare("CreateProcedure_BackwardsCompatible", GetInputFile("CreateProcedure_BackwardsCompatible.sql"), 
                GetBaselineFile("CreateProcedure_BackwardsCompatible.sql"), new FormatOptions(), true);
        }

        [Test]
        public void CreateProcedure_BeginEnd()
        {
            LoadAndFormatAndCompare("CreateProcedure_BeginEnd", GetInputFile("CreateProcedure_BeginEnd.sql"), 
                GetBaselineFile("CreateProcedure_BeginEnd.sql"), new FormatOptions(), true);
        }

        [Test]
        public void CreateProcedure_Minimal()
        {
            LoadAndFormatAndCompare("CreateProcedure_Minimal", GetInputFile("CreateProcedure_Minimal.sql"), 
                GetBaselineFile("CreateProcedure_Minimal.sql"), new FormatOptions(), true);
        }

        [Test]
        public void CreateProcedure_MultipleBatches()
        {
            LoadAndFormatAndCompare("CreateProcedure_MultipleBatches", GetInputFile("CreateProcedure_MultipleBatches.sql"), 
                GetBaselineFile("CreateProcedure_MultipleBatches.sql"), new FormatOptions(), true);
        }
        [Test]
        public void CreateProcedure_MultipleParams()
        {
            LoadAndFormatAndCompare("CreateProcedure_MultipleParams", GetInputFile("CreateProcedure_MultipleParams.sql"), 
                GetBaselineFile("CreateProcedure_MultipleParams.sql"), new FormatOptions(), true);
        }

        [Test]
        public void CreateProcedure_OneParam()
        {
            LoadAndFormatAndCompare("CreateProcedure_OneParam", GetInputFile("CreateProcedure_OneParam.sql"), 
                GetBaselineFile("CreateProcedure_OneParam.sql"), new FormatOptions(), true);
        }

        [Test]
        public void CreateProcedure_ParamsRecompileReturn()
        {
            LoadAndFormatAndCompare("CreateProcedure_ParamsRecompileReturn", GetInputFile("CreateProcedure_ParamsRecompileReturn.sql"), 
                GetBaselineFile("CreateProcedure_ParamsRecompileReturn.sql"), new FormatOptions(), true);
        }

        [Test]
        public void CreateProcedure_Select()
        {
            LoadAndFormatAndCompare("CreateProcedure_Select", GetInputFile("CreateProcedure_Select.sql"),
                GetBaselineFile("CreateProcedure_Select.sql"), new FormatOptions(), true);
        }

        [Test]
        public void CreateProcedure_CommaBeforeNextColumn()
        {
            // Verifies that commas are placed before the next column instead of after the current one,
            // when PlaceCommasBeforeNextStatement = true
            LoadAndFormatAndCompare("CreateProcedure_CommaBeforeNextColumn", 
                GetInputFile("CreateProcedure_CommaHandling1.sql"),
                GetBaselineFile("CreateProcedure_CommaBeforeNext.sql"),
                new FormatOptions()
                {
                    DatatypeCasing = CasingOptions.Lowercase,
                    KeywordCasing = CasingOptions.Uppercase,
                    PlaceCommasBeforeNextStatement = true,
                    PlaceEachReferenceOnNewLineInQueryStatements = true
                }, true);
        }

        [Test]
        public void CreateProcedure_CommaBeforeNextColumnRepeated()
        {
            // Verifies that formatting isn't changed for text that's already been formatted
            // as having the comma placed before the next column instead of after the prevous one
            LoadAndFormatAndCompare("CreateProcedure_CommaBeforeNextColumnRepeated", 
                GetInputFile("CreateProcedure_CommaHandling2.sql"),
                GetBaselineFile("CreateProcedure_CommaBeforeNext.sql"),
                new FormatOptions()
                {
                    DatatypeCasing = CasingOptions.Lowercase,
                    KeywordCasing = CasingOptions.Uppercase,
                    PlaceCommasBeforeNextStatement = true,
                    PlaceEachReferenceOnNewLineInQueryStatements = true
                },
                true);
        }
        
        [Test]
        public void CreateProcedure_CommaBeforeNextColumnNoNewline()
        {
            // Verifies that commas are placed before the next column instead of after the current one,
            // when PlaceCommasBeforeNextStatement = true
            // And that whitespace is used instead of newline if PlaceEachReferenceOnNewLineInQueryStatements = false
            LoadAndFormatAndCompare("CreateProcedure_CommaBeforeNextColumnNoNewline",
                GetInputFile("CreateProcedure_CommaHandling1.sql"),
                GetBaselineFile("CreateProcedure_CommaSpaced.sql"),
                new FormatOptions()
                {
                    DatatypeCasing = CasingOptions.Lowercase,
                    KeywordCasing = CasingOptions.Uppercase,
                    PlaceCommasBeforeNextStatement = true,
                    PlaceEachReferenceOnNewLineInQueryStatements = false
                }, true);
        }

        [Test]
        public void CreateProcedure_TwoPartName()
        {
            LoadAndFormatAndCompare("CreateProcedure_TwoPartName", GetInputFile("CreateProcedure_TwoPartName.sql"), 
                GetBaselineFile("CreateProcedure_TwoPartName.sql"), new FormatOptions(), true);
        }

        [Test]
        public void CreateProcedure_WithCTE()
        {
            LoadAndFormatAndCompare("CreateProcedure_WithCTE", GetInputFile("CreateProcedure_WithCTE.sql"), 
                GetBaselineFile("CreateProcedure_WithCTE.sql"), new FormatOptions(), true);
        }

        [Test]
        public void CreateProcedure_WithEncryptionModule()
        {
            LoadAndFormatAndCompare("CreateProcedure_WithEncryptionModule", GetInputFile("CreateProcedure_WithEncryptionModule.sql"), 
                GetBaselineFile("CreateProcedure_WithEncryptionModule.sql"), new FormatOptions(), true);
        }

        [Test]
        public void CreateProcedure_WithExecuteAsModule()
        {
            LoadAndFormatAndCompare("CreateProcedure_WithExecuteAsModule", GetInputFile("CreateProcedure_WithExecuteAsModule.sql"), 
                GetBaselineFile("CreateProcedure_WithExecuteAsModule.sql"), new FormatOptions(), true);
        }

        [Test]
        public void CreateProcedure_WithThreeModules()
        {
            LoadAndFormatAndCompare("CreateProcedure_WithThreeModules", GetInputFile("CreateProcedure_WithThreeModules.sql"), 
                GetBaselineFile("CreateProcedure_WithThreeModules.sql"), new FormatOptions(), true);
        }
    }
}
