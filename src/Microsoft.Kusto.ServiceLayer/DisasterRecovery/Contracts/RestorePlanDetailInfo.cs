//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts
{
    /// <summary>
    /// Class to include the plan detail 
    /// </summary>
    public class RestorePlanDetailInfo
    {
        /// <summary>
        /// The name of the option from RestoreOptionsHelper
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The current value of the option
        /// </summary>
        public object CurrentValue { get; set; }

        /// <summary>
        /// Indicates whether the option is read only or can be changed in client
        /// </summary>
        public bool IsReadOnly { get; set; }

        /// <summary>
        ///  Indicates whether the option should be visibile in client
        /// </summary>
        public bool IsVisiable { get; set; }

        /// <summary>
        /// The default value of the option
        /// </summary>
        public object DefaultValue { get; set; }

        /// <summary>
        /// Error message if the current value is not valid
        /// </summary>
        public object ErrorMessage { get; set; }

        internal static RestorePlanDetailInfo Create(string name, object currentValue, bool isReadOnly = false, bool isVisible = true, object defaultValue = null)
        {
            return new RestorePlanDetailInfo
            {
                CurrentValue = currentValue,
                IsReadOnly = isReadOnly,
                Name = name,
                IsVisiable = isVisible,
                DefaultValue = defaultValue
            };
        }
    }


}
