//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode;
using System.Data.SqlClient;


namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.TSQLExecutionEngine
{
    internal class BatchEventHandler: IBatchEventsHandler
    {
        List<int> resultCounts = new List<int>();
        List<string> sqlMessages = new List<string>();
        List<string> errorMessage = new List<string>();
        int batchfinishedEventCounter = 0;
        SqlDataReader dr = null;
        bool cancelEventFired = false;

        #region Public properties
        public List<int> ResultCounts
        {
            get
            {
                return resultCounts;
            }
        }

        public List<string> SqlMessages
        {
            get
            {
                return sqlMessages;
            }
        }

        public List<string> ErrorMessages
        {
            get
            {
                return errorMessage;
            }
        }

        public int BatchfinishedEventCounter
        {
            get
            {
                return batchfinishedEventCounter;
            }
        }
        public bool CancelFired
        {
            get
            {
                return cancelEventFired;
            }
        }
        #endregion

        #region IBatchEventHandlers Members
        public void OnBatchCancelling(object sender, EventArgs args)
        {
            Console.WriteLine("\tOnBatchCancelling:");
            cancelEventFired = true;
        }

        public void OnBatchError(object sender, BatchErrorEventArgs args)
        {
            Console.WriteLine("\tOnBatchError:");
            Console.WriteLine("\t\tLine {0} has error: ", args.Line);
            Console.WriteLine("\t\tError description: " + args.Description);
            Console.WriteLine("\t\tError message: " + args.Message);
            Console.WriteLine("\t\tError Line: " + args.TextSpan.iStartLine);

            errorMessage.Add(args.Description);
        }

        public void OnBatchMessage(object sender, BatchMessageEventArgs args)
        {
            Console.WriteLine("\tOnBatchMessage ...");
            Console.WriteLine("\t\tMessage: " + args.Message);
            Console.WriteLine("\t\tDetail message:" + args.DetailedMessage);

            if (args.DetailedMessage != "")
            {
                sqlMessages.Add(args.DetailedMessage);
            }
            else
            {
                SqlMessages.Add(null);
            }
        }

        public void OnBatchResultSetFinished(object sender, EventArgs args)
        {
            Console.WriteLine("\tOnBatchResultSetFinished...");
            Console.WriteLine("\t\tBatch result set finished");
            batchfinishedEventCounter++;
        }

        public void OnBatchResultSetProcessing(object sender, BatchResultSetEventArgs args)
        {                 
            lock (this)
            { 
                Console.WriteLine("\tOnBatchResultProcessing...");
                dr = args.DataReader as SqlDataReader;
                int count = 0;
                while (dr.Read() && !cancelEventFired)
                {
                    count++;
                }
                Console.WriteLine("\t\tOnBatchResultProcessing: Records returned: " + count);
                resultCounts.Add(count);
            }
        }

        #endregion
    }
}
