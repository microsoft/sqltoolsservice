//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.InsightsGenerator
{
    public class DataArray
    {
        public enum DataType 
        {
            Number,
            String,
            DateTime
        }

        public string[] ColumnNames { get; set; }

        public DataType[] ColumnDataType { get; set; }

        public object[][] Cells { get; set; }
    }
}

