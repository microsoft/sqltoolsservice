using System.Collections.Generic;
using System.Xml;

namespace Microsoft.SqlTools.ShowPlan
{
    public interface ITraverseXml
    {
        void Traverse(XmlNode xmlNode, string nodeId, Dictionary<string, HashSet<string>> nodeIdAndColRefsResultDict);
    }
}