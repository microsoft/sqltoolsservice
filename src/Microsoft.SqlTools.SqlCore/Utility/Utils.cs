//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Text;

namespace Microsoft.SqlTools.SqlCore.Utility
{
    public class StringUtils
    {
        /// <summary>
        /// Function doubles up specified character in a string
        /// </summary>
        /// <param name="s"></param>
        /// <param name="cEsc"></param>
        /// <returns></returns>
        public static String EscapeString(string s, char cEsc)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return s;
            }

            StringBuilder sb = new StringBuilder(s.Length * 2);
            foreach (char c in s)
            {
                sb.Append(c);
                if (cEsc == c)
                    sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Function doubles up ']' character in a string
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static String EscapeStringCBracket(string s)
        {
            return EscapeString(s, ']');
        }

        /// <summary>
        /// Function doubles up '\'' character in a string
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static String EscapeStringSQuote(string s)
        {
            return EscapeString(s, '\'');
        }

        /// <summary>
        /// Function removes doubled up specified character from a string
        /// </summary>
        /// <param name="s"></param>
        /// <param name="cEsc"></param>
        /// <returns></returns>
        public static String UnEscapeString(string s, char cEsc)
        {
            StringBuilder sb = new StringBuilder(s.Length);
            bool foundBefore = false;
            foreach (char c in s)
            {
                if (cEsc == c) // character to unescape
                {
                    if (foundBefore) // skip second occurrence
                    {
                        foundBefore = false;
                    }
                    else // set the flag to skip next time around
                    {
                        sb.Append(c);
                        foundBefore = true;
                    }
                }
                else
                {
                    sb.Append(c);
                    foundBefore = false;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Function removes doubled up ']' character from a string
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static String UnEscapeStringCBracket(string s)
        {
            return UnEscapeString(s, ']');
        }

        /// <summary>
        /// Function removes doubled up '\'' character from a string
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static String UnEscapeStringSQuote(string s)
        {
            return UnEscapeString(s, '\'');
        }
    }
}