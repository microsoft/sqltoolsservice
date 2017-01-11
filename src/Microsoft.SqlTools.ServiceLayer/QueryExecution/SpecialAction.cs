// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution
{
    /// <summary>
    /// Class that represents a Special Action which occured by user request during the query 
    /// </summary>
    public class SpecialAction {
        
        #region Private Class variables 
        private bool none;
        private bool expectActualYukonXmlShowPlan;
        private bool expectEstimatedYukonXmlShowPlan;

        #endregion

        /// <summary>
        /// The type of XML execution plan that is contained with in a result set  
        /// </summary>
        public SpecialAction()
        {
            None = true;
        }

        #region Public Functions
        /// <summary>
        /// No Special action performed 
        /// </summary>
        public bool None {
            get { return none; }
            set { 
                none = value;
                if (value)
                {
                    expectActualYukonXmlShowPlan = false;
                    expectEstimatedYukonXmlShowPlan = false;
                }
            }
        }

        /// <summary>
        /// Contains an actual XML execution plan result set
        /// </summary>
        public bool ExpectActualYukonXmlShowPlan 
        {
            get { return expectActualYukonXmlShowPlan; }
            set { this.RegisterSpecialAction(ref expectActualYukonXmlShowPlan, value); }
        }

        /// <summary>
        /// Contains estimated XML execution plan result set 
        /// </summary>
        public bool ExpectEstimatedYukonXmlShowPlan 
        {
            get { return expectEstimatedYukonXmlShowPlan; }
            set { this.RegisterSpecialAction(ref expectEstimatedYukonXmlShowPlan, value); }
        }

        /// <summary>
        /// Contains an XML execution plan result set  
        /// </summary>
        public bool ExpectYukonXMLShowPlan 
        {
            get { return ExpectEstimatedYukonXmlShowPlan || ExpectActualYukonXmlShowPlan; }
            set 
            { 
                ExpectEstimatedYukonXmlShowPlan = value;
                ExpectActualYukonXmlShowPlan = value;
            }
        }

        /// <summary>
        /// Aggregate this special action with another one  
        /// </summary>
        public void CombineSpecialAction(SpecialAction action)
        {
            if (!action.None)
            {   
                this.None = false;
                
                if (action.ExpectActualYukonXmlShowPlan) 
                {
                    this.ExpectActualYukonXmlShowPlan = true;
                }

                if (action.ExpectEstimatedYukonXmlShowPlan) 
                {
                    this.ExpectEstimatedYukonXmlShowPlan = true;
                }
            }
        }
        
        #endregion

        #region Private Helper Functions 
        /// <summary>
        /// Check to see if all properties are false, other than none 
        /// </summary>
        private bool AreAllFalse()
        {
            return (!ExpectActualYukonXmlShowPlan && !ExpectEstimatedYukonXmlShowPlan);
        }

        /// <summary>
        /// Helper function to turn a special action on and implement needed side effects  
        /// </summary>
        private void RegisterSpecialAction(ref bool state, bool change)
        {
            state = change;
            if (change) 
            {
                None = false;
            }
            else if (this.AreAllFalse())
            {
                None = true;
            }
        }

        #endregion

    };
}
