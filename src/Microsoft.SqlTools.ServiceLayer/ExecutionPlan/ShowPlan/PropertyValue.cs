//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.ComponentModel;

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.ShowPlan
{
	internal sealed class PropertyValue : PropertyDescriptor
    {
        #region Constructors

        public PropertyValue(string name, object value)
            : base(name, null)
        {
            this.propertyValue = value;
        }

        public PropertyValue(PropertyDescriptor baseProperty, object value) 
			: this(baseProperty.Name, value)
		{
            this.baseProperty = baseProperty;
        }

        #endregion

        #region Public methods and properties

        public object Value
		{
			get { return this.propertyValue; }
			set { this.propertyValue = value; }
		}

        public string DisplayValue
        {
            get => this.Converter.ConvertToString(null, null, this.Value); 
        }

        public int DisplayOrder
        {
            get
            {
                InitializeDisplayAttributesIfNecessary();
                return this.displayOrder;
            }
        }

        public bool ShowInTooltip
        {
            get
            {
                return this.showInTooltip;
            }
        }

        public bool IsLongString
        {
            get
            {
                InitializeDisplayAttributesIfNecessary();
                return this.isLongString;
            }
        }

        public void SetDisplayNameAndDescription(string newDisplayName, string newDescription)
        {
            this.displayName = newDisplayName;
            this.description = newDescription;
        }

        #endregion

		#region PropertyDesciptor overrides

        public override AttributeCollection Attributes
        {
            get
            {
                return this.baseProperty != null ? baseProperty.Attributes : base.Attributes;
            }
        }

		public override bool CanResetValue(object component)
		{
			return false;
		}

		public override bool IsReadOnly
		{
			get { return true; }
		}

		public override Type ComponentType
		{
			get { return this.GetType(); }
		}

		public override Type PropertyType
		{
			get { return this.propertyValue != null ? this.propertyValue.GetType() : typeof(string); }
		}

		public override object GetValue(object component)
		{
			return this.propertyValue;
		}

		public override void ResetValue(object component)
		{
		}

		public override void SetValue(object component, object value)
		{
		}

		public override bool ShouldSerializeValue(object component)
		{
			return false;
		}

        public override string DisplayName
        {
            get
            {
                InitializeDisplayAttributesIfNecessary();

                if (this.displayName != null || this.displayNameKey != null)
                {
                    if (this.displayName == null)
                    {
                        this.displayName = SR.Keys.GetString(this.displayNameKey);
                    }

                    return this.displayName;
                }

                return base.DisplayName;
            }
        }

        public override string Description
        {
            get
            {
                InitializeDisplayAttributesIfNecessary();

                if (this.description != null || this.descriptionKey != null)
                {
                    if (this.description == null)
                    {
                        this.description = SR.Keys.GetString(this.descriptionKey);
                    }

                    return this.description;
                }

                return base.Description;
            }
        }

		#endregion

        private void InitializeDisplayAttributesIfNecessary()
        {
            if (this.initialized)
            {
                return;
            }

            this.initialized = true;

            DisplayNameDescriptionAttribute displayNameDescriptionAttribute =
                Attributes[typeof(DisplayNameDescriptionAttribute)] as DisplayNameDescriptionAttribute;
            if (displayNameDescriptionAttribute != null)
            {
                this.displayNameKey = displayNameDescriptionAttribute.DisplayName;
                this.descriptionKey = displayNameDescriptionAttribute.Description;
                if (this.descriptionKey == null)
                {
                    this.descriptionKey = this.displayNameKey;
                }
            }

            DisplayOrderAttribute displayOrderAttribute =
                Attributes[typeof(DisplayOrderAttribute)] as DisplayOrderAttribute;
            if (displayOrderAttribute != null)
            {
                this.displayOrder = displayOrderAttribute.DisplayOrder;
            }

            ShowInToolTipAttribute showInToolTipAttribute =
                Attributes[typeof(ShowInToolTipAttribute)] as ShowInToolTipAttribute;
            if (showInToolTipAttribute != null)
            {
                this.isLongString = showInToolTipAttribute.LongString;
                this.showInTooltip = showInToolTipAttribute.Value;
            } else 
            {
                this.showInTooltip = false;
            }
        }

        #region Private members

        private object propertyValue;
        private string displayName;
        private string displayNameKey;
        private string description;
        private string descriptionKey;
        private int displayOrder = Int32.MaxValue;
        private PropertyDescriptor baseProperty;
        private bool isLongString;
        private bool initialized;
        private bool showInTooltip;

        #endregion

        #region OrderComparer

        internal sealed class OrderComparer : IComparer
        {
            int IComparer.Compare(object x, object y)
            {
                return Compare(x as PropertyValue, y as PropertyValue);
            }

            private static int Compare(PropertyValue x, PropertyValue y)
            {
                if (x.IsLongString != y.IsLongString)
                {
                    return x.IsLongString ? 1 : -1;
                }

                int orderOfX = x != null ? x.DisplayOrder : Int32.MaxValue - 1;
                int orderOfY = y != null ? y.DisplayOrder : Int32.MaxValue - 1;

                return orderOfX - orderOfY;
            }

            public static readonly OrderComparer Default = new OrderComparer();
        }

        #endregion
    }
}
