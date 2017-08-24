using System;
using System.Xml;

namespace Microsoft.SqlTools.ShowPlan
{
    public class XdocAndNamespaceManager : IXdocAndNamespaceManager
    {
        public XmlDocument Xdoc {get;set;}

        public XmlNamespaceManager Nsmgr{get;set;}

        private const string ShowplanNamespace = "http://schemas.microsoft.com/sqlserver/2004/07/showplan"; //can also call root.getDefaultNamespace();

        /// <summary>
        /// Xdoc initialization/loading xml into Xdoc and name space manager initialization/adding showplan namespace        
        /// </summary>
        /// <param name="showPlanXML">String literal FILE PATH to xml file to be parsed</param>
        /// <param name="isFilePath">boolean, true = string of ShowPlanXml</param>

        public void LoadShowPlanXmlAndAddShowPlanNamespace(string showPlanXML, Boolean isFilePath)
        {
            if (showPlanXML==null || showPlanXML.Equals(string.Empty))
            {
                throw new System.Exception("LoadXmlFilePathStringAndAddShowPlanNamespace::XML file path string cannot be null or empty");
            }
            Xdoc = new XmlDocument();
            if (isFilePath)
            {
                Xdoc.Load(showPlanXML); //Load, for file path
            }
            else
            {
                Xdoc.LoadXml(showPlanXML); //LoadXml, for string literal xml
            }
            Nsmgr = new XmlNamespaceManager(Xdoc.NameTable);
            Nsmgr.AddNamespace("shp", ShowplanNamespace);
        }
    }
}
