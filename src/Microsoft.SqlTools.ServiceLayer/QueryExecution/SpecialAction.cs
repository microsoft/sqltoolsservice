// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution
{
    public class SpecialAction {
        bool _None;
        bool _ExpectActualYukonXmlShowPlan;
        bool _ExpectEstimatedYukonXmlShowPlan;
        bool _ExpectActualYukonTextShowPlan;
        bool _ExpectEstimatedYukonTextShowPlan;

        public bool None {
            get { return _None; }
            set { 
                _None = value;
                if (value)
                {
                    _ExpectActualYukonXmlShowPlan = false;
                    _ExpectEstimatedYukonXmlShowPlan = false;
                    _ExpectActualYukonTextShowPlan = false;
                    _ExpectEstimatedYukonTextShowPlan = false;
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

        public bool ExpectActualYukonTextShowPlan 
        {
            get { return _ExpectActualYukonTextShowPlan; }
            set { this.registerSpecialAction(ref _ExpectActualYukonTextShowPlan, value); }
        }

        public bool ExpectEstimatedYukonTextShowPlan 
        {
            get { return _ExpectEstimatedYukonTextShowPlan; }
            set { this.registerSpecialAction(ref _ExpectEstimatedYukonTextShowPlan, value); }
        }
 
        public SpecialAction()
        {
            None = true;
            ExpectActualYukonXmlShowPlan = false;
            ExpectEstimatedYukonXmlShowPlan = false;
            ExpectActualYukonTextShowPlan = false;
            ExpectEstimatedYukonTextShowPlan = false;
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

                if (action.ExpectActualYukonTextShowPlan) 
                {
                    this.ExpectActualYukonTextShowPlan = true;
                } 

                if (action.ExpectEstimatedYukonTextShowPlan) 
                {
                    this.ExpectEstimatedYukonTextShowPlan = true;
                } 
            }
        }
        
        private bool areAllFalse()
        {
            if (!ExpectActualYukonXmlShowPlan && !ExpectEstimatedYukonXmlShowPlan &&
                !ExpectActualYukonTextShowPlan && !ExpectEstimatedYukonTextShowPlan)
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