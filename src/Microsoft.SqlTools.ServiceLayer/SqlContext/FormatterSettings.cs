//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Formatter;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.SqlTools.ServiceLayer.SqlContext
{
    /// <summary>
    /// Contract for receiving formatter-specific settings as part of workspace settings
    /// </summary>
    public class FormatterSettings
    {
        /// <summary>
        /// Should names be escaped, for example converting dbo.T1 to [dbo].[T1]
        /// </summary>
        public bool? UseBracketForIdentifiers
        {
            get;
            set;
        }

        /// <summary>
        /// Should comma separated lists have the comma be at the start of a new line.
        /// <code>
        /// CREATE TABLE T1 (
        ///     C1 INT
        ///     , C2 INT)
        /// </code>
        /// </summary>
        public bool? PlaceCommasBeforeNextStatement
        {
            get;
            set;
        }

        /// <summary>
        /// Should select statement references be on its own line or should references to multiple objects
        /// be kept on a single line
        /// <code>
        /// SELECT * 
        /// FROM T1,
        ///      T2
        /// </code>
        /// </summary>
        public bool? PlaceSelectStatementReferencesOnNewLine
        {
            get;
            set;
        }

        /// <summary>
        /// Should each reference be on its own line or should references to multiple objects
        /// be kept on a single line
        /// <code>
        /// SELECT * 
        /// FROM T1,
        ///      T2
        /// </code>
        /// </summary>
        public bool? PlaceEachReferenceOnNewLineInQueryStatements
        {
            get;
            set;
        }

        /// <summary>
        /// Should keyword casing be ignored, converted to all uppercase, or
        /// converted to all lowercase
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public CasingOptions KeywordCasing
        {
            get;
            set;
        }


        /// <summary>
        /// Should data type casing be ignored, converted to all uppercase, or
        /// converted to all lowercase
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public CasingOptions DatatypeCasing
        {
            get;
            set;
        }


        /// <summary>
        /// Should column definitions be aligned or left non-aligned?
        /// </summary>
        public bool? AlignColumnDefinitionsInColumns
        {
            get;
            set;
        }        
    }
}
