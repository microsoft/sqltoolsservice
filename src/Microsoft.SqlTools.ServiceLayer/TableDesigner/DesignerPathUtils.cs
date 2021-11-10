//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Linq;
using Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner
{
    public static class DesignerPathUtils
    {
        ///<summary>
        /// Parse the path in the table designer change information.
        /// Below are the 3 scenarios and their expected path format.
        /// Note: 'index-{x}' in the description below represents the index of the object in the list.
		/// 1. 'Add' scenario
		///     a. 'propertyName1'. Example: add a column to the columns property: 'columns'.
		///     b. 'propertyName1/index-1/propertyName2'. Example: add a column mapping to the first foreign key: 'foreignKeys/0/mappings'.
		/// 2. 'Update' scenario
		///     a. 'propertyName1'. Example: update the name of the table: 'name'.
		///     b. 'propertyName1/index-1/propertyName2'. Example: update the name of a column: 'columns/0/name'.
		///     c. 'propertyName1/index-1/propertyName2/index-2/propertyName3'. Example: update the source column of an entry in a foreign key's column mapping table: 'foreignKeys/0/mappings/0/source'.
		/// 3. 'Remove' scenario
        ///     a. 'propertyName1/index-1'. Example: remove a column from the columns property: 'columns/0'.
		///     b. 'propertyName1/index-1/propertyName2/index-2': Example: remove a column mapping from a foreign key's column mapping table: 'foreignKeys/0/mappings/0'.
        ///<summary>
        public static ArrayList Parse(string path, DesignerEditType editType)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException(SR.TableEditPathNotProvidedException);
            }

            var parts = Split(path);

            // Length validation
            int[] validLengthList;
            if (editType == DesignerEditType.Add)
            {
                validLengthList = new int[] { 1, 3 };
            }
            else if (editType == DesignerEditType.Update)
            {
                validLengthList = new int[] { 1, 3, 5 };
            }
            else
            {
                validLengthList = new int[] { 2, 4 };
            }

            bool isValid = validLengthList.ToList<int>().Contains(parts.Length);
            ArrayList list = new ArrayList();
            if (isValid)
            {
                for (int i = 0; i < parts.Length; i++)
                {
                    // On odd number positions, the value must be a number.
                    if (i % 2 != 0)
                    {
                        int idx;
                        if (Int32.TryParse(parts[i], out idx))
                        {
                            list.Add(idx);
                        }
                        else
                        {
                            isValid = false;
                            break;
                        }
                    }
                    else
                    {
                        list.Add(parts[i]);
                    }
                }
            }

            if (!isValid)
            {
                throw new ArgumentException(SR.InvalidTableEditPathException(path, editType.ToString()));
            }
            return list;
        }

        private static string[] Split(string path)
        {
            return path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        }
    }
}