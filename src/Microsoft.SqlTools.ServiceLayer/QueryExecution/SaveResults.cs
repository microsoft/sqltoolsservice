//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.Text;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution
{
    internal class SaveResults{

        /// Method ported from SSMS

        /// <summary>
        /// Encodes a single field for inserting into a CSV record. The following rules are applied:
        /// <list type="bullet">
        /// <item><description>All double quotes (") are replaced with a pair of consecutive double quotes</description></item>
        /// </list>
        /// The entire field is also surrounded by a pair of double quotes if any of the following conditions are met:
        /// <list type="bullet">
        /// <item><description>The field begins or ends with a space</description></item>
        /// <item><description>The field begins or ends with a tab</description></item>
        /// <item><description>The field contains the ListSeparator string</description></item>
        /// <item><description>The field contains the '\n' character</description></item>
        /// <item><description>The field contains the '\r' character</description></item>
        /// <item><description>The field contains the '"' character</description></item>
        /// </list>
        /// </summary>
        /// <param name="field">The field to encode</param>
        /// <returns>The CSV encoded version of the original field</returns>
        internal static String EncodeCsvField(String field)
        {
            StringBuilder sbField = new StringBuilder(field);
            
            //Whether this field has special characters which require it to be embedded in quotes
            bool embedInQuotes = false;

            //Check for leading/trailing spaces
            if (sbField.Length > 0 &&
                (sbField[0] == ' ' ||
                sbField[0] == '\t' ||
                sbField[sbField.Length - 1] == ' ' ||
                sbField[sbField.Length - 1] == '\t'))
            {
                embedInQuotes = true;
            }
            else
            {   //List separator being in the field will require quotes
                if (field.Contains(","))
                {
                    embedInQuotes = true;
                }
                else
                {
                    for (int i = 0; i < sbField.Length; ++i)
                    {
                        //Check whether this character is a special character
                        if (sbField[i] == '\r' ||
                            sbField[i] == '\n' ||
                            sbField[i] == '"')
                        { //If even one character requires embedding the whole field will
                            //be embedded in quotes so we can just break out now
                            embedInQuotes = true;
                            break;
                        }
                    }
                }
            }
            
            //Replace all quotes in the original field with double quotes
            sbField.Replace("\"", "\"\"");

            String ret = sbField.ToString();
          
            if (embedInQuotes)
            {
                ret = "\"" + ret + "\"";
            }

            return ret;
        }

        internal static bool isSaveSelection(SaveResultsRequestParams saveParams)
        {
            return ( (saveParams.ColumnStartIndex != null ) && (saveParams.ColumnEndIndex != null) 
                && (saveParams.RowEndIndex != null) && (saveParams.RowEndIndex != null)  );
        }
    }

}