//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.ExecPlanGraph
{
    /// <summary>
    /// Interface represents ability to split an data source containing multiple
    /// batches / statement into statements and return an XML containing a single statement.
    /// This is used for XML ShowPlan saving.
    /// </summary>
    public interface IXmlBatchParser
	{
        /// <summary>
        /// Builds one or more Graphs that
        /// represnet data from the data source.
        /// </summary>
        /// <param name="dataSource">Data Source.</param>
        /// <returns>An array of AnalysisServices Graph objects.</returns>
		string GetSingleStatementXml(object dataSource, int statementIndex);

        /// <summary>
        /// Returns statements block type object
        /// </summary>
        /// <param name="dataSource">Data source</param>
        /// <param name="statementIndex">Statement index in the data source</param>
        /// <returns>Statement block type object</returns>
        StmtBlockType GetSingleStatementObject(object dataSource, int statementIndex);
	}
}
