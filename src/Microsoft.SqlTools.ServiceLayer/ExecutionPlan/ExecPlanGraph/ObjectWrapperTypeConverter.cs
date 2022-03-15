//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.ExecPlanGraph
{
    /// <summary>
    /// This class converts methods for converting from ShowPlanXML native classes
    /// such as ColumnReferenceType or DefinedValuesListTypeDefinedValue to
    /// ShowPlan control types used in UI such as string or ExpandableObjectWrapper.
    /// 
    /// The actual Conversion is done within multiple static Convert methods which are
    /// invoked dynamically via reflection. There is code in the static constructor which
    /// discovers all Convert methods and stores them in a hash table using the type
    /// to convert from as a key.
    /// If you need to add a new conversion type, you typically just need to add a new
    /// Convert() method.
    /// </summary>
    internal class ObjectWrapperTypeConverter : ExpandableObjectConverter
    {
        /// <summary>
        /// Default instance
        /// </summary>
        public static ObjectWrapperTypeConverter Default = new ObjectWrapperTypeConverter();

        /// <summary>
        /// Default converter to ExpandableObjectWrapper 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static ExpandableObjectWrapper ConvertToWrapperObject(object item)
        {
            ExpandableObjectWrapper wrapper = new ExpandableObjectWrapper(item);
            wrapper.DisplayName = MakeDisplayNameFromObjectNamesAndValues(wrapper);
            return wrapper;
        }

        #region Specific converters

        /// <summary>
        /// Converts ColumnReferenceType to a wrapper object.
        /// </summary>
        /// <param name="item">Object to convert.</param>
        /// <returns>Wrapper object.</returns>
        public static ExpandableObjectWrapper Convert(ColumnReferenceType item)
        {
            string displayName = MergeString(".", item.Database, item.Schema, item.Table, item.Column);
            return new ExpandableObjectWrapper(item, "Column", displayName);
        }

        /// <summary>
        /// Converts GroupingSetReferenceType to a wrapper object.
        /// </summary>
        /// <param name="item">Object to convert.</param>
        /// <returns>Wrapper object.</returns>
        public static ExpandableObjectWrapper Convert(GroupingSetReferenceType item)
        {
            string displayName = item.Value;
            return new ExpandableObjectWrapper(item, "GroupingSet", displayName);
        }

        /// <summary>
        /// Converts ObjectType to a wrapper object.
        /// </summary>
        /// <param name="item">Object to convert.</param>
        /// <returns>Wrapper object.</returns>
        public static ExpandableObjectWrapper Convert(ObjectType item)
        {
            string displayName = MergeString(".", item.Server, item.Database, item.Schema, item.Table, item.Index);
            displayName = MergeString(" ", displayName, item.Alias);
            if (item.CloneAccessScopeSpecified)
            {
                string cloneAccessScope = ObjectWrapperTypeConverter.Convert(item.CloneAccessScope);
                displayName = MergeString(" ", displayName, cloneAccessScope);
            }
            return new ExpandableObjectWrapper(item, "Index", displayName);
        }

        /// <summary>
        /// Converts ObjectType to a wrapper object.
        /// </summary>
        /// <param name="item">Object to convert.</param>
        /// <returns>Wrapper object.</returns>
        public static ExpandableObjectWrapper Convert(SingleColumnReferenceType item)
        {
            return Convert(item.ColumnReference);
        }

        /// <summary>
        /// Converts array of DefinedValuesListTypeDefinedValue to a wrapper array.
        /// </summary>
        /// <param name="item">Object to convert.</param>
        /// <returns>Wrapper object.</returns>
        public static ExpandableObjectWrapper Convert(DefinedValuesListTypeDefinedValue[] definedValues)
        {
            // Note that DefinedValuesListTypeDefinedValue has both Item and Items properties.
            // Each Item gets converted to property name, while Items converted to property value.

            ExpandableObjectWrapper wrapper = new ExpandableObjectWrapper();
            StringBuilder stringBuilder = new StringBuilder();

            foreach (DefinedValuesListTypeDefinedValue definedValue in definedValues)
            {
                if (definedValue.Item == null)
                {
                    continue;
                }

                // Get property name which is a string representation of the first item
                string name = ObjectWrapperTypeConverter.Default.ConvertFrom(definedValue.Item).ToString();
                if (name.Length == 0)
                {
                    // Empty property name cannot be handled
                    // TODO: may need to generate a random property name
                    continue;
                }

                // If the property with such name already exists, skip it and continue to
                // the next defined value
                if (wrapper[name] != null)
                {
                    continue;
                }

                if (stringBuilder.Length > 0)
                {
                    stringBuilder.Append(CultureInfo.CurrentCulture.TextInfo.ListSeparator);
                    stringBuilder.Append(" ");
                }

                if (definedValue.Items == null || definedValue.Items.Length == 0)
                {
                    // If there is just one item, add the property now as an empty string
                    stringBuilder.Append(name);
                    wrapper[name] = String.Empty;
                    continue;
                }

                // Convert remaining items to an wrapper object
                object wrappedValue = ConvertToObjectWrapper(definedValue.Items);

                // Add string representation of the wrappedValue to the string builder
                if (definedValue.Items.Length > 1)
                {
                    // In the case of multiple items, we need parenthesis around the value string,
                    // which should be a comma separated list in this case.
                    stringBuilder.AppendFormat(CultureInfo.CurrentCulture, "[{0}] = ({1})", name, wrappedValue);
                }
                else
                {
                    stringBuilder.AppendFormat(CultureInfo.CurrentCulture, "[{0}] = {1}", name, wrappedValue);
                }

                wrapper[name] = wrappedValue;
            }

            // Finally store the display name in the wrapper and return it.
            wrapper.DisplayName = stringBuilder.ToString();
            return wrapper;
        }

        /// <summary>
        /// Converts ObjectType to a wrapper object.
        /// </summary>
        /// <param name="item">Object to convert.</param>
        /// <returns>Wrapper object.</returns>
        public static ExpandableObjectWrapper Convert(SetOptionsType item)
        {
            return ConvertToWrapperObject(item);
        }

        /// <summary>
        /// Converts RollupLevelType to a wrapper object.
        /// </summary>
        /// <param name="item">Object to convert.</param>
        /// <returns>Wrapper object.</returns>
        public static ExpandableObjectWrapper Convert(RollupLevelType item)
        {
            return new ExpandableObjectWrapper(item,SR.Level,item.Level.ToString());
        }
        
        /// <summary>
        /// Converts WarningsType to a wrapper object.
        /// </summary>
        /// <param name="item">Object to convert.</param>
        /// <returns>Wrapper object.</returns>
        public static ExpandableObjectWrapper Convert(WarningsType item)
        {
            string displayName = String.Empty;
            ExpandableObjectWrapper wrapper = new ExpandableObjectWrapper(item);

            ProcessSpillOccurred(wrapper, ref displayName);
            ProcessColumnWithNoStatistics(wrapper, ref displayName);
            ProcessNoJoinPredicate(wrapper, ref displayName);
            ProcessSpillToTempDb(wrapper, ref displayName);
            ProcessHashSpillDetails(wrapper, ref displayName);
            ProcessSortSpillDetails(wrapper, ref displayName);
            ProcessWaits(wrapper, ref displayName);
            ProcessPlanAffectingConvert(wrapper, ref displayName);
            ProcessMemoryGrantWarning(wrapper, ref displayName);
            ProcessFullUpdateForOnlineIndexBuild(wrapper, ref displayName);

            if (wrapper["FullUpdateForOnlineIndexBuild"] != null)
            {
                displayName = MergeString(CultureInfo.CurrentCulture.TextInfo.ListSeparator + " ",
                    displayName, SR.FullUpdateForOnlineIndexBuild);
            }

            wrapper.DisplayName = displayName;

            return wrapper;
        }

        private static void ProcessWaits(ExpandableObjectWrapper wrapper, ref string displayName)
        {
            if (wrapper["Wait"] != null)
            {
                List<ExpandableObjectWrapper> propList = GetPropertyList(wrapper, "Wait");

                Dictionary<string, int> waitTimePerWaitType = new Dictionary<string, int>();

                foreach (ExpandableObjectWrapper eow in propList)
                {
                    PropertyValue pVal = eow.Properties["WaitTime"] as PropertyValue;
                    string waitTime = pVal.Value.ToString();

                    pVal = eow.Properties["WaitType"] as PropertyValue;
                    string waitType = pVal.Value.ToString();

                    if (waitTimePerWaitType.ContainsKey(waitType))
                    {
                        waitTimePerWaitType[waitType] += int.Parse(waitTime);
                    }
                    else
                    {
                        waitTimePerWaitType.Add(waitType, int.Parse(waitTime));
                    }
                }

                foreach (KeyValuePair<string, int> kvp in waitTimePerWaitType)
                {
                    string displayStr = string.Format(SR.Wait, kvp.Value, kvp.Key);

                    displayName = MergeString(CultureInfo.CurrentCulture.TextInfo.ListSeparator + " ",
                    displayName, displayStr);
                }
            }
        }

        private static void ProcessSpillToTempDb(ExpandableObjectWrapper wrapper, ref string displayName)
        {
            if (wrapper["SpillToTempDb"] != null)
            {
                List<ExpandableObjectWrapper> propList = GetPropertyList(wrapper, "SpillToTempDb");

                foreach (ExpandableObjectWrapper eow in propList)
                {
                    PropertyValue pVal = eow.Properties["SpillLevel"] as PropertyValue;
                    string spillLevel = pVal.Value.ToString();
                    pVal = eow.Properties["SpilledThreadCount"] as PropertyValue;
                    string displayStr = pVal != null ?
                        string.Format(CultureInfo.CurrentCulture, SR.SpillToTempDb, spillLevel, pVal.Value.ToString()) :
                        string.Format(CultureInfo.CurrentCulture, SR.SpillToTempDbOld, spillLevel);

                    displayName = MergeString(CultureInfo.CurrentCulture.TextInfo.ListSeparator + " ",
                                      displayName, displayStr);
                }
            }

        }

        private static void GetCommonSpillDetails(ExpandableObjectWrapper eow, out string grantedMemory, out string usedMemory, out string writes, out string reads)
        {
            PropertyValue pVal = eow.Properties["GrantedMemoryKb"] as PropertyValue;
            grantedMemory = pVal.Value.ToString();
            pVal = eow.Properties["UsedMemoryKb"] as PropertyValue;
            usedMemory = pVal.Value.ToString();
            pVal = eow.Properties["WritesToTempDb"] as PropertyValue;
            writes = pVal.Value.ToString();
            pVal = eow.Properties["ReadsFromTempDb"] as PropertyValue;
            reads = pVal.Value.ToString();
        }

        private static void ProcessHashSpillDetails(ExpandableObjectWrapper wrapper, ref string displayName)
        {
            if (wrapper["HashSpillDetails"] != null)
            {
                List<ExpandableObjectWrapper> propList = GetPropertyList(wrapper, "HashSpillDetails");

                string grantedMemory;
                string usedMemory;
                string writes;
                string reads;
                foreach (ExpandableObjectWrapper eow in propList)
                {
                    GetCommonSpillDetails(eow, out grantedMemory, out usedMemory, out writes, out reads);

                    string displayStr = string.Format(SR.HashSpillDetails, writes, reads, grantedMemory, usedMemory);

                    displayName = MergeString(CultureInfo.CurrentCulture.TextInfo.ListSeparator + " ",
                    displayName, displayStr);
                }
            }

        }

        private static void ProcessSortSpillDetails(ExpandableObjectWrapper wrapper, ref string displayName)
        {
            if (wrapper["SortSpillDetails"] != null)
            {
                List<ExpandableObjectWrapper> propList = GetPropertyList(wrapper, "SortSpillDetails");

                string grantedMemory;
                string usedMemory;
                string writes;
                string reads;
                foreach (ExpandableObjectWrapper eow in propList)
                {
                    GetCommonSpillDetails(eow, out grantedMemory, out usedMemory, out writes, out reads);

                    string displayStr = string.Format(SR.SortSpillDetails, writes, reads, grantedMemory, usedMemory);

                    displayName = MergeString(CultureInfo.CurrentCulture.TextInfo.ListSeparator + " ",
                    displayName, displayStr);
                }
            }

        }

        private static void ProcessSpillOccurred(ExpandableObjectWrapper wrapper, ref string displayName)
        {
            if (wrapper["SpillOccurred"] != null)
            {
                displayName = MergeString(CultureInfo.CurrentCulture.TextInfo.ListSeparator + " ",
                    displayName, SR.SpillOccurredDisplayString);
            }
        }

        private static void ProcessNoJoinPredicate(ExpandableObjectWrapper wrapper, ref string displayName)
        {
            if (Object.Equals(wrapper["NoJoinPredicate"], true))
            {
                displayName = MergeString(CultureInfo.CurrentCulture.TextInfo.ListSeparator + " ",
                    displayName, SR.NoJoinPredicate);
            }
        }

        private static void ProcessColumnWithNoStatistics(ExpandableObjectWrapper wrapper, ref string displayName)
        {
            if (wrapper["ColumnsWithNoStatistics"] != null)
            {

                ExpandableObjectWrapper eow = wrapper["ColumnsWithNoStatistics"] as ExpandableObjectWrapper;
                PropertyValue pVal = eow.Properties["ColumnReference"] as PropertyValue;
                string displayStr = pVal.Value.ToString();

                displayName = SR.NameValuePair(SR.ColumnsWithNoStatistics, displayStr);
            }
        }

	private static void ProcessFullUpdateForOnlineIndexBuild(ExpandableObjectWrapper wrapper, ref string displayName)
	{
            if (wrapper["FullUpdateForOnlineIndexBuild"] != null)
            {
                displayName = MergeString(CultureInfo.CurrentCulture.TextInfo.ListSeparator + " ",
                    displayName, SR.FullUpdateForOnlineIndexBuild);
      	    }
	}


        private static void ProcessPlanAffectingConvert(ExpandableObjectWrapper wrapper, ref string displayName)
        {
            if (wrapper["PlanAffectingConvert"] != null)
            {
                List<ExpandableObjectWrapper> propList = GetPropertyList(wrapper, "PlanAffectingConvert");

                foreach (ExpandableObjectWrapper eow in propList)
                {
                    PropertyValue pVal = eow.Properties["ConvertIssue"] as PropertyValue;
                    string convertIssue = pVal.Value.ToString();
                    pVal = eow.Properties["Expression"] as PropertyValue;
                    string expression = pVal.Value.ToString();
                    string displayStr = string.Format(SR.PlanAffectingConvert, expression, convertIssue);

                    displayName = MergeString(CultureInfo.CurrentCulture.TextInfo.ListSeparator + " ",
                    displayName, displayStr);
                }
            }
        }

        private static void ProcessMemoryGrantWarning(ExpandableObjectWrapper wrapper, ref string displayName)
        {
            if (wrapper["MemoryGrantWarning"] != null)
            {
                List<ExpandableObjectWrapper> propList = GetPropertyList(wrapper, "MemoryGrantWarning");

                foreach (ExpandableObjectWrapper eow in propList)
                {
                    PropertyValue pValKind = eow.Properties["GrantWarningKind"] as PropertyValue;
                    PropertyValue pValRequested = eow.Properties["RequestedMemory"] as PropertyValue;
                    PropertyValue pValGranted = eow.Properties["GrantedMemory"] as PropertyValue;
                    PropertyValue pValUsed = eow.Properties["MaxUsedMemory"] as PropertyValue;

                    if (pValKind != null && pValGranted != null && pValRequested != null && pValUsed != null)
                    {
                        string displayString = string.Format(SR.MemoryGrantWarning, pValKind.Value.ToString(),
                            pValRequested.Value.ToString(), pValGranted.Value.ToString(), pValUsed.Value.ToString());

                        displayName = MergeString(CultureInfo.CurrentCulture.TextInfo.ListSeparator + " ",
                            displayName, displayString);
                    }
                }
            }
        }
        
        private static List<ExpandableObjectWrapper> GetPropertyList(ExpandableObjectWrapper wrapper, string propertyName)
        {
            List<ExpandableObjectWrapper> propList = new List<ExpandableObjectWrapper>();

            foreach (PropertyDescriptor pd in wrapper.Properties)
            {
                if (pd.Name == propertyName)
                {
                    PropertyValue pVal = pd as PropertyValue;
                    propList.Add(pVal.Value as ExpandableObjectWrapper);
                }
            }

            return propList;
        }

        /// <summary>
        /// Converts WarningsType to a wrapper object.
        /// </summary>
        /// <param name="item">Object to convert.</param>
        /// <returns>Wrapper object.</returns>
        public static ExpandableObjectWrapper Convert(MemoryFractionsType item)
        {
            return ConvertToWrapperObject(item);
        }

        /// <summary>
        /// Converts ScalarType[][] to a string
        /// The format looks like (1,2,3), (4,5,6)
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static ExpandableObjectWrapper Convert(ScalarType[][] items)
        {
            ExpandableObjectWrapper wrapper = new ExpandableArrayWrapper(items);
            string separator = CultureInfo.CurrentCulture.TextInfo.ListSeparator + " ";
            StringBuilder stringBuilder = new StringBuilder();

            for (int i = 0; i < items.Length; i++)
            {
                if (i != 0)
                {
                    stringBuilder.Append(separator);
                }

                stringBuilder.Append("(");

                for (int j = 0; j < items[i].Length; j++ )
                {
                    if (j != 0)
                    {
                        stringBuilder.Append(separator);
                    }
                    stringBuilder.Append(Convert(items[i][j]));
                }

                stringBuilder.Append(")");
            }

            wrapper.DisplayName = stringBuilder.ToString();
            return wrapper;
        }

        /// <summary>
        /// Converts ScalarType to a wrapper object
        /// </summary>
        /// <param name="item">scalar to be converted</param>
        /// <returns></returns>
        public static ExpandableObjectWrapper Convert(ScalarType item)
        {
            ExpandableObjectWrapper wrapper = new ExpandableObjectWrapper(item);
            if (!string.IsNullOrEmpty(item.ScalarString)) // optional attribute
            {
                wrapper.DisplayName = String.Format(
                    CultureInfo.CurrentCulture, "{0}({1})", SR.ScalarOperator, item.ScalarString);
            }
            else
            {
                wrapper.DisplayName = SR.ScalarOperator;
            }
            return wrapper;
        }

        /// <summary>
        /// Converts ScalarType to a string.
        /// </summary>
        /// <param name="item">Object to convert.</param>
        /// <returns>Scalar string.</returns>
        public static string Convert(ScalarExpressionType item)
        {
            return item.ScalarOperator != null ? item.ScalarOperator.ScalarString : String.Empty;
        }

        /// <summary>
        /// Converts CompareType to a wrapper object.
        /// </summary>
        /// <param name="item">Object to convert.</param>
        /// <returns>Wrapper object.</returns>
        public static ExpandableObjectWrapper Convert(CompareType item)
        {
            ExpandableObjectWrapper wrapper = new ExpandableObjectWrapper(item);
            
            object scalarOperator = wrapper["ScalarOperator"];
            if (scalarOperator != null)
            {
                wrapper.DisplayName = item.CompareOp.ToString();
            }
            else
            {
                wrapper.DisplayName = String.Format(
                    CultureInfo.CurrentCulture, "{0}({1})", item.CompareOp, scalarOperator);
            }

            return wrapper;
        }

        /// <summary>
        /// Converts OrderByTypeOrderByColumn to a wrapper object.
        /// </summary>
        /// <param name="item">Object to convert.</param>
        /// <returns>Wrapper object.</returns>
        public static ExpandableObjectWrapper Convert(OrderByTypeOrderByColumn item)
        {
            ExpandableObjectWrapper wrapper = new ExpandableObjectWrapper(item);
            wrapper.DisplayName = String.Format(
                CultureInfo.CurrentCulture,
                "{0} {1}",
                wrapper["ColumnReference"],
                item.Ascending ? SR.Ascending : SR.Descending);
            return wrapper;
        }

        /// <summary>
        /// Converts ScanRangeType to a wrapper object.
        /// </summary>
        /// <param name="item">Object to convert.</param>
        /// <returns>Wrapper object.</returns>
        public static ExpandableObjectWrapper Convert(ScanRangeType item)
        {
            ExpandableObjectWrapper wrapper = new ExpandableObjectWrapper(item);

            object rangeColumn = wrapper["RangeColumns"];
            object rangeExpressions = wrapper["RangeExpressions"];

            if (rangeColumn != null && rangeExpressions != null)
            {
                string compareOperator = String.Empty;
                switch (item.ScanType)
                {
                    case CompareOpType.EQ:
                        compareOperator = "=";
                        break;
                    case CompareOpType.GE:
                        compareOperator = ">=";
                        break;
                    case CompareOpType.GT:
                        compareOperator = ">";
                        break;
                    case CompareOpType.LE:
                        compareOperator = "<=";
                        break;
                    case CompareOpType.LT:
                        compareOperator = "<";
                        break;
                    case CompareOpType.NE:
                        compareOperator = "<>";
                        break;
                }

                if (compareOperator.Length > 0)
                {
                    wrapper.DisplayName = String.Format(
                        CultureInfo.CurrentCulture,
                        "{0} {1} {2}",
                        rangeColumn,
                        compareOperator,
                        rangeExpressions);
                }
                else
                {
                    wrapper.DisplayName = String.Format(
                        CultureInfo.CurrentCulture,
                        "{0}({1})",
                        item.ScanType,
                        MergeString(",", rangeColumn, rangeExpressions));
                }
            }

            return wrapper;
        }

        /// <summary>
        /// Converts SeekPredicateType to a wrapper object.
        /// </summary>
        /// <param name="item">Object to convert.</param>
        /// <returns>Wrapper object.</returns>
        public static ExpandableObjectWrapper Convert(SeekPredicateType item)
        {
            ExpandableObjectWrapper wrapper = new ExpandableObjectWrapper(item);
            // Make display name from names and values of the following 3 properties
            wrapper.DisplayName = MakeDisplayNameFromObjectNamesAndValues(wrapper,
                "Prefix", "StartRange", "EndRange");

            return wrapper;
        }


        /// <summary>
        /// Converts SeekPredicatesType to a wrapper object.
        /// </summary>
        /// <param name="items">Objects to convert.</param>
        /// <returns>Wrapper object.</returns>
        public static ExpandableObjectWrapper Convert(SeekPredicatesType item)
        {
            ExpandableObjectWrapper wrapper = null;
            if (item.Items.Length > 0 && item.Items[0] is SeekPredicateNewType)
            {
                // New schema. Parse it differently.

                wrapper = new ExpandableArrayWrapper(item.Items);
                string separator = CultureInfo.CurrentCulture.TextInfo.ListSeparator + " ";

                PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(wrapper);
                if (properties.Count > 1)
                {
                    // If there are more than one SeekPredicateNew, merge the tooltips
                    StringBuilder stringBuilder = new StringBuilder();
                    for (int i = 0; i < properties.Count; i++)
                    {
                        if (i != 0)
                        {
                            stringBuilder.Append(separator);
                        }
                        stringBuilder.Append(String.Format(
                                CultureInfo.CurrentCulture,
                                "{0} {1}",
                                properties[i].DisplayName,
                                properties[i].GetValue(wrapper).ToString()));
                    }
                    wrapper.DisplayName = stringBuilder.ToString();
                }
            }
            else
            {
                wrapper = new ExpandableArrayWrapper(item.Items);
            }
            return wrapper;
        }

        /// <summary>
        /// Converts SeekPredicateNewType to a wrapper object.
        /// </summary>
        /// <param name="items">Objects to convert.</param>
        /// <returns>Wrapper object.</returns>
        public static ExpandableObjectWrapper Convert(SeekPredicateNewType item)
        {
            ExpandableObjectWrapper wrapper = new ExpandableArrayWrapper(item.SeekKeys);

            // Add string "SeekKeys" to the tooltip
            string separator = CultureInfo.CurrentCulture.TextInfo.ListSeparator + " ";
            StringBuilder stringBuilder = new StringBuilder();

            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(wrapper);
            for (int i = 0; i < properties.Count; i++)
            {
                if (i != 0)
                {
                    stringBuilder.Append(separator);
                }
                stringBuilder.Append(String.Format(
                        CultureInfo.CurrentCulture,
                        "{0}[{1}]: {2}",
                        SR.SeekKeys,
                        i+1,
                        properties[i].GetValue(wrapper).ToString()));
            }

            wrapper.DisplayName = stringBuilder.ToString();
            return wrapper;
        }

		/// <summary>
        /// Converts SeekPredicatePartType to a wrapper object.
        /// </summary>
        /// <param name="items">Objects to convert.</param>
        /// <returns>Wrapper object.</returns>
        public static ExpandableObjectWrapper Convert(SeekPredicatePartType item)
        {
            ExpandableObjectWrapper wrapper = new ExpandableArrayWrapper(item.Items);
            string separator = CultureInfo.CurrentCulture.TextInfo.ListSeparator + " ";

            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(wrapper);
            if (properties.Count > 1)
            {
                // If there are more than one SeekPredicateNew, merge the tooltips
                StringBuilder stringBuilder = new StringBuilder();
                for (int i = 0; i < properties.Count; i++)
                {
                    if (i != 0)
                    {
                        stringBuilder.Append(separator);
                    }
                    stringBuilder.Append(String.Format(
                            CultureInfo.CurrentCulture,
                            "{0} {1}",
                            properties[i].DisplayName,
                            properties[i].GetValue(wrapper).ToString()));
                }
                wrapper.DisplayName = stringBuilder.ToString();
            }
            return wrapper;
        }
		
        /// <summary>
        /// Converts MergeColumns to a wrapper object.
        /// </summary>
        /// <param name="item">Object to convert.</param>
        /// <returns>Wrapper object.</returns>
        public static ExpandableObjectWrapper Convert(MergeColumns item)
        {
            ExpandableObjectWrapper wrapper = new ExpandableObjectWrapper(item);
            
            object innerSideJoinColumns = wrapper["InnerSideJoinColumns"];
            object outerSideJoinColumns = wrapper["OuterSideJoinColumns"];

            if (innerSideJoinColumns != null && outerSideJoinColumns != null)
            {
                wrapper.DisplayName = String.Format(
                    CultureInfo.CurrentCulture,
                    "({0}) = ({1})",
                    innerSideJoinColumns,
                    outerSideJoinColumns);
            }

            return wrapper;
        }

        public static string Convert(BaseStmtInfoTypeStatementOptmEarlyAbortReason item)
        {
            switch (item)
            {
                case BaseStmtInfoTypeStatementOptmEarlyAbortReason.TimeOut:
                    return SR.TimeOut;

                case BaseStmtInfoTypeStatementOptmEarlyAbortReason.MemoryLimitExceeded:
                    return SR.MemoryLimitExceeded;

                case BaseStmtInfoTypeStatementOptmEarlyAbortReason.GoodEnoughPlanFound:
                    return SR.GoodEnoughPlanFound;

                default:
                    return item.ToString();
            }
        }

        public static string Convert(CloneAccessScopeType item)
        {
            switch (item)
            {
                case CloneAccessScopeType.Primary:
                    return SR.PrimaryClones;

                case CloneAccessScopeType.Secondary:
                    return SR.SecondaryClones;

                case CloneAccessScopeType.Both:
                    return SR.BothClones;

                case CloneAccessScopeType.Either:
                    return SR.EitherClones;

                case CloneAccessScopeType.ExactMatch:
                    return SR.ExactMatchClones;

                default:
                    return item.ToString();
            }
        }

        #endregion

        #region Converters for types to be ignored

        public static object Convert(InternalInfoType item)
        {
            ExpandableObjectWrapper wrapper = new ExpandableObjectWrapper();
            StringBuilder stringBuilder = new StringBuilder();

            using (XmlTextWriter writer = new XmlTextWriter(new StringWriter(stringBuilder, CultureInfo.InvariantCulture)))
            {
                writer.WriteStartElement("InternalInfo");

                if (item.AnyAttr != null)
                {
                    foreach (XmlAttribute attribute in item.AnyAttr)
                    {
                        object value = ObjectWrapperTypeConverter.Convert(attribute);
                        if (value != null)
                        {
                            wrapper[attribute.Name] = value;
                            writer.WriteAttributeString(XmlConvert.EncodeLocalName(attribute.Name), value.ToString());
                        }
                    }
                }

                if (item.Any != null)
                {
                    foreach (XmlElement node in item.Any)
                    {
                        object value = ObjectWrapperTypeConverter.Convert(node);
                        if (value != null)
                        {
                            wrapper[node.Name] = value;
                            writer.WriteRaw(Convert(node).ToString());
                        }
                    }
                }

                writer.WriteEndElement();
            }

            wrapper.DisplayName = stringBuilder.ToString();
            return wrapper;
        }

        public static object Convert(System.Xml.XmlElement item)
        {
            ExpandableObjectWrapper wrapper = new ExpandableObjectWrapper();

            StringBuilder stringBuilder = new StringBuilder();

            using (XmlTextWriter writer = new XmlTextWriter(new StringWriter(stringBuilder, CultureInfo.InvariantCulture)))
            {
                writer.WriteStartElement(XmlConvert.EncodeLocalName(item.Name));

                foreach (XmlAttribute attribute in item.Attributes)
                {
                    object value = ObjectWrapperTypeConverter.Convert(attribute);
                    if (value != null)
                    {
                        wrapper[attribute.Name] = value;
                        writer.WriteAttributeString(XmlConvert.EncodeLocalName(attribute.Name), value.ToString());
                    }
                }

                foreach (XmlElement node in item.ChildNodes)
                {
                    object value = ObjectWrapperTypeConverter.Convert(node);
                    if (value != null)
                    {
                        wrapper[node.Name] = value;
                        writer.WriteRaw(Convert(node).ToString());
                    }
                }

                writer.WriteEndElement();
            }

            wrapper.DisplayName = stringBuilder.ToString();
            return wrapper;
        }

        public static object Convert(System.Xml.XmlAttribute item)
        {
            return item.Value;
        }

        #endregion

        #region TypeConverter overrides

        /// <summary>
        /// Determines if this converter can convert an object from the specified type.
        /// </summary>
        /// <param name="context">Type descriptor context.</param>
        /// <param name="sourceType">Source object type.</param>
        /// <returns>True if the object can be converted; otherwise false.</returns>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return Type.GetTypeCode(sourceType) == TypeCode.Object || sourceType.IsArray || convertMethods.ContainsKey(sourceType);
        }

        /// <summary>
        /// Converts an object to a type supported by this converter.
        /// Note that the target type is determined by the converter itself.
        /// </summary>
        /// <param name="context">Type descriptor context.</param>
        /// <param name="culture">Culture.</param>
        /// <param name="value">The object or value to convert from.</param>
        /// <returns>The converted object.</returns>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            MethodInfo converter;
            if (convertMethods.TryGetValue(value.GetType(), out converter))
            {
                return converter.Invoke(
                    null,
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new object[]{ value },
                    CultureInfo.CurrentCulture);
            }
            else
            {
                // TODO: review this - may need better condition
                return ConvertToObjectWrapper(value);
            }
        }

        /// <summary>
        /// Converts an object to a specified type.
        /// </summary>
        /// <param name="context">Type descriptor context.</param>
        /// <param name="culture">Culture.</param>
        /// <param name="value">The object or value to convert from.</param>
        /// <param name="destType">Target type to convert to.</param>
        /// <returns>The converted object.</returns>
        public override object ConvertTo(ITypeDescriptorContext context,
                                     System.Globalization.CultureInfo culture,
                                     object value, Type destType)
        {
            MethodInfo converter;
            if (convertMethods.TryGetValue(value.GetType(), out converter) && converter.ReturnType == destType)
            {
                return converter.Invoke(this, new object[] { value });
            }
            else
            {
                // TODO: review this - may need better condition
                return ConvertToObjectWrapper(value);
            }
        }

        #endregion

        #region Implementation details

        /// <summary>
        /// Converts an object to a wrapper object.
        /// </summary>
        /// <param name="item">An object to convert.</param>
        /// <returns>Array or object wrapper that implements ICustomTypeDescriptor and provides expandable properties.</returns>
        private static object ConvertToObjectWrapper(object item)
        {
            ICollection collection = item as ICollection;
            if (collection != null)
            {
                if (collection.Count == 1)
                {
                    // There is only one object in the collection
                    // so return the first item.
                    IEnumerator enumerator = collection.GetEnumerator();
                    enumerator.MoveNext();
                    return ObjectWrapperTypeConverter.Default.ConvertFrom(enumerator.Current);
                }
                else
                {
                        return new ExpandableArrayWrapper(collection);
                }
            }
            else
            {
                // Non-collection case.
                return new ExpandableObjectWrapper(item);
            }
        }

        /// <summary>
        /// Static constructor
        /// </summary>
        static ObjectWrapperTypeConverter()
        {
            // Hash all Convert methods by their argument type
            foreach (MethodInfo methodInfo in typeof(ObjectWrapperTypeConverter).GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (methodInfo.Name == "Convert")
                {
                    ParameterInfo[] parameters = methodInfo.GetParameters();
                    if (parameters.Length == 1)
                    {
                        convertMethods.Add(parameters[0].ParameterType, methodInfo);
                    }
                }
            }
        }

        /// <summary>
        /// Constructs string from multiple items.
        /// </summary>
        /// <param name="separator">Separator placed between items.</param>
        /// <param name="items">Items to be merged.</param>
        /// <returns>Text string that contains merged items with separators between them.</returns>
        internal static string MergeString(string separator, params object[] items)
        {
            StringBuilder builder = new StringBuilder();

            foreach (object item in items)
            {
                if (item != null)
                {
                    string itemText = item.ToString();

                    if (itemText.Length > 0)
                    {
                        if (builder.Length > 0)
                        {
                            builder.Append(separator);
                        }

                        builder.Append(itemText);
                    }
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Makes a comma separated list from object property names and values.
        /// This method overload enumerates all properties.
        /// </summary>
        /// <param name="item">Object to get display name for.</param>
        /// <returns>Display name string.</returns>
        private static string MakeDisplayNameFromObjectNamesAndValues(object item)
        {
            StringBuilder stringBuilder = new StringBuilder();

            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(item))
            {
                AppendPropertyNameValuePair(stringBuilder, item, property);
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Makes a comma separated list from object property names and values.
        /// This method overload uses only specified properties.
        /// </summary>
        /// <param name="item">Object to get display name for.</param>
        /// <returns></returns>
        private static string MakeDisplayNameFromObjectNamesAndValues(object item, params string[] propertyNames)
        {
            StringBuilder stringBuilder = new StringBuilder();
            PropertyDescriptorCollection allProperties = TypeDescriptor.GetProperties(item);
            foreach (string name in propertyNames)
            {
                PropertyDescriptor property = allProperties[name];
                if (property != null)
                {
                    AppendPropertyNameValuePair(stringBuilder, item, property);
                }
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// An utility method that appends property name and value to string builder.
        /// </summary>
        /// <param name="stringBuilder">String builder.</param>
        /// <param name="item">Object that contains properties.</param>
        /// <param name="property">Property Descriptor.</param>
        private static void AppendPropertyNameValuePair(StringBuilder stringBuilder, object item, PropertyDescriptor property)
        {
            object propertyValue = property.GetValue(item);
            if (propertyValue != null)
            {
                if (stringBuilder.Length > 0)
                {
                    stringBuilder.Append(CultureInfo.CurrentCulture.TextInfo.ListSeparator);
                    stringBuilder.Append(" ");
                }

                stringBuilder.Append(SR.NameValuePair(property.DisplayName, propertyValue.ToString()));
            }
        }

        private static Dictionary<Type, MethodInfo> convertMethods = new Dictionary<Type, MethodInfo>();

        #endregion
    }
}
