using System.Xml;

namespace Microsoft.SqlTools.ShowPlan
{
    public interface IXdocAndNamespaceManager
    {
        XmlNamespaceManager Nsmgr { get; set; }
        XmlDocument Xdoc { get; set; }

        void LoadShowPlanXmlAndAddShowPlanNamespace(string showPlanXML, bool isFilePath);
    }
}