//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.ComponentModel;
using System.Collections;

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.ShowPlan
{
    /// <summary>
    /// Base class for all object / Node parsers
    /// Used for parsing properties and hierarchy.
    /// </summary>
    public abstract class ObjectParser
    {
        /// <summary>
        /// Parses item properties.
        /// </summary>
        /// <param name="parsedItem">Item which properties are being parsed.</param>
        /// <param name="targetPropertyBag">Target property bag to populate with property wrappers.</param>
        /// <param name="context">Node builder context.</param>
        public virtual void ParseProperties(object parsedItem, PropertyDescriptorCollection targetPropertyBag, NodeBuilderContext context)
        {
            PropertyDescriptorCollection allProperties = TypeDescriptor.GetProperties(parsedItem);



            foreach (PropertyDescriptor property in allProperties)
            {
                if (property.Attributes.Contains(XmlIgnoreAttribute))
                {
                    continue;
                }

                // a special "...Specified" property (such as StatementIdSpecified)
                PropertyDescriptor specifiedProperty = allProperties[property.Name + "Specified"];
                if (specifiedProperty != null && specifiedProperty.GetValue(parsedItem).Equals(false))
                {
                    // The "...Specified" property value is false.
                    // We should skip this property
                    continue;
                }

                object value = property.GetValue(parsedItem);
                if (value == null)
                {
                    continue;
                }

                if (Type.GetTypeCode(property.PropertyType) == TypeCode.Object && ShouldSkipProperty(property))
                {
                    continue;
                }

                // In case of xml Choice group, the property name can be general like "Item" or "Items".
                // Ideally, it should contain/refer to only one of the possible values in xml choice group,
                // but due to limitations on engine side the xml choice group can contain more than one
                // value and it is not possible to change the choice group to a sequence group in XSD
                // because engine is not able to generate values in a particular order for the case of
                // warnings type. Hence, we need to iterate through all the values in items and create
                // separate property for each value and add it to target property bag.
                if (property.Name == "Items" || property.Name == "Item")
                {
                    ICollection collection = value as ICollection;
                    if (collection != null)
                    {
                        foreach (object obj in collection)
                        {
                            ObjectParser.AddProperty(targetPropertyBag, property, obj);
                        }
                    }
                    else
                    {
                        //We can get single object in choice like SeekPredicateNew in Spool as Item
                        ObjectParser.AddProperty(targetPropertyBag, property, value);
                    }
                }
                else
                {
                    ObjectParser.AddProperty(targetPropertyBag, property, value);
                }
            }
        }

        private static void AddProperty(PropertyDescriptorCollection targetPropertyBag, PropertyDescriptor property, object value)
        {
            PropertyDescriptor wrapperProperty = PropertyFactory.CreateProperty(property, value);
            if (wrapperProperty != null)
            {
                targetPropertyBag.Add(wrapperProperty);
            }
        }

        /// <summary>
        /// Determines if the current property is used to reference a child item.
        /// Hierarchy properties are skipped when property wrappers are being created.
        /// </summary>
        /// <param name="property">Property subject to test.</param>
        /// <returns>True if the property is a hierarchy property;
        /// false if this is a regular property that should appear in the property grid.
        /// </returns>
        protected virtual bool ShouldSkipProperty(PropertyDescriptor property)
        {
            return false;
        }

        private static readonly Attribute XmlIgnoreAttribute = new System.Xml.Serialization.XmlIgnoreAttribute();
    }
}
