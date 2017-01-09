// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution
{
    public class SpecialAction {
        bool _None;
        bool _ExpectActualYukonXmlShowPlan;
        bool _ExpectEstimatedYukonXmlShowPlan;
        bool _ExpectActualTextShowPlan;
        bool _ExpectEstimatedTextShowPlan;

        public bool None {
            get { return _None; }
            set { 
                _None = value;
                if (value)
                {
                    _ExpectActualYukonXmlShowPlan = false;
                    _ExpectEstimatedYukonXmlShowPlan = false;
                    _ExpectActualTextShowPlan = false;
                    _ExpectEstimatedTextShowPlan = false;
                }
            }
        }

        public bool ExpectActualYukonXmlShowPlan 
        {
            get { return _ExpectActualYukonXmlShowPlan; }
            set { this.registerSpecialAction(ref _ExpectActualYukonXmlShowPlan, value); }
        }

        public bool ExpectEstimatedYukonXmlShowPlan 
        {
            get { return _ExpectEstimatedYukonXmlShowPlan; }
            set { this.registerSpecialAction(ref _ExpectEstimatedYukonXmlShowPlan, value); }
        }

        public bool ExpectActualTextShowPlan 
        {
            get { return _ExpectActualTextShowPlan; }
            set { this.registerSpecialAction(ref _ExpectActualTextShowPlan, value); }
        }

        public bool ExpectEstimatedTextShowPlan 
        {
            get { return _ExpectEstimatedTextShowPlan; }
            set { this.registerSpecialAction(ref _ExpectEstimatedTextShowPlan, value); }
        }

        public bool ExpectYukonXMLShowPlan 
        {
            get { return ExpectEstimatedYukonXmlShowPlan || ExpectActualYukonXmlShowPlan; }
            set 
            { 
                ExpectEstimatedYukonXmlShowPlan = value;
                ExpectActualYukonXmlShowPlan = value;
            }
        }
 
        public SpecialAction()
        {
            None = true;
            ExpectActualYukonXmlShowPlan = false;
            ExpectEstimatedYukonXmlShowPlan = false;
            ExpectActualTextShowPlan = false;
            ExpectEstimatedTextShowPlan = false;
        }

        public void combineSpecialAction(SpecialAction action)
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

                if (action.ExpectActualTextShowPlan) 
                {
                    this.ExpectActualTextShowPlan = true;
                } 

                if (action.ExpectEstimatedTextShowPlan) 
                {
                    this.ExpectEstimatedTextShowPlan = true;
                } 
            }
        }
        
        private bool areAllFalse()
        {
            if (!ExpectActualYukonXmlShowPlan && !ExpectEstimatedYukonXmlShowPlan &&
                !ExpectActualTextShowPlan && !ExpectEstimatedTextShowPlan)
            {
                return true;
            }
            
            return false;
        }

        private void registerSpecialAction(ref bool state, bool change)
        {
            state = change;
            if (change) 
            {
                None = false;
            }
            else if (this.areAllFalse())
            {
                None = true;
            }
        }

    };
}
