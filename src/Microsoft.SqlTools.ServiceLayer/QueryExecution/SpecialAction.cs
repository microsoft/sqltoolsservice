// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution
{
    /// <summary>
    /// Class that represents a Special Action which occured by user request during the query 
    /// </summary>
    public class SpecialAction {
        
        #region Private Class variables 

        // Underlying representation as bitwise flags to simplify logic
        [Flags]
        private enum ActionFlags 
        {
            None    = 0,
            // All added options must be powers of 2
            ExpectYukonXmlShowPlan = 1
        }

        private ActionFlags flags; 
        private bool none;
        private bool expectYukonXmlShowPlan;

        #endregion

        /// <summary>
        /// The type of XML execution plan that is contained with in a result set  
        /// </summary>
        public SpecialAction()
        {
            flags = ActionFlags.None;
        }

        #region Public Functions
        /// <summary>
        /// No Special action performed 
        /// </summary>
        public bool None 
        { 
            get { return none; } 
            set 
            {
                flags = ActionFlags.None;
                update();
            }
        }

        /// <summary>
        /// Contains an XML execution plan result set  
        /// </summary>
        public bool ExpectYukonXMLShowPlan 
        {
            get { return expectYukonXmlShowPlan; }
            set 
            { 
                flags |= ActionFlags.ExpectYukonXmlShowPlan;
                update();
            }
        }

        /// <summary>
        /// Aggregate this special action with the input
        /// </summary>
        public void CombineSpecialAction(SpecialAction action)
        {
            flags |= action.flags;
        }
        
        #endregion

        #region Private Helper Functions 

        /// <summary>
        /// Helper function to update internal state base on flags
        /// </summary>
        private void update()
        {
            none = flags.HasFlag(ActionFlags.None);
            expectYukonXmlShowPlan = flags.HasFlag(ActionFlags.ExpectYukonXmlShowPlan);
        }

        #endregion

    };
}
