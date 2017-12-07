//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Utility.SqlScriptFormatters
{
    /// <summary>
    /// Provides utilities for converting from SQL script syntax into POCOs.
    /// </summary>
    public static class FromSqlScript
    {
        // Regex: optionally starts with N, captures string wrapped in single quotes
        private static readonly Regex StringRegex = new Regex("^N?'(.*)'$", RegexOptions.Compiled);
        
        /// <summary>
        /// Decodes a multipart identifier as used in a SQL script into an array of the multiple
        /// parts of the identifier. Implemented as a state machine that iterates over the
        /// characters of the multipart identifier.
        /// </summary>
        /// <param name="multipartIdentifier">Multipart identifier to decode (eg, "[dbo].[test]")</param>
        /// <returns>The parts of the multipart identifier in an array (eg, "dbo", "test")</returns>
        /// <exception cref="FormatException">
        /// Thrown if an invalid state transition is made, indicating that the multipart identifer
        /// is not valid. 
        /// </exception>
        public static string[] DecodeMultipartIdentifier(string multipartIdentifier)
        {
            StringBuilder sb = new StringBuilder();
            List<string> namedParts = new List<string>();
            bool insideBrackets = false;
            bool bracketsClosed = false;
            for (int i = 0; i < multipartIdentifier.Length; i++)
            {
                char iChar = multipartIdentifier[i];
                if (insideBrackets)
                {
                    if (iChar == ']')
                    {
                        if (HasNextCharacter(multipartIdentifier, ']', i))
                        {
                            // This is an escaped ]
                            sb.Append(iChar);
                            i++;
                        }
                        else
                        {
                            // This bracket closes the bracket we were in
                            insideBrackets = false;
                            bracketsClosed = true;
                        }
                    }
                    else
                    {
                        // This is a standard character
                        sb.Append(iChar);
                    }
                }
                else
                {
                    switch (iChar)
                    {
                        case '[':
                            if (bracketsClosed)
                            {
                                throw new FormatException();
                            }

                            // We're opening a set of brackets
                            insideBrackets = true;
                            bracketsClosed = false;
                            break;
                        case '.':
                            if (sb.Length == 0)
                            {
                                throw new FormatException();
                            }

                            // We're splitting the identifier into a new part
                            namedParts.Add(sb.ToString());
                            sb = new StringBuilder();
                            bracketsClosed = false;
                            break;
                        default:
                            if (bracketsClosed)
                            {
                                throw new FormatException();
                            }

                            // This is a standard character
                            sb.Append(iChar);
                            break;
                    }
                }
            }
            if (sb.Length == 0)
            {
                throw new FormatException();
            }
            namedParts.Add(sb.ToString());
            return namedParts.ToArray();
        }
        
        /// <summary>
        /// Converts a value from a script into a plain version by unwrapping literal wrappers
        /// and unescaping characters.
        /// </summary>
        /// <param name="literal">The value to unwrap (eg, "(N'foo''bar')")</param>
        /// <returns>The unwrapped/unescaped literal (eg, "foo'bar")</returns>
        public static string UnwrapLiteral(string literal)
        {
            // Always remove parens
            literal = literal.Trim('(', ')');

            // Attempt to unwrap inverted commas around a string
            Match match = StringRegex.Match(literal);
            if (match.Success)
            {
                // Like: N'stuff' or 'stuff'
                return UnEscapeString(match.Groups[1].Value, '\'');
            }
            return literal;
        }
        
        #region Private Helpers
        
        private static bool HasNextCharacter(string haystack, char needle, int position)
        {
            return position + 1 < haystack.Length
                   && haystack[position + 1] == needle;
        }
        
        private static string UnEscapeString(string value, char escapeCharacter)
        {
            Validate.IsNotNull(nameof(value), value);

            // Replace 2x of the escape character with 1x of the escape character
            return value.Replace(new string(escapeCharacter, 2), escapeCharacter.ToString());
        }
        
        #endregion
    }
}