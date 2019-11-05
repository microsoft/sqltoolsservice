//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Xml;
using System.Collections;
using System.Diagnostics;

namespace Microsoft.SqlTools.ServiceLayer.Management
{
    /// <summary>
    /// SqlTools Parameters, used to define what goes into starting up a Workbench Form
    ///  AKA a dbCommander, AKA a "dialog"
    ///  These parameters are xml snippets
    /// </summary>
    public class STParameters
    {
        public XmlDocument m_doc;

        /// <summary>
        /// The data type we are interested in
        /// </summary>
        public enum STType { eNULL, eInt, eLong, eString };

        /// <summary>
        /// default constructor
        /// </summary>
        public STParameters()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="xmlDoc">The xml snippet used to control the dbCommander</param>
        public STParameters(XmlDocument xmlDoc)
        {
            m_doc = xmlDoc;
        }

        /// <summary>
        /// Changing the xml snippet we are using
        /// </summary>
        /// <param name="xmlDoc">the new xml snippet</param>
        public void SetDocument(XmlDocument xmlDoc)
        {
            m_doc = xmlDoc;
        }

        /// <summary>
        /// Access to the xml we are using for dbCommander parameters
        /// </summary>
        /// <returns>our current parameters</returns>
        public XmlDocument GetDocument()
        {
            return m_doc;
        }

        /// <summary>
        /// Search for an xml tag, and return its value
        /// </summary>
        /// <param name="parameterName">the xml tag name</param>
        /// <param name="value">the value of that tag</param>
        /// <returns>flag that is true if the data was found, false if not</returns>
        public bool GetBaseParam(string parameterName, ref object value)
        {
            XmlNodeList nodeList = null;
            bool parameterExists;

            if (m_doc == null)
                return false;

            parameterExists = false;
            nodeList = m_doc.GetElementsByTagName(parameterName);

            if (nodeList.Count > 1)
            {
                value = null;
            }
            else if (nodeList.Count != 0)     // anything there?
            {
                try
                {
                    XmlNode node = nodeList.Item(0);
                    if (null != node)
                    {
                        value = node.InnerText as object;
                        parameterExists = true;
                    }
                }
                catch (Exception /*e*/)
                {
                }

            }

            return parameterExists;
        }

        /// <summary>
        /// Finds an existing xml tag, and sets it to a new value, or if the tag is not found
        /// create it and set it's value
        /// </summary>
        /// <param name="parameterName">tag name</param>
        /// <param name="value">new value</param>
        /// <returns>flag that is true if the tag was set, false if not</returns>
        public bool SetBaseParam(string parameterName, object value)
        {
            XmlNodeList nodeList;
            bool success = false;

            nodeList = m_doc.GetElementsByTagName(parameterName);

            if (nodeList.Count == 1)
            {
                try
                {
                    nodeList.Item(0).InnerText = (string)value;
                    success = true;
                }
                catch (InvalidCastException /*e*/)
                {
                    success = false;
                }

            }

            if (nodeList.Count == 0)
            {
                try
                {
                    XmlElement xmlElement = m_doc.CreateElement(parameterName);
                    XmlNode root = m_doc.DocumentElement;

                    nodeList = m_doc.GetElementsByTagName("params");

                    if (nodeList.Count == 1 && value is string)
                    {
                        xmlElement.InnerText = (string)value;
                        nodeList.Item(0).InsertAfter(xmlElement, nodeList.Item(0).LastChild);

                        success = true;
                    }
                }
                catch (Exception e)
                {
                    string sz = e.ToString();
                    success = false;
                }

            }

            return success;

        }

        /// <summary>
        /// Get back an interger parameter.
        /// NOTE: if the tag exists, but it contains non-numeric data, this will throw
        /// An exception of type 'System.FormatException' 
        /// with Additional information: Could not find any parsible digits.
        /// </summary>
        /// <param name="parameterName">xml tag name for the parameter of interest</param>
        /// <param name="iValue">out value of parameter</param>
        /// <returns>flag that is true if the data was found, false if not</returns>
        public bool GetParam(string parameterName, out int value)
        {
            bool parameterExists = false;

            value = 0;

            try
            {
                object oAux = null;

                if (parameterExists = GetBaseParam(parameterName, ref oAux))
                {
                    try
                    {
                        value = Convert.ToInt32((string)oAux, 10);    // data is always a string, it is the value of XmlNode.InnerText
                    }
                    catch (FormatException e)
                    {
                        Debug.WriteLine(String.Format(System.Globalization.CultureInfo.CurrentCulture, "Non numeric data in tag: {0}", parameterName));
                        Debug.WriteLine(e.Message);
                        throw;
                    }
                }
            }
            catch (InvalidCastException /*e*/)
            {
            }

            return parameterExists;
        }

        /// <summary>
        /// Accessor for a boolean parameter
        /// NOTE: if the tag exists, but it contains non-numeric data, this will throw
        /// An exception of type 'System.FormatException' 
        /// with Additional information: Could not find any parsible digits.
        /// </summary>
        /// <param name="parameterName">xml tag name for the parameter of interest</param>
        /// <param name="value">out value of parameter</param>
        /// <returns>flag that is true if the data was found, false if not</returns>
        public bool GetParam(string parameterName, ref bool value)
        {
            bool parameterExists = false;

            value = false;

            try
            {
                object oAux = null;
                if (parameterExists = GetBaseParam(parameterName, ref oAux))
                {
                    try
                    {
                        value = Convert.ToBoolean((string)oAux, System.Globalization.CultureInfo.InvariantCulture);   // data is always a string, it is the value of XmlNode.InnerText
                    }
                    catch (FormatException e)
                    {
                        Debug.WriteLine(String.Format(System.Globalization.CultureInfo.CurrentCulture, "Non boolean data in tag: {0}", parameterName));
                        Debug.WriteLine(e.Message);
                        throw;
                    }
                }
            }
            catch (InvalidCastException /*e*/)
            {
            }

            return parameterExists;
        }

        /// <summary>
        /// Accessor to a string parameter
        /// </summary>
        /// <param name="parameterName">xml tag name for the parameter of interest</param>
        /// <param name="value">out value of parameter</param>
        /// <returns>flag that is true if the data was found, false if not</returns>
        public bool GetParam(string parameterName, ref string value)
        {
            bool parameterExists;

            value = "";
            parameterExists = false;

            try
            {
                object oAux = null;

                if (parameterExists = GetBaseParam(parameterName, ref oAux))
                {
                    value = (string)oAux;
                }
            }
            catch (InvalidCastException /*e*/)
            {
            }

            return parameterExists;

        }

        /// <summary>
        /// Accessor to long parameter (Int64)
        /// NOTE: if the tag exists, but it contains non-numeric data, this will throw
        /// An exception of type 'System.FormatException' 
        /// with Additional information: Could not find any parsible digits.
        /// </summary>
        /// <param name="parameterName">xml tag name for the parameter of interest</param>
        /// <param name="value">out value of parameter</param>
        /// <returns>flag that is true if the data was found, false if not</returns>
        public bool GetParam(string parameterName, out long value)
        {
            bool parameterExists = false;

            value = 0;

            try
            {
                object oAux = null;

                if (parameterExists = GetBaseParam(parameterName, ref oAux))
                {
                    try
                    {
                        value = Convert.ToInt64((string)oAux, 10);    // data is always a string, it is the value of XmlNode.InnerText
                    }
                    catch (FormatException e)
                    {
                        Debug.WriteLine(String.Format(System.Globalization.CultureInfo.CurrentCulture, "Non numeric data in tag: {0}", parameterName));
                        Debug.WriteLine(e.Message);
                        throw;
                    }
                }
            }
            catch (InvalidCastException /*e*/)
            {
            }

            return parameterExists;

        }


        /// <summary>
        /// Set an int (Int32) parameter
        /// </summary>
        /// <param name="parameterName">tag name for parameter</param>
        /// <param name="value">integer value</param>
        /// <returns>true if set was successful, false if not</returns>
        public bool SetParam(string parameterName, int value)
        {
            bool success;

            success = SetBaseParam(parameterName, (object)value);

            return success;

        }

        /// <summary>
        /// Set a string parameter
        /// </summary>
        /// <param name="parameterName">tag name for parameter</param>
        /// <param name="value">string value</param>
        /// <returns>true if set was successful, false if not</returns>
        public bool SetParam(string parameterName, string value)
        {
            bool success;

            success = SetBaseParam(parameterName, (object)value);

            return success;

        }

        /// <summary>
        /// Set a long (Int64) parameter
        /// </summary>
        /// <param name="parameterName">tag name for parameter</param>
        /// <param name="value">long value</param>
        /// <returns>true if set was successful, false if not</returns>
        public bool SetParam(string parameterName, long value)
        {
            bool success;

            success = SetBaseParam(parameterName, (object)value);

            return success;

        }

        /// <summary>
        /// Get a string collection parameter
        /// </summary>
        /// <param name="parameterName">name of collection</param>
        /// <param name="list">collection that gets filled up with parameters</param>
        /// <param name="getInnerXml">true if we want to get at inner nodes, false if not</param>
        /// <returns>true if parameter(s) exist</returns>
        public bool GetParam(string parameterName, System.Collections.Specialized.StringCollection list, bool getInnerXml)
        {
            /// necessary for OALP objects path that is in an XML form
            if (true == getInnerXml)
            {
                XmlNodeList nodeList;
                bool parameterExists;
                long lCount;

                parameterExists = false;
                nodeList = m_doc.GetElementsByTagName(parameterName);

                list.Clear();

                lCount = nodeList.Count;

                if (lCount > 0)
                {
                    parameterExists = true;

                    for (long i = 0; i < lCount; i++)
                    {
                        list.Add(nodeList.Item((int)i).InnerXml);
                    }
                }
                else
                {
                    parameterExists = false;
                }

                return parameterExists;
            }
            else
            {
                return GetParam(parameterName, list);
            }
        }

        /// <summary>
        /// Access to a collection of parameters
        /// </summary>
        /// <param name="parameterName">name of collection</param>
        /// <param name="list">list to fill with parameters</param>
        /// <returns>parameter(s) exist</returns>
        public bool GetParam(string parameterName, System.Collections.Specialized.StringCollection list)
        {
            XmlNodeList nodeList;
            bool parameterExists;
            long lCount;

            parameterExists = false;
            nodeList = m_doc.GetElementsByTagName(parameterName);

            list.Clear();

            lCount = nodeList.Count;

            if (lCount > 0)
            {
                parameterExists = true;

                for (long i = 0; i < lCount; i++)
                {
                    list.Add(nodeList.Item((int)i).InnerText);
                }
            }
            else
            {
                parameterExists = false;
            }

            return parameterExists;
        }

        public bool GetParam(string parameterName, ref ArrayList list)
        {
            System.Collections.Specialized.StringCollection stringList = new System.Collections.Specialized.StringCollection();
            bool parameterExists = GetParam(parameterName, stringList);
            list.Clear();
            if (!parameterExists)
            {
                return false;
            }
            else
            {
                for (int i = 0; i < stringList.Count; i++)
                {
                    list.Add(stringList[i]);
                }
                return true;
            }
        }

        /// <summary>
        /// This function does nothing but return false
        /// </summary>
        /// <param name="parameterName">ignored</param>
        /// <param name="type">ignored</param>
        /// <returns>always false</returns>
        public bool GetParamType(string parameterName, STType type)
        {
            bool whatever = false;

            return whatever;
        }

        /// <summary>
        /// This function does nothing but return false
        /// </summary>
        /// <param name="parameterName">ignored</param>
        /// <param name="type">ignored</param>
        /// <returns>always false</returns>
        public bool SetParamType(string parameterName, STType type)
        {
            bool whatever = false;

            return whatever;
        }

    }
}
