using System.Xml;

namespace Microsoft.SqlTools.ShowPlan
{
    public class FundamentalSharedParserLogic
    {
        protected internal readonly string NotFound = "!!NOT FOUND!!"; // to be returned and checked for when a value is not parsed/found

        ///<summary>GetStatementLabel returns NotFound or string value for the node's specified label</summary>
        /// <param name="node">Xml Node as input, with it's values we want still present, from the xdoc</param>
        /// <param name="xmlLabel">String literal matching the xml label which has the desired value</param>
        /// <returns>A string literal containing the value for the given inputted xmlnode and label</returns>
        protected internal string GetStatementLabel(XmlNode node, string xmlLabel)
        {
            if (node.Attributes?[xmlLabel] != null)
            {
                string text = node.Attributes[xmlLabel].Value;
                if (!string.IsNullOrEmpty(text))
                {
                    text = text.TrimStart();
                    return text;
                }
            }
            return NotFound;
        }
    }
}
