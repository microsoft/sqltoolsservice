// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution
{
    public class SpecialAction {
        bool _None;
        bool _ExpectActualExecutionPlan;
        bool _ExpectEstimatedExecutionPlan;
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
                    _ExpectActualExecutionPlan = false;
                    _ExpectEstimatedExecutionPlan = false;
                    _ExpectActualYukonXmlShowPlan = false;
                    _ExpectEstimatedYukonXmlShowPlan = false;
                    _ExpectActualYukonTextShowPlan = false;
                    _ExpectEstimatedYukonTextShowPlan = false;
                }
            }
        }

         public bool ExpectActualExecutionPlan {
            get { return _ExpectActualExecutionPlan; }
            set { 
                _ExpectActualExecutionPlan = value;
                if (value) {
                    _None = true;
                }
            }
        }

        public bool ExpectEstimatedExecutionPlan {
            get { return _ExpectEstimatedExecutionPlan; }
            set { 
                _ExpectEstimatedExecutionPlan = value;
                if (value) {
                    _None = true;
                }
            }
        }

        public bool ExpectActualYukonXmlShowPlan {
            get { return _ExpectActualYukonXmlShowPlan; }
            set { 
                _ExpectActualYukonXmlShowPlan = value;
                if (value) {
                    _None = true;
                }
            }
        }

        public bool ExpectEstimatedYukonXmlShowPlan {
            get { return _ExpectEstimatedYukonXmlShowPlan; }
            set { 
                _ExpectEstimatedYukonXmlShowPlan = value;
                if (value) {
                    _None = true;
                }
            }
        }

        public bool ExpectActualYukonTextShowPlan {
            get { return _ExpectActualYukonTextShowPlan; }
            set { 
                _ExpectActualYukonTextShowPlan = value;
                if (value) {
                    _None = true;
                }
            }
        }

        public bool ExpectEstimatedYukonTextShowPlan {
            get { return _ExpectEstimatedYukonTextShowPlan; }
            set { 
                _ExpectEstimatedYukonTextShowPlan = value;
                if (value) {
                    _None = true;
                }
            }
        }

        public SpecialAction()
        {
            None = true;
            _ExpectActualExecutionPlan = false;
            _ExpectEstimatedExecutionPlan = false;
            _ExpectActualYukonXmlShowPlan = false;
            _ExpectEstimatedYukonXmlShowPlan = false;
            _ExpectActualYukonTextShowPlan = false;
            _ExpectEstimatedYukonTextShowPlan = false;
        }

        public void combineSpecialAction(SpecialAction action)
        {
            if (!action.None)
            {   
                this.None = false;

                if (action._ExpectActualExecutionPlan) 
                {
                    this._ExpectActualExecutionPlan = true;
                } 
                
                if (action._ExpectEstimatedExecutionPlan) 
                {
                    this._ExpectEstimatedExecutionPlan = true;
                } 
                
                if (action._ExpectActualYukonXmlShowPlan) 
                {
                    this._ExpectActualYukonXmlShowPlan = true;
                }

                if (action._ExpectEstimatedYukonXmlShowPlan) 
                {
                    this._ExpectEstimatedYukonXmlShowPlan = true;
                } 

                if (action._ExpectActualYukonTextShowPlan) 
                {
                    this._ExpectActualYukonTextShowPlan = true;
                } 

                if (action._ExpectEstimatedYukonTextShowPlan) 
                {
                    this._ExpectEstimatedYukonTextShowPlan = true;
                } 
            }
        }
    };
}