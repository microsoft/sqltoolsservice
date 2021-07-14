//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Formatter;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Formatter
{
    public class GeneralFormatterTests : FormatterUnitTestsBase
    {
        [SetUp]
        public void Init()
        {
            InitFormatterUnitTestsBase();
        }

        [Test]
        public void GoNewLineShouldBePreserved()
        {
            LoadAndFormatAndCompare("GoNewLineShouldBePreserved", 
                GetInputFile("Go.sql"),
                GetBaselineFile("Go_NewlineHandling.sql"), 
                new FormatOptions() { 
                    KeywordCasing = CasingOptions.Lowercase,
                    DatatypeCasing = CasingOptions.Uppercase,
                    PlaceEachReferenceOnNewLineInQueryStatements = true
                }, 
                verifyFormat: true);
        }

        [Test]
        public void KeywordCaseConversionUppercase()
        {
            LoadAndFormatAndCompare("KeywordCaseConversion", 
                GetInputFile("KeywordCaseConversion.sql"),
                GetBaselineFile("KeywordCaseConversion_Uppercase.sql"), 
                new FormatOptions() { KeywordCasing = CasingOptions.Uppercase }, 
                verifyFormat: true);
        }

        [Test]
        public void KeywordCaseConversionLowercase()
        {
            LoadAndFormatAndCompare("KeywordCaseConversion",
                GetInputFile("KeywordCaseConversion.sql"),
                GetBaselineFile("KeywordCaseConversion_Lowercase.sql"),
                new FormatOptions() { KeywordCasing = CasingOptions.Lowercase },
                verifyFormat: true);
        }

        [Test]
        public void SelectWithOrderByShouldCorrectlyIndent()
        {
            LoadAndFormatAndCompare("SelectWithOrderByShouldCorrectlyIndent",
                GetInputFile("SelectWithOrderBy.sql"),
                GetBaselineFile("SelectWithOrderBy_CorrectIndents.sql"),
                new FormatOptions(),
                verifyFormat: true);
        }

        [Test]
        public void SelectStatementShouldCorrectlyIndent()
        {
            LoadAndFormatAndCompare("SelectStatementShouldCorrectlyIndent",
                GetInputFile("CreateProcedure.sql"),
                GetBaselineFile("CreateProcedure_CorrectIndents.sql"),
                new FormatOptions(),
                verifyFormat: true);
        }
    }
}
