//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.SqlTools.SqlCore.QueryDataStore.Common
{
    /// <summary>
    /// List of Wait Category enumeration which we support in QDS
    /// </summary>
    public enum WaitCategory
    {
        [WaitCategoryName("Unknown")]
        Unknown = 0,
        [WaitCategoryName("CPU")]
        CPU,
        [WaitCategoryName("Worker Thread")]
        Worker_Thread,
        [WaitCategoryName("Lock")]
        Lock,
        [WaitCategoryName("Latch")]
        Latch,
        [WaitCategoryName("Buffer Latch")]
        Buffer_Latch,
        [WaitCategoryName("Buffer IO")]
        Buffer_IO,
        [WaitCategoryName("Compilation")]
        Compilation,
        [WaitCategoryName("SQL CLR")]
        SQL_CLR,
        [WaitCategoryName("Mirroring")]
        Mirroring,
        [WaitCategoryName("Transaction")]
        Transaction,
        [WaitCategoryName("Idle")]
        Idle,
        [WaitCategoryName("Preemptive")]
        Preemptive,
        [WaitCategoryName("Service Broker")]
        Service_Broker,
        [WaitCategoryName("Tran Log IO")]
        Tran_Log_IO,
        [WaitCategoryName("Network IO")]
        Network_IO,
        [WaitCategoryName("Parallelism")]
        Parallelism,
        [WaitCategoryName("Memory")]
        Memory,
        [WaitCategoryName("User Wait")]
        User_Wait,
        [WaitCategoryName("Tracing")]
        Tracing,
        [WaitCategoryName("Full Text Search")]
        Full_Text_Search,
        [WaitCategoryName("Other Disk IO")]
        Other_Disk_IO,
        [WaitCategoryName("Replication")]
        Replication,
        [WaitCategoryName("Log Rate Governor")]
        Log_Rate_Governor,
    }
}
