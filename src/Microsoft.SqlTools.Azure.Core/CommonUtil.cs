//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using Microsoft.SqlTools.Azure.Core.Authentication;

namespace Microsoft.SqlTools.Azure.Core
{
    internal static class CommonUtil
    {
        private const int KeyValueNameLength = 1024; // 1024 should be enough for registry key value name. 

        //********************************************************************************************
        /// <summary>
        /// Throw an exception if the object is null.
        /// </summary>
        /// <param name="var">the object to check</param>
        /// <param name="varName">the variable or parameter name to display</param>
        //********************************************************************************************
        public static void CheckForNull(Object var, string varName)
        {
            if (var == null)
            {
                throw new ArgumentNullException(varName);
            }
        }

        //********************************************************************************************
        /// <summary>
        /// Throw an exception if a string is null or empty.
        /// </summary>
        /// <param name="stringVar">string to check</param>
        /// <param name="stringVarName">the variable or parameter name to display</param>
        //********************************************************************************************
        public static void CheckStringForNullOrEmpty(string stringVar, string stringVarName)
        {
            CheckStringForNullOrEmpty(stringVar, stringVarName, false);
        }

        //********************************************************************************************
        /// <summary>
        /// Throw an exception if a string is null or empty.  
        /// </summary>
        /// <param name="stringVar">string to check</param>
        /// <param name="stringVarName">the variable or parameter name to display</param>
        /// <param name="trim">If true, will trim the string after it is determined not to be null</param>
        //********************************************************************************************
        public static void CheckStringForNullOrEmpty(string stringVar, string stringVarName, bool trim)
        {
            CheckForNull(stringVar, stringVarName);
            if (trim == true)
            {
                stringVar = stringVar.Trim();
            }
            if (stringVar.Length == 0)
            {
                throw new ArgumentException("EmptyStringNotAllowed", stringVarName);
            }
        }

        internal static bool SameString(string value1, string value2)
        {
            return (value1 == null && value2 == null) || (value2 != null && value2.Equals(value1));
        }

        internal static bool SameUri(Uri value1, Uri value2)
        {
            return (value1 == null && value2 == null) || (value2 != null && value2.Equals(value1));
        }

        internal static bool SameSubscriptionIdentifier(IAzureSubscriptionIdentifier value1,
            IAzureSubscriptionIdentifier value2)
        {
            return (value1 == null && value2 == null) || (value2 != null && value2.Equals(value1));
        }

        internal static bool SameUserAccount(IAzureUserAccount value1, IAzureUserAccount value2)
        {
            return (value1 == null && value2 == null) || (value2 != null && value2.Equals(value1));
        }

        public static string GetExceptionMessage(Exception e)
        {
            string message;

#if DEBUG
            string nl2 = Environment.NewLine + Environment.NewLine;
            message = e.Message + nl2 + "DEBUG ONLY:" + nl2 + e.ToString();
#else
            message = e.Message;
#endif

            return message;
        }
        
    }
}
