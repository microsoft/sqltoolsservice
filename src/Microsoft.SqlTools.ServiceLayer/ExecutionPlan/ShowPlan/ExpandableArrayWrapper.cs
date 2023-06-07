//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections;
using System.Globalization;
using System.Text;

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.ShowPlan
{

    public class ExpandableArrayWrapper : ExpandableObjectWrapper
    {
        public ExpandableArrayWrapper(ICollection collection) : base()
        {
            PopulateProperties(collection);
        }

        #region Implementation details

        private void PopulateProperties(ICollection collection)
        {
            StringBuilder stringBuilder = new StringBuilder();
            int index = 0;

            foreach (object item in collection)
            {
                if (ObjectWrapperTypeConverter.Default.CanConvertFrom(item.GetType()))
                {
                    object convertedItem = ObjectWrapperTypeConverter.Default.ConvertFrom(item);
                    if (convertedItem != null)
                    {
                        this[GetPropertyName(++index)] = convertedItem;

                        if (stringBuilder.Length > 0)
                        {
                            stringBuilder.Append(CultureInfo.CurrentCulture.TextInfo.ListSeparator);
                            stringBuilder.Append(" ");
                        }

                        stringBuilder.Append(convertedItem.ToString());
                    }
                }
            }

            this.DisplayName = stringBuilder.ToString();
        }

        public static string GetPropertyName(int index)
        {
            return String.Format(CultureInfo.CurrentCulture, "[{0}]", index);
        }

        #endregion
    }
}
