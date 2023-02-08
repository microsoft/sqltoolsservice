//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.ShowPlan
{
    /// <summary>
    /// RunTimeCounters class stores RunTimeCountersPerThread information
    /// </summary>
    [TypeConverterAttribute(typeof(ExpandableObjectConverter))]
    internal class RunTimeCounters : ICustomTypeDescriptor
    {
        #region Inner classes

        protected struct Counter
        {
            public int Thread;
            public int BrickId;
            public bool BrickIdSpecified;
            public ulong Value;

            public Counter(int thread, ulong value)
            {
                Thread = thread;
                BrickIdSpecified = false;
                BrickId = 0;
                Value = value;
            }

            public Counter(int thread, int brickId, ulong value)
            {
                Thread = thread;
                BrickIdSpecified = true;
                BrickId = brickId;                
                Value = value;
            }
        }

        #endregion
    
        #region Fields

        ulong totalCounters;
        ulong maxCounter;
        protected List<Counter> counters = new List<Counter>();

        #endregion

        #region Constructors

        public RunTimeCounters()
        {
            maxCounter = 0;
            DisplayTotalCounters = true;
        }

        #endregion

        #region Public methods and properties

        public void AddCounter(int thread, ulong counterValue)
        {
            this.counters.Add(new Counter(thread, counterValue));
            this.totalCounters += counterValue;
            if (counterValue > maxCounter)
            {
                maxCounter = counterValue;
            }
        }
        
        public void AddCounter(int thread, int brickId, ulong counterValue)
        {
            this.counters.Add(new Counter(thread, brickId, counterValue));
            this.totalCounters += counterValue;
            if (counterValue > maxCounter)
            {
                maxCounter = counterValue;
            }
        }

        /// <summary>
        /// sum of values passed to AddCounter
        /// </summary>
        public ulong TotalCounters
        {
            get { return this.totalCounters; }
        }

        /// <summary>
        /// max value passed to AddCounter
        /// </summary>
        public ulong MaxCounter
        {
            get { return this.maxCounter; }
        }

        /// <summary>
        /// if true, display TotalCounters as string representation, otherwise display MaxCounter
        /// </summary>
        public bool DisplayTotalCounters
        {
            get; set;
        }

        /// <summary>
        /// Returns the number of Counter objects added to counters list
        /// Does not represent the calculated total count.
        /// </summary>
        public int NumOfCounters
        {
            get { return this.counters.Count; }
        }

        /// <summary>
        /// string representation of RunTimeCounters
        /// </summary>
        public override string ToString()
        {
            // display max counter value as the string representation of this class for specific properties, for ex ActualElapsedms
            // for other properties, display total counter value
            if (DisplayTotalCounters)
            {
                return TotalCounters.ToString(CultureInfo.CurrentCulture);
            }
            else
            {
                return MaxCounter.ToString(CultureInfo.CurrentCulture);
            }
        }

        #endregion

        #region ICustomTypeDescriptor

        AttributeCollection ICustomTypeDescriptor.GetAttributes() 
        {
            return TypeDescriptor.GetAttributes(GetType());
        }

        EventDescriptor ICustomTypeDescriptor.GetDefaultEvent() 
        {
            return TypeDescriptor.GetDefaultEvent(GetType());
        }

        PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty()
        {
            return TypeDescriptor.GetDefaultProperty(GetType());
        }

        object ICustomTypeDescriptor.GetEditor(Type editorBaseType) 
        {
            return TypeDescriptor.GetEditor(GetType(), editorBaseType);
        }

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
        {
            return TypeDescriptor.GetEvents(GetType());
        }

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes) 
        {
            return TypeDescriptor.GetEvents(GetType(), attributes );
        }

        object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor propertyDescriptor) 
        {
            return this;
        }

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties() 
        {
            PropertyDescriptor[] propertiesDescriptors = new PropertyDescriptor[this.counters.Count];
            string description = SR.Keys.PerThreadCounterDescription;

            if (this.counters.Count == 1)
            {
                PropertyValue property;
                if (this.counters[0].BrickIdSpecified)
                {
                    property = new PropertyValue(SR.RuntimeCounterThreadOnInstance(this.counters[0].Thread, this.counters[0].BrickId), this.counters[0].Value);
                }
                else
                {
                    property = new PropertyValue(SR.RuntimeCounterThreadAll, this.counters[0].Value);
                }
                property.SetDisplayNameAndDescription(property.Name, description);
                propertiesDescriptors[0] = property;
            }
            else
            {
                for (int i=0; i<this.counters.Count; i++)
                {
                    PropertyValue property;
                    if (this.counters[i].BrickIdSpecified)
                    {
                        property = new PropertyValue(SR.RuntimeCounterThreadOnInstance(this.counters[i].Thread, this.counters[i].BrickId), this.counters[i].Value);
                    }
                    else
                    {
                        property = new PropertyValue(SR.RuntimeCounterThread(this.counters[i].Thread), this.counters[i].Value);
                    }
                    property.SetDisplayNameAndDescription(property.Name, description);
                    propertiesDescriptors[i] = property;
                }
            }

            return new PropertyDescriptorCollection(propertiesDescriptors);
        }

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes) 
        {
            return ((ICustomTypeDescriptor)this).GetProperties();
        }

        string ICustomTypeDescriptor.GetComponentName() 
        {
            return null;
        }

        TypeConverter ICustomTypeDescriptor.GetConverter() 
        {
            return TypeDescriptor.GetConverter(GetType());
        }

        string ICustomTypeDescriptor.GetClassName() 
        {
            return GetType().Name;
        }

        #endregion
    }

    /// <summary>
    /// derived class that overrides ToString for memory grant related properties
    /// </summary>
    [TypeConverterAttribute(typeof(ExpandableObjectConverter))]
    internal class MemGrantRunTimeCounters : RunTimeCounters
    {
        /// <summary>
        /// string representation of MemGrantRunTimeCounters
        /// </summary>
        public override string ToString()
        {
            ulong displayValue = this.TotalCounters;

            // if there is more than one thread/counter, memory grant from thread 0 is not used so it doesn't carry meaningful counter value and needs to ignored
            if (this.NumOfCounters > 1)
            {
                // find thread 0 counter value, note it may not be the first element in counters list
                foreach (var ct in this.counters)
                {
                    if (ct.Thread == 0)
                    {
                        displayValue -= ct.Value;
                        break;
                    }
                }
            }

            return displayValue.ToString(CultureInfo.CurrentCulture);
        }
    }
}