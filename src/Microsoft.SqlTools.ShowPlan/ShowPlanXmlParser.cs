using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;

namespace Microsoft.SqlTools.ShowPlan
{
    /// <summary>
    /// A class that takes in a list of XML Show Plan(s) (as filepaths or strings) 
    /// and Parses the ShowPlanXml into statements with nodes which are analyzed for/populated with their insights/probable cause(s) 
    /// </summary>
    public class ShowPlanXmlParser : TraverseXml
    {
        /// <summary>
        /// ParseSingleShowPlanXmlFilePath parses a SINGLE ShowPlanXml FILE from the given FILE PATH into a list of resulting statements
        /// which contain both a list of all populated AllRelOpNodes as well as a list of problem AllRelOpNodes for each statement
        /// </summary>
        /// <param name="showPlanXmlFilePath">filepath to single file containing ShowPlanXML (full path if xml is not in project dir/name of xml, if it is in the project debug dir)</param>
        /// <returns>list of objects which each hold all relevant data for a single ShowPlanXml</returns>
        public List<ShowPlanXmlStatement> ParseSingleShowPlanXmlFilePath(string showPlanXmlFilePath)
        {          
            return ExecuteParseShowPlanXmlFilePath(showPlanXmlFilePath);
        }

        /// <summary>
        /// ParseListOfShowPlanXmlFilePaths parses each ShowPlanXml FILE into a list of resulting statements
        /// which contain both a list of all populated AllRelOpNodes as well as a list of problem AllRelOpNodes for each statement
        /// </summary>
        /// <param name="listofShowPlanXmlFilePaths">list of strings containing XML path if xml is not in project dir/name of xml if it is in the project debug dir</param>
        /// <returns>list of objects which each hold all relevant data for a single ShowPlanXml </returns>
        public List<List<ShowPlanXmlStatement>> ParseListOfShowPlanXmlFilePaths(List<string> listofShowPlanXmlFilePaths)
        {
            List<List<ShowPlanXmlStatement>> results = new List<List<ShowPlanXmlStatement>>();
            foreach (string xmlFilePath in listofShowPlanXmlFilePaths)
            {
                results.Add(ExecuteParseShowPlanXmlFilePath(xmlFilePath));
            }
            return results;
        }

        /// <summary>
        /// ParseListOfShowPlanXmlStrings parses a single ShowPlanXml STRING into a list of resulting statements
        /// which contain both a list of all populated AllRelOpNodes as well as a list of problem AllRelOpNodes for each statement
        /// </summary>
        /// <param name="showPlanXmlString">String containing Show Plan XML</param>
        /// <returns>list of objects which each hold all relevant data for a single ShowPlanXml</returns>
        public List<ShowPlanXmlStatement> ParseShowPlanXmlString(string showPlanXmlString)
        {
            return ExecuteParseShowPlanXmlString(showPlanXmlString);
        }

        /// <summary>
        /// ParseListOfShowPlanXmlStrings parses each ShowPlanXml STRING into a list of resulting statements
        /// which contain both a list of all populated AllRelOpNodes as well as a list of problem AllRelOpNodes for each statement
        /// </summary>
        /// <param name="listofShowPlanXmlStrings">list of strings containing XML path if xml is not in project dir/name of xml if it is in the project debug dir</param>
        /// <returns>list of objects which each hold all relevant data for a single ShowPlanXml</returns>
        public List<List<ShowPlanXmlStatement>> ParseListOfShowPlanXmlStrings(List<string> listofShowPlanXmlStrings)
        {
            List<List<ShowPlanXmlStatement>> results = new List<List<ShowPlanXmlStatement>>();
            foreach (string xmlString in listofShowPlanXmlStrings)
            {
                results.Add(ExecuteParseShowPlanXmlString(xmlString));
            }
            return results;
        }

        /// <summary>
        /// Executes parse given a STRING of xml
        /// </summary>
        /// <param name="xmlString">String containing all of the Show Plan Xml</param>
        /// <returns>List of populated statement objects which each contain a list of their relOpNodes</returns>
        public List<ShowPlanXmlStatement> ExecuteParseShowPlanXmlString(string xmlString)
        {
            try
            {
                XdocAndNamespaceManager xdocAndNamespaceManager = new XdocAndNamespaceManager();
                xdocAndNamespaceManager.LoadShowPlanXmlAndAddShowPlanNamespace(xmlString, false);                
                Dictionary<string, XmlNodeList> dictionaryOfSelectedNodes = GetDictionaryOfSelectedNodes(xdocAndNamespaceManager); // Gets a list of nodes who are at the diff XPATH locs needed for vals
                return GetResultShowPlans(dictionaryOfSelectedNodes, xdocAndNamespaceManager); // Sets spnodes, set all vals for each ShowPlanXml statement node (spnode) from the xml            
            }
            catch (Exception e)
            {

                Debug.WriteLine("Exception {0} thrown by ExecuteParseShowPlanXmlString() ", e);
                return null;
            }
        }

        /// <summary>
        /// Executes parse given a FILEPATH of a ShowPlanXml file
        /// </summary>
        /// <param name="xmlFilePath">Filepath of ShowPlanXml file to be parsed</param>
        /// <returns>List of populated statement objects which each contain a list of their relOpNodes</returns>
        public List<ShowPlanXmlStatement> ExecuteParseShowPlanXmlFilePath(string xmlFilePath)
        {
            try
            {
                XdocAndNamespaceManager xdocAndNamespaceManager = new XdocAndNamespaceManager();
                xdocAndNamespaceManager.LoadShowPlanXmlAndAddShowPlanNamespace(xmlFilePath, true);
                Dictionary<string, XmlNodeList> dictionaryOfSelectedNodes = GetDictionaryOfSelectedNodes(xdocAndNamespaceManager); // Gets a list of nodes who are at the diff XPATH locs needed for vals
                return GetResultShowPlans(dictionaryOfSelectedNodes, xdocAndNamespaceManager); // Sets spnodes, set all vals for each ShowPlanXml statement node (spnode) from the xml            
            }
            catch(Exception e)
            {
            
                Debug.WriteLine("Exception {0} thrown by ExecuteParseShowPlanXmlFilePath() ", e);
                return null;
            }
        }

        /// <summary>
        /// Populates and returns a list of statement objects which contain all statement level information 
        /// as well as populates statements with relOpNodes and insights
        /// </summary>
        /// <param name="dictionaryOfSelectedNodes">Dictionary of lists of selected nodes and their corresponding labels</param>
        /// <param name="xDocNamespaceMngr">Xdoc with xml loaded and namespace manager with showplan namespace loaded</param>
        /// <returns>List of populated statement objects</returns>
        private List<ShowPlanXmlStatement> GetResultShowPlans(Dictionary<string, XmlNodeList> dictionaryOfSelectedNodes, XdocAndNamespaceManager xDocNamespaceMngr)
        {
            List<ShowPlanXmlStatement> resultShowPlans = new List<ShowPlanXmlStatement>();
            //Set statement level vals, queryHash and queryPlanHash
            XmlNodeList statementXmlNodeList = dictionaryOfSelectedNodes["StmtSimple"];
            if (statementXmlNodeList == null || statementXmlNodeList.Count.Equals(0)) { throw new ArgumentNullException(nameof(dictionaryOfSelectedNodes)); }

            //logic to allow for multiple spnodes / multiple queryhash/multiple statements per 1 xml file            
            //each tree/root/ShowPlanXmlNode will have qh (query hash) ans qph (query plan hash)
            foreach (XmlNode statements in statementXmlNodeList) //likely only to be one
            {
                foreach (XmlNode statementSimple in statements.ChildNodes)
                {
                    string queryHash = GetStatementLabel(statementSimple, "QueryHash");
                    string queryPlanHash = GetStatementLabel(statementSimple, "QueryPlanHash");
                    string cardinalityEstimationModelVersion = GetStatementLabel(statementSimple, "CardinalityEstimationModelVersion");

                    if (queryHash.Equals(NotFound) || queryPlanHash.Equals(NotFound) ) //|| cardinalityEstimationModelVersion.Equals(_notFound)
                    { throw new ArgumentNullException(nameof(dictionaryOfSelectedNodes)); }

                    ParameterManager parameterManager = new ParameterManager();
                    List<Parameter> parametersForStatement = parameterManager.GetParametersForStatement(statementSimple, xDocNamespaceMngr.Nsmgr); // Get all parameters from <ParameterList> 
                    parametersForStatement = parameterManager.GetParametersWithProblemNodeIds(statementSimple, parametersForStatement, xDocNamespaceMngr.Nsmgr); // Get all node ids and associate them with their parameter refs

                    ShowPlanXmlStatement statementNode = new ShowPlanXmlStatement(cardinalityEstimationModelVersion, queryHash, queryPlanHash, parametersForStatement);
                    statementNode = AddRelOpNodesToShowplan(dictionaryOfSelectedNodes, statementNode, xDocNamespaceMngr, parametersForStatement);
                    if(statementNode == null)
                    { throw new ArgumentNullException(nameof(statementNode)); }
                    statementNode = SetRootCausesForProblemNodes(statementNode);        
                    resultShowPlans.Add(statementNode);
                }
            }
            return resultShowPlans;
        }
    }
}
