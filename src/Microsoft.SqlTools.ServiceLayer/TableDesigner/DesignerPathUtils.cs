//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Linq;
using Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner
{
    public static class DesignerPathUtils
    {
        ///<summary>
        /// validate the path in the table designer change information.
        /// Below are the 3 scenarios and their expected path.
        /// Note: 'index-{x}' in the description below are numbers represent the index of the object in the list.
		/// 1. 'Add' scenario
		///     a. ['propertyName1',index-1]. Example: add a column to the columns property: ['columns', 0].
		///     b. ['propertyName1',index-1,'propertyName2']. Example: add a column mapping to the first foreign key: ['foreignKeys',0,'mappings'].
		/// 2. 'Update' scenario
		///     a. ['propertyName1']. Example: update the name of the table: ['name'].
		///     b. ['propertyName1',index-1,'propertyName2']. Example: update the name of a column: ['columns',0,'name'].
		///     c. ['propertyName1',index-1,'propertyName2',index-2,'propertyName3']. Example: update the source column of an entry in a foreign key's column mapping table: ['foreignKeys',0,'mappings',0,'source'].
		/// 3. 'Remove' scenario
        ///     a. ['propertyName1',index-1]. Example: remove a column from the columns property: ['columns',0'].
		///     b. ['propertyName1',index-1,'propertyName2',index-2]. Example: remove a column mapping from a foreign key's column mapping table: ['foreignKeys',0,'mappings',0].
		/// 4. 'Move' scenario
        ///     a. ['propertyName1',fromIndex - 1,toIndex - 1]. Example: move the second column to the third place: ['columns', 1, 2].
        ///<summary>
        public static void Validate(object[] path, DesignerEditType editType)
        {
            if (path == null || path.Length == 0)
            {
                throw new ArgumentException(SR.TableEditPathNotProvidedException);
            }

            // Length validation
            int[] validLengthList;
            if (editType == DesignerEditType.Add)
            {
                validLengthList = new int[] { 2, 3 };
            }
            else if (editType == DesignerEditType.Update)
            {
                validLengthList = new int[] { 1, 3, 5 };
            }
            else if (editType == DesignerEditType.Remove)
            {
                validLengthList = new int[] { 2, 4 };
            } else
            {
                validLengthList = new int[] { 3 };
            }

            bool isValid = validLengthList.ToList<int>().Contains(path.Length);
            if (isValid)
            {
                for (int i = 0; i < path.Length; i++)
                {
                    // On odd number positions, the value must be a number.
                    if (i % 2 != 0)
                    {
                        int val;
                        isValid = Int32.TryParse(path[i]?.ToString(), out val);
                    }
                    else
                    {
                        isValid = path[i] is string;
                    }
                    if (!isValid)
                    {
                        break;
                    }
                }
            }

            if (!isValid)
            {
                throw new ArgumentException(SR.InvalidTableEditPathException(string.Join(',', path), editType.ToString()));
            }
        }
    }
}