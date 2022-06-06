//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Migration.Assessment.Common.Contracts.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Migration.Contracts
{
    public class SaveAssessmentResultParams
    {
        /// <summary>
        /// Contains the raw assessment
        /// </summary>
        public ISqlMigrationAssessmentModel AssessmentResult { get; set; }
    }

    public class SaveAssessmentResult
    {
        /// <summary>
        /// Full file name where assessment result is saved
        /// </summary>
        public string AssessmentReportFileName { get; set; }

    }

    public class SaveAssessmentResultRequest
    {
        public static readonly
            RequestType<SaveAssessmentResultParams, SaveAssessmentResult> Type =
                RequestType<SaveAssessmentResultParams, SaveAssessmentResult>.Create("migration/saveassessment");
    }
}
