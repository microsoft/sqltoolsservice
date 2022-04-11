﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.SqlTools.ServiceLayer.AutoParameterizaition;
using Microsoft.SqlTools.ServiceLayer.AutoParameterizaition.Exceptions;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using NUnit.Framework;

using static System.Linq.Enumerable;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.AutoParameterization
{
    [TestFixture]
    /// <summary>
    /// Parameterization for Always Encrypted is a feature that automatically converts Transact-SQL variables
    /// into query parameters (instances of <c>SqlParameter</c> Class). This allows the underlying .NET Framework
    /// Data Provider for SQL Server to detect data targeting encrypted columns, and to encrypt such data before
    /// sending it to the database.
    /// Without parameterization, the .NET Framework Data Provider passes each statement in the Query Editor,
    /// as a non-parameterized query. If the query contains literals or Transact-SQL variables that target encrypted columns,
    /// the.NET Framework Data Provider for SQL Server won't be able to detect and encrypt them, before sending the query to the database.
    /// As a result, the query will fail due to type mismatch (between the plaintext literal Transact-SQL variable and the encrypted column).
    /// This class unit-tests the functionality os the Parameterization for Always Encrypted feature.
    /// </summary>
    public class SqlParameterizerTests
    {
        #region Query Parameterization Tests

        /// <summary>
        /// SqlParameterizer should parameterize Transact-SQL variables that meet the following pre-requisite conditions:
        /// - Are declared and initialized in the same statement(inline initialization).
        /// - Are initialized using a single literal.
        /// </summary>
        [Test]
        public void SqlParameterizerShouldParameterizeValidVariables()
        {
            const string ssn = "795-73-9838";
            const string birthday = "19990104";
            const string salary = "$30000";

            string sql = $@"
                DECLARE @SSN CHAR(11) = '{ssn}'
                DECLARE @BIRTHDAY DATE = '{birthday}'
                DECLARE @SALARY MONEY = '{salary}'

                SELECT * FROM [dbo].[Patients]
                WHERE [SSN] = @SSN AND [BIRTHDAY] = @BIRTHDAY AND [SALARY] = @SALARY";

            DbCommand command = new SqlCommand { CommandText = sql };
            command.Parameterize();

            Assert.AreEqual(expected: 3, actual: command.Parameters.Count);
        }

        /// <summary>
        /// SqlParameterizer should not attempt parameterize Transact-SQL variables that do not meet the following pre-requisite conditions:
        /// - Are declared and initialized in the same statement(inline initialization).
        /// - Are initialized using a single literal.
        /// The first variable has initialization separate from declaration and so should not be parameterized.
        /// The second is using a function used instead of a literal and so should not be parameterized.
        /// The third is using an expression used instead of a literal and so should not be parameterized.
        /// </summary>
        [Test]
        public void SqlParameterizerShouldNotParameterizeInvalidVariables()
        {
            string sql = $@"
                DECLARE @Name nvarchar(50);
                SET @Name = 'Abel';
                DECLARE @StartDate date = GETDATE();
                DECLARE @NewSalary money = @Salary * 1.1;

                SELECT * FROM [dbo].[Patients]
                WHERE [Name] = @Name AND [StartDate] = @StartDate AND [NewSalary] = @NewSalary";

            DbCommand command = new SqlCommand { CommandText = sql };
            command.Parameterize();

            Assert.AreEqual(expected: 0, actual: command.Parameters.Count);
        }

        /// <summary>
        /// SQLDOM parser currently cannot handle very large scripts and runs out of memory.
        /// Batch statements larger than 300000 characters (Approximately 600 Kb) should
        /// throw <c>ParameterizationScriptTooLargeException</c>.
        /// </summary>
        [Test]
        public void SqlParameterizerShouldThrowWhenSqlIsTooLong()
        {
            
            string sqlLength_300 = $@"
                DECLARE @SSN CHAR(11) = '123-45-6789'
                DECLARE @BIRTHDAY DATE = '19990104'
                DECLARE @SALARY MONEY = '$30000'

                SELECT * FROM [dbo].[Patients]
                WHERE [N] = @SSN AND [B] = @BIRTHDAY AND [S] = @SALARY
                GO";

            // SQL less than or equal to 300000 should pass  
            string smallSql = string.Concat(Repeat(element: sqlLength_300, count: 1000));
            DbCommand command1 = new SqlCommand { CommandText = smallSql };
            command1.Parameterize();

            // SQL greater than 300000 characters should throw   
            string bigSql = string.Concat(Repeat(element: sqlLength_300, count: 1100));
            DbCommand command2 = new SqlCommand { CommandText = bigSql };
            Assert.Throws<ParameterizationScriptTooLargeException>(() => command2.Parameterize());
        }

        /// <summary>
        /// During parameterization, if we could not parse the SQL we will throw an <c>ParameterizationParsingException</c>.
        /// Better to catch the error here than on the server.
        /// </summary>
        [Test]
        public void SqlParameterizerShouldThrowWhenSqlIsInvalid()
        {
            string invalidSql = "THIS IS INVALID SQL";
            
            string sql = string.Concat(Repeat(element: invalidSql, count: 1000));
            DbCommand command = new SqlCommand { CommandText = sql };

            Assert.Throws<ParameterizationParsingException>(() => command.Parameterize());
        }

        /// <summary>
        /// While the SqlParameterizer should parameterize Transact-SQL variables that are declared and initialized 
        /// in the same statement(inline initialization) and are initialized using a single literal, the type of the 
        /// literal used for the initialization of the variable must also match the type in the variable declaration.
        /// If not, a <c>ParameterizationFormatException</c> should get thrown.
        /// </summary>
        [Test]
        public void SqlParameterizerShouldThrowWhenLiteralHasTypeMismatch()
        {
            // variable is declared an int but is getting set to character data
            string sql = $@"
                DECLARE @Number int = 'ABCDEFG'

                SELECT * FROM [dbo].[Table]
                WHERE [N] = @Number
                GO";

            DbCommand command = new SqlCommand { CommandText = sql };
            Assert.Throws<ParameterizationFormatException>(() => command.Parameterize());
        }

        /// <summary>
        /// A side effect of the parameterization process is that, when a batch script was composed
        /// entirely of comments, the comments were stripped away and the <c>CommandText</c>
        /// property of the <c>DbCommand</c> would be replaced with an empty string. When this happens,
        /// the DbCommand object will throw an exception with the following message:
        ///    BeginExecuteReader: CommandText property has not been initialized
        /// </summary>
        [Test]
        public void CommentOnlyBatchesShouldNotBeErasedFromCommandText()
        {
            string sql = $@"
                -- ALTER TABLE BatchParameterization
                --     ALTER COLUMN
                --         [unique_key] [UNIQUEIDENTIFIER] NOT NULL";

            DbCommand command = new SqlCommand { CommandText = sql };
            command.Parameterize();

            Assert.False(string.IsNullOrEmpty(command.CommandText));
            Assert.AreEqual(expected: sql, actual: command.CommandText);
        }

        #endregion

        #region Prarmeterization Codesense Tests

        /// <summary>
        /// When requesting a collection of <c>ScriptFileMarker</c> by calling the <c>SqlParameterizer.CodeSense</c>
        /// method, if a null script is passed in, the reuslt should be an empty collection.
        /// </summary>
        [Test]
        public void CodeSenseShouldReturnEmptyListWhenGivenANullScript()
        {
            string sql = null;
            IList<ScriptFileMarker> result = SqlParameterizer.CodeSense(sql);

            Assert.NotNull(result);
            Assert.That(result, Is.Empty);
        }

        /// <summary>
        /// When requesting a collection of <c>ScriptFileMarker</c> by calling the <c>SqlParameterizer.CodeSense</c>
        /// method, if a script is passed in that contains no valid parameters, the reuslt should be an empty collection.
        /// </summary>
        [Test]
        public void CodeSenseShouldReturnEmptyListWhenGivenAParameterlessScript()
        {
            // SQL with no parameters
            string sql = $@"
                SELECT * FROM [dbo].[Patients]
                WHERE [N] = @SSN AND [B] = @BIRTHDAY AND [S] = @SALARY
                GO";

            IList<ScriptFileMarker> result = SqlParameterizer.CodeSense(sql);

            Assert.NotNull(result);
            Assert.That(result, Is.Empty);
        }

        /// <summary>
        /// SQLDOM parser currently cannot handle very large scripts and runs out of memory.
        /// SQL statements larger than 300000 characters (Approximately 600 Kb) should
        /// return a max string sength code sense item. These will be returned to ADS to display to the user as intelli-sense.
        /// </summary>
        [Test]
        public void CodeSenseShouldReturnMaxStringLengthScriptFileMarkerErrorItemWhenScriptIsTooLong()
        {
            // SQL length of 300 characters
            string sqlLength_300 = $@"
                DECLARE @SSN CHAR(11) = '123-45-6789'
                DECLARE @BIRTHDAY DATE = '19990104'
                DECLARE @SALARY MONEY = '$30000'

                SELECT * FROM [dbo].[Patients]
                WHERE [N] = @SSN AND [B] = @BIRTHDAY AND [S] = @SALARY
                GO";
            
            // Repeat the SQL 1001 times to exceed length threshold
            string sql = string.Concat(Repeat(element: sqlLength_300, count: 1100));

            IList<ScriptFileMarker> result = SqlParameterizer.CodeSense(sql);
            string expectedMessage = SR.ScriptTooLarge(maxChars: 300000, currentChars: sql.Length);

            Console.WriteLine(result[0].Message);

            Assert.That(result, Is.Not.Empty);
            Assert.AreEqual(expected: 1, actual: result.Count);
            Assert.AreEqual(expected: ScriptFileMarkerLevel.Error, actual: result[0].Level);
            Assert.AreEqual(expected: expectedMessage, actual: result[0].Message);
        }

        /// <summary>
        /// When requesting a collection of <c>ScriptFileMarker</c> by calling the <c>SqlParameterizer.CodeSense</c>
        /// method, if a script is passed in that contains 3 valid parameters, the reuslt should be a collection of
        /// three informational code sense items. These will be returned to ADS to display to the user as intelli-sense.
        /// </summary>
        [Test]
        public void CodeSenseShouldReturnInformationalCodeSenseItemsForValidParameters()
        {
            const string ssn = "795-73-9838";
            const string birthday = "19990104";
            const string salary = "$30000";

            string sql = $@"
                DECLARE @SSN CHAR(11) = '{ssn}'
                DECLARE @BIRTHDAY DATE = '{birthday}'
                DECLARE @SALARY MONEY = '{salary}'

                SELECT * FROM [dbo].[Patients]
                WHERE [SSN] = @SSN AND [BIRTHDAY] = @BIRTHDAY AND [SALARY] = @SALARY";

            IList<ScriptFileMarker> result = SqlParameterizer.CodeSense(sql);

            Assert.That(result, Is.Not.Empty);
            Assert.AreEqual(expected: 3, actual: result.Count);
            Assert.True(Enumerable.All(result, i => i.Level == ScriptFileMarkerLevel.Information));
        }

        #endregion
    }
}
