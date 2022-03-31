//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Text;
using System.Xml.Serialization;
using System.Reflection;

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.ShowPlan
{
    /// <summary>
    /// Builds hierarchy of Graph objects from ShowPlan XML
    /// </summary>
	internal sealed class XmlPlanNodeBuilder : INodeBuilder, IXmlBatchParser
    {
        #region Constructor

        public XmlPlanNodeBuilder(ShowPlanType showPlanType)
        {
            this.showPlanType = showPlanType;
        }

        #endregion

        #region INodeBuilder

        /// <summary>
        /// Builds one or more Graphs that
        /// represnet data from the data source.
        /// </summary>
        /// <param name="dataSource">Data Source.</param>
        /// <returns>An array of AnalysisServices Graph objects.</returns>
        public ShowPlanGraph[] Execute(object dataSource)
        {
            ShowPlanXML plan = dataSource as ShowPlanXML;
            if (plan == null)
            {
                plan = ReadXmlShowPlan(dataSource);
            }
            List<ShowPlanGraph> graphs = new List<ShowPlanGraph>();

            int statementIndex = 0;
            foreach (BaseStmtInfoType statement in EnumStatements(plan))
            {
                // Reset currentNodeId (used through Context) and create new context
                this.currentNodeId = 0;
                NodeBuilderContext context = new NodeBuilderContext(new ShowPlanGraph(), this.showPlanType, this);
                // Parse the statement block
                XmlPlanParser.Parse(statement, null, null, context);
                // Get the statement XML for the graph.
                context.Graph.XmlDocument = GetSingleStatementXml(dataSource, statementIndex);
                // Parse the graph description.
                context.Graph.Description = ParseDescription(context.Graph);
                // Add graph to the list
                graphs.Add(context.Graph);
                // Incrementing statement index
                statementIndex++;
            }

            return graphs.ToArray();
        }

        #endregion

        #region IXmlBatchParser

        /// <summary>
        /// Returns an XML string for a specific ShowPlan statement.
        /// This is used to save a plan corresponding to a particular graph control.
        /// </summary>
        /// <param name="dataSource">Data source that contains the full plan.</param>
        /// <param name="statementIndex">Statement index.</param>
        /// <returns>XML string that contains execution plan for the specified statement index.</returns>
        public string GetSingleStatementXml(object dataSource, int statementIndex)
        {
            StmtBlockType newStatementBlock = GetSingleStatementObject(dataSource, statementIndex);

            // Now make the new plan based on the existing one that contains only one statement.
            ShowPlanXML plan = ReadXmlShowPlan(dataSource);
            plan.BatchSequence = new StmtBlockType[][]
            {
                new StmtBlockType[] { newStatementBlock }
            };

            // Serialize the new plan.
            StringBuilder stringBuilder = new StringBuilder();
            Serializer.Serialize(new StringWriter(stringBuilder), plan);

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Returns single statement block type object
        /// </summary>
        /// <param name="dataSource">Data source</param>
        /// <param name="statementIndex">Statement index in the data source</param>
        /// <returns>Single statement block type object</returns>
        public StmtBlockType GetSingleStatementObject(object dataSource, int statementIndex)
        {
            // First read the whole plan from the data source
            ShowPlanXML plan = ReadXmlShowPlan(dataSource);

            int index = 0;
            StmtBlockType newStatementBlock = new StmtBlockType();

            // Locate the statement for the specified index
            foreach (BaseStmtInfoType statement in EnumStatements(plan))
            {
                if (statementIndex == index++)
                {
                    // This is the statement we are looking for
                    newStatementBlock.Items = new BaseStmtInfoType[] { statement };
                    break;
                }
            }

            if (newStatementBlock.Items == null)
            {
                throw new ArgumentOutOfRangeException("statementIndex");
            }

            return newStatementBlock;
        }

        #endregion

        #region Internal properties

        /// <summary>
        /// Gets current node Id and internally increments the Id.
        /// </summary>
        /// <returns>ID.</returns>
        internal int GetCurrentNodeId()
        {
            return ++currentNodeId;
        }

        #endregion


        #region Implementation details

        /// <summary>
        /// Deserializes XML ShowPlan from the data source
        /// </summary>
        /// <param name="dataSource">Data Source</param>
        /// <returns>ShowPlanXML object which is the root of deserialized plan.</returns>
        private ShowPlanXML ReadXmlShowPlan(object dataSource)
        {
            ShowPlanXML result = null;

            string stringData = dataSource as string;
            if (stringData != null)
            {
                using (StringReader reader = new StringReader(stringData))
                {
                    result = Serializer.Deserialize(reader) as ShowPlanXML;
                }
            }
            else
            {
                byte[] binaryData = dataSource as byte[];
                if (binaryData != null)
                {
                    using (MemoryStream stream = new MemoryStream(binaryData))
                    {
                        // We need to use reflection to obtain private method of XmlReader class
                        // that can create a binary reader. Public XmlReader.Create does not 
                        // support this.
                        MethodInfo createSqlReaderMethodInfo = typeof(System.Xml.XmlReader).GetMethod("CreateSqlReader", BindingFlags.Static | BindingFlags.NonPublic);
                        object[] args = new object[3] { stream, null, null };

                        using (XmlReader reader = (XmlReader)createSqlReaderMethodInfo.Invoke(null, args))
                        {
                            result = Serializer.Deserialize(reader) as ShowPlanXML;
                        }
                    }
                }
            }

            if (null == result)
            {
                Debug.Assert(false, "Unexpected ShowPlan source = " + dataSource.GetType().ToString());
                throw new ArgumentException(SR.Keys.UnknownShowPlanSource);
            }

            return result;
        }

        /// <summary>
        /// Enumerates statements in XML ShowPlan. This also looks inside each statement and
        /// enumerates sub-statements found in FunctionType blocks.
        /// </summary>
        /// <param name="plan">XML ShowPlan.</param>
        /// <returns>Statements enumerator.</returns>
        private IEnumerable<BaseStmtInfoType> EnumStatements(ShowPlanXML plan)
        {
            foreach (StmtBlockType[] statementBatch in plan.BatchSequence)
            {
                foreach (StmtBlockType statementBlock in statementBatch)
                {
                    ExtractFunctions(statementBlock);

                    // flatten out any statements contained within then / else clauses to make it appear as though all code paths are
                    // executed sequentially, this is useful for the Live show plan case because it only displays a single show-plan instance at any given time.
                    if (showPlanType == ShowPlanType.Live)
                    {
                        FlattenConditionClauses(statementBlock);
                    }

                    foreach (BaseStmtInfoType statement in EnumStatements(statementBlock))
                    {
                        yield return statement;
                    }
                }
            }
        }

        /// <summary>
        /// We do some special handling of the showplan graphs to flatten out control nodes.  See VSTS 3657984.
        /// Essentially the problem is that the Actual showplan and the predicted show plan are treated differently.
        /// The predicted show plan shows the control node (while, if-then-else) when the actual show plans only contain a single
        /// plan per statement.  This difference makes it difficult to match up the running query against the predicted showplan.  Further
        /// complicating the situation is that each statement may re-use nodeIDs which violates a fundamental assumption
        /// of the LQS tool and the progress estimators.  We can work-around this by flattening out the predicted show plan graph
        /// to look as a series of statements without the control structures or nesting
        /// </summary>
        private void FlattenConditionClauses(StmtBlockType statementBlock)
        {
            if (statementBlock != null && statementBlock.Items != null)
            {
                ArrayList targetStatementList = new ArrayList();

                foreach (BaseStmtInfoType statement in statementBlock.Items)
                {
                    targetStatementList.Add(statement);

                    FlattenConditionClauses(statement, targetStatementList);
                }

                // Make a new Items array for the statement block by combining existing items and 
                // new wrapper statements
                statementBlock.Items = new BaseStmtInfoType[targetStatementList.Count];
                targetStatementList.CopyTo(statementBlock.Items);
            }
        }

        private void FlattenConditionClauses(BaseStmtInfoType statement, ArrayList targetStatementList)
        {
            // Enum statement children and genetate wrapper statements for them
            XmlPlanParser parser = XmlPlanParserFactory.GetParser(statement.GetType());
            foreach (object child in parser.GetChildren(statement))
            {
                StmtCondTypeThen stmtThen = child as StmtCondTypeThen;
                if (stmtThen != null)
                {
                    //add this element and its children
                    if (stmtThen.Statements != null && stmtThen.Statements.Items != null)
                    {
                        foreach (BaseStmtInfoType subStatement in stmtThen.Statements.Items)
                        {
                            targetStatementList.Add(subStatement);
                            FlattenConditionClauses(subStatement, targetStatementList);
                        }
                    }
                }

                else
                {
                    StmtCondTypeElse stmtElse = child as StmtCondTypeElse;
                    if (stmtElse != null)
                    {
                        //add this element and its children
                        if (stmtElse.Statements != null && stmtElse.Statements.Items != null)
                        {
                            foreach (BaseStmtInfoType subStatement in stmtElse.Statements.Items)
                            {
                                targetStatementList.Add(subStatement);
                                FlattenConditionClauses(subStatement, targetStatementList);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Extracts UDF and StoredProc items and places each of them at the top level
        /// wrapping each of them with an empty statement.
        /// </summary>
        /// <param name="statementBlock">Statement block</param>
        private void ExtractFunctions(StmtBlockType statementBlock)
        {
            if (statementBlock != null && statementBlock.Items != null)
            {
                ArrayList targetStatementList = new ArrayList();

                foreach (BaseStmtInfoType statement in statementBlock.Items)
                {
                    targetStatementList.Add(statement);

                    ExtractFunctions(statement, targetStatementList);
                }

                // Make a new Items array for the statement block by combining existing items and 
                // new wrapper statements
                statementBlock.Items = new BaseStmtInfoType[targetStatementList.Count];
                targetStatementList.CopyTo(statementBlock.Items);
            }
        }

        /// <summary>
        /// Extracts UDF and StoredProc items from a statement and adds them to a target list.
        /// </summary>
        /// <param name="statement">Statement.</param>
        /// <param name="targetStatementList">Target list to add a newly generated statement to.</param>
        private void ExtractFunctions(BaseStmtInfoType statement, ArrayList targetStatementList)
        {
            // Enum FunctionType objects and generate wrapper statements for them
            XmlPlanParser parser = XmlPlanParserFactory.GetParser(statement.GetType());
            foreach (FunctionTypeItem functionItem in parser.ExtractFunctions(statement))
            {
                StmtSimpleType subStatement = null;

                if (functionItem.Type == FunctionTypeItem.ItemType.StoredProcedure)
                {
                    subStatement = new StmtSimpleType();
                    subStatement.StoredProc = functionItem.Function;
                }
                else if (functionItem.Type == FunctionTypeItem.ItemType.Udf)
                {
                    subStatement = new StmtSimpleType();
                    subStatement.UDF = new FunctionType[] { functionItem.Function };
                }
                else
                {
                    Debug.Assert(false, "Ivalid function type");
                }

                if (subStatement != null)
                {
                    targetStatementList.Add(subStatement);

                    // Call itself recursively.
                    if (functionItem.Function.Statements != null && functionItem.Function.Statements.Items != null)
                    {
                        foreach (BaseStmtInfoType functionStatement in functionItem.Function.Statements.Items)
                        {
                            ExtractFunctions(functionStatement, targetStatementList);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Recursively enumerates statements in StmtBlockType.
        /// </summary>
        /// <param name="statementBlock">Statement block (may contain multiple statements).</param>
        /// <returns>Statement enumerator.</returns>
        private IEnumerable<BaseStmtInfoType> EnumStatements(StmtBlockType statementBlock)
        {
            if (statementBlock != null && statementBlock.Items != null)
            {
                foreach (BaseStmtInfoType statement in statementBlock.Items)
                {
                    yield return statement;
                }
            }
        }
        private Description ParseDescription(ShowPlanGraph graph)
        {
            XmlDocument stmtXmlDocument = new XmlDocument();
            stmtXmlDocument.LoadXml(graph.XmlDocument);
            var nsMgr = new XmlNamespaceManager(stmtXmlDocument.NameTable);
            //Manually add our showplan namespace since the document won't have it in the default NameTable
            nsMgr.AddNamespace("shp", "http://schemas.microsoft.com/sqlserver/2004/07/showplan");

            //The root node in this case is the statement node
            XmlNode rootNode = stmtXmlDocument.DocumentElement;
            if(rootNode == null)
            {
                //Couldn't find our statement node, this should never happen in a properly formed document
                throw new ArgumentNullException("StatementNode");
            }

            XmlNode missingIndexes = rootNode.SelectSingleNode("descendant::shp:MissingIndexes", nsMgr);

            List<MissingIndex> parsedIndexes = new List<MissingIndex>();

            // Not all plans will have a missing index. For those plans, just return the description.
            if (missingIndexes != null)
            {

                // check Memory Optimized table.
                bool memoryOptimzed = false;
                XmlNode scan = rootNode.SelectSingleNode("descendant::shp:IndexScan", nsMgr);
                if (scan == null)
                {
                    scan = rootNode.SelectSingleNode("descendant::shp:TableScan", nsMgr);
                }
                if (scan != null && scan.Attributes["Storage"] != null)
                {
                    if (0 == string.Compare(scan.Attributes["Storage"].Value, "MemoryOptimized", StringComparison.Ordinal))
                    {
                        memoryOptimzed = true;
                    }
                }

                // getting all the indexgroups from the plan. A plan can have multiple missing index groups.
                XmlNodeList indexGroups = missingIndexes.SelectNodes("descendant::shp:MissingIndexGroup", nsMgr);

                // missing index template
                const string createIndexTemplate = "CREATE NONCLUSTERED INDEX [<Name of Missing Index, sysname,>]\r\nON {0}.{1} ({2})\r\n";
                const string addIndexTemplate = "ALTER TABLE {0}.{1}\r\nADD INDEX [<Name of Missing Index, sysname,>]\r\nNONCLUSTERED ({2})\r\n";
                const string includeTemplate = "INCLUDE ({0})";

                // iterating over all missing index groups
                foreach (XmlNode indexGroup in indexGroups)
                {
                    // we only have one missing index per index group 
                    XmlNode missingIndex = indexGroup.SelectSingleNode("descendant::shp:MissingIndex", nsMgr);

                    string database = missingIndex.Attributes["Database"].Value;
                    string schemaName = missingIndex.Attributes["Schema"].Value;
                    string tableName = missingIndex.Attributes["Table"].Value;
                    string indexColumns = string.Empty;
                    string includeColumns = string.Empty;

                    // populate index columns and include columns
                    XmlNodeList columnGroups = missingIndex.SelectNodes("shp:ColumnGroup", nsMgr);
                    foreach (XmlNode columnGroup in columnGroups)
                    {
                        foreach (XmlNode column in columnGroup.ChildNodes)
                        {
                            string columnName = column.Attributes["Name"].Value;
                            if (0 != string.Compare(columnGroup.Attributes["Usage"].Value, "INCLUDE", StringComparison.Ordinal))
                            {
                                if (indexColumns == string.Empty)
                                    indexColumns = columnName;
                                else
                                    indexColumns = $"{indexColumns},{columnName}";
                            }
                            else if (!memoryOptimzed)
                            {
                                if (includeColumns == string.Empty)
                                    includeColumns = columnName;
                                else
                                    includeColumns = $"{indexColumns},{columnName}";
                            }
                        }
                    }

                    // for memory optimized we just alter the existing index where as for non optimized tables we create a new one.
                    string queryText = string.Format((memoryOptimzed) ? addIndexTemplate : createIndexTemplate, schemaName, tableName, indexColumns);
                    if (!string.IsNullOrEmpty(includeColumns))
                    {
                        queryText += string.Format(includeTemplate, includeColumns);
                    }

                    string impact = indexGroup.Attributes["Impact"].Value;
                    string caption = SR.MissingIndexFormat(impact, queryText);
                    parsedIndexes.Add(new MissingIndex()
                    {
                        MissingIndexDatabase = database,
                        MissingIndexQueryText = queryText,
                        MissingIndexImpact = impact,
                        MissingIndexCaption = caption
                    });
                }
            }


            Description description = new Description
            {
                QueryText = graph.Statement,
                MissingIndices = parsedIndexes,
            };
            return description;
        }

        #endregion

        #region Private members

        private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(ShowPlanXML));

        private ShowPlanType showPlanType;
        private int currentNodeId;

        #endregion
    }
}
