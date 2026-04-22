//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Globalization;

namespace Microsoft.SqlServer.Management.QueryStoreModel.Common
{
    /// <summary>
    /// Provides core enum to localized string conversion functionality for UI components
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class EnumStringConverter<T> : TypeConverter where T : Enum
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (value is T casted && destinationType == typeof(string))
            {
                return EnumToString(casted);
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => false;

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) => throw new NotSupportedException();

        protected abstract string EnumToString(T enumValue);
    }
}
