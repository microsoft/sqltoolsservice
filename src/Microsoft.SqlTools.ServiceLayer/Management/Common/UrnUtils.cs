//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlTools.ServiceLayer.Management
{
    internal class UrnUtils
    {
        private UrnUtils () { }

        /// <summary>
        /// Get the list of Urn attributes for this item.
        /// </summary>
        /// <param name="urn">Urn to be checked</param>
        /// <returns>String array of Urn attribute names. This can be zero length but will not be null</returns>
        public static string[] GetUrnAttributes(Urn urn)
        {
            String[]? urnAttributes = null;

            if(urn.XPathExpression != null && urn.XPathExpression.Length > 0)
            {
                int index = urn.XPathExpression.Length - 1;
                if(index >= 0)
                {
                    System.Collections.SortedList list = urn.XPathExpression[index].FixedProperties;
                    System.Collections.ICollection keys = list.Keys;

                    urnAttributes = new String[keys.Count];

                    int i = 0;
                    foreach(object o in keys)
                    {
                        string? key = o.ToString();
                        if (key != null)
                            urnAttributes[i++] = key;
                    }
                }
            }
            return urnAttributes != null ? urnAttributes : new String[0];
        }
    }
}
