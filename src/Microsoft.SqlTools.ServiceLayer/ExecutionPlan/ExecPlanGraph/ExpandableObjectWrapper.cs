//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.ComponentModel;

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.ExecPlanGraph
{
    public class ExpandableObjectWrapper : ObjectParser, ICustomTypeDescriptor
    {
        public ExpandableObjectWrapper()
            : this(null, null, String.Empty)
        {
        }

        public ExpandableObjectWrapper(object item)
            : this(item, null)
        {
        }

        public ExpandableObjectWrapper(object item, string defaultPropertyName)
            : this(item, defaultPropertyName, GetDefaultDisplayName(item))
        {
        }

        public ExpandableObjectWrapper(object item, string defaultPropertyName, string displayName)
        {
            this.properties = new PropertyDescriptorCollection(new PropertyDescriptor[]{});

            if (item != null)
            {
                ParseProperties(item, this.properties, null);
            }

            if (defaultPropertyName != null)
            {
                defaultProperty = this.properties[defaultPropertyName];
            }

            this.displayName = displayName;
        }

        /// <summary>
        /// Gets or sets node property value.
        /// </summary>
        public object this[string propertyName]
        {
            get
            {
                PropertyValue property = this.properties[propertyName] as PropertyValue;
                return property != null ? property.Value : null;
            }

            set
            {
                PropertyValue property = this.properties[propertyName] as PropertyValue;
                if (property != null)
                {
                    // Overwrite existing property value
                    property.Value = value;
                }
                else
                {
                    // Add new property
                    this.properties.Add(new PropertyValue(propertyName, value));
                }
            }
        }

        [Browsable(false)]
        public string DisplayName
        {
            get { return this.displayName; }
            set { this.displayName = value; }
        }

        [Browsable(false)]
        public PropertyDescriptorCollection Properties
        {
            get { return this.properties; }
        }

        public override string ToString()
        {
            return this.displayName;
        }

        /// <summary>
        /// Gets the result of item.ToString if it isn't the item class name.
        /// </summary>
        /// <param name="item">Item to stringize.</param>
        /// <returns>Default item display name.</returns>
        public static string GetDefaultDisplayName(object item)
        {
            string itemString = item.ToString();
            return itemString != item.GetType().ToString() ? itemString : String.Empty;
        }

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
            return defaultProperty;
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
            return TypeDescriptor.GetEvents(GetType(), attributes);
        }

        object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd)
        {
            return this;
        }

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
        {
            return this.properties;
        }

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
        {
            return this.properties;
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

        private PropertyDescriptorCollection properties;
        private PropertyDescriptor defaultProperty;
        private string displayName;
    }
}
