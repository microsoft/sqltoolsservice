//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Text;
using System.Xml;

using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlTools.ServiceLayer.Management
{
    /// <summary>
    /// Provides helper functions for converting urn enumerator urns
    /// to olap path equivilent.
    /// </summary>
#if DEBUG || EXPOSE_MANAGED_INTERNALS
    public
#else
    internal
#endif
    class UrnDataPathConverter
    {
        // static only members
        private UrnDataPathConverter()
        {
        }

        /// <summary>
        /// Convert a Urn to and olap compatible datapath string
        /// </summary>
        /// <param name="urn">Source urn</param>
        /// <returns>string that Xml that can be used as an olap path</returns>
        /// <remarks>
        /// Node types are
        /// ServerID
        /// DatabaseID
        /// CubeID
        /// DimensionID
        /// MeasureGroupID
        /// PartitionID
        /// MiningStructureID
        /// MininingModelID
        /// 
        /// These currently map mostly to enuerator types with the addition of ID
        /// 
        /// string is of the format <ObjectTypeID>ObjectID</ObjectTypeID>�.<ObjectTypeID>ObjectID</ObjectTypeID>
        /// 
        /// </remarks>
        public static string ConvertUrnToDataPath(Urn urn)
        {
            String element = String.Empty;
            if(urn == null)
            {
                throw new ArgumentNullException("urn");
            }

            StringWriter stringWriter = new StringWriter();
            XmlTextWriter xmlWriter = new XmlTextWriter(stringWriter);

            ConvertUrnToDataPath(urn, xmlWriter);

            xmlWriter.Flush();
            xmlWriter.Close();

            return stringWriter.ToString();
        }
        /// <summary>
        /// Datapath conversion helper. Does the conversion using XmlWriter and recursion.
        /// </summary>
        /// <param name="urn">Urn to be converted</param>
        /// <param name="writer">XmlWriter that the results will be written to.</param>
        private static void ConvertUrnToDataPath(Urn urn, XmlWriter xmlWriter)
        {
            if(urn == null)
            {
                throw new ArgumentNullException("urn");
            }
            if(xmlWriter == null)
            {
                throw new ArgumentNullException("xmlWriter");
            }

            // preserve the order so do the parent first
            Urn parent = urn.Parent;
            if(parent != null)
            {
                ConvertUrnToDataPath(parent, xmlWriter);
            }

            String tag = urn.Type;

            // don't put server into the olap path.
            if(tag != "OlapServer")
            {
                xmlWriter.WriteElementString(tag + "ID", urn.GetAttribute("ID"));
            }
        }
        /// <summary>
        /// Convert an xml body string that is compatible with a string representation
        /// (i.e. deal with < > &)
        /// </summary>
        /// <param name="s">source</param>
        /// <returns>string that can be used as the body for xml stored in a string</returns>
        public static string TokenizeXml(string source)
        {
            System.Diagnostics.Debug.Assert(false, "do not use this function. See bugs 322423 and 115450 in SQLBU Defect tracking");

            if(null == source) return String.Empty;

            StringBuilder sb = new StringBuilder();
            foreach(char c in source)
            {
                switch(c)
                {
                    case '<':
                        sb.Append("&lt;");
                        break;
                    case '>':
                        sb.Append("&gt;");
                        break;
                    case '&':
                        sb.Append("&amp;");
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
