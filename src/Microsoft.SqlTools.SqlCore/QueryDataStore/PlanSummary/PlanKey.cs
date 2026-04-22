//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using Microsoft.SqlServer.Management.QueryStoreModel.Common;

namespace Microsoft.SqlServer.Management.QueryStoreModel.PlanSummary
{
    /// <summary>
    /// Plan Key used to uniquely identify query plans based on the Plan ID and ExecutionType
    /// </summary>
    public struct PlanKey : IEquatable<PlanKey>, IComparable
    {
        /// <summary>
        /// The plan id.
        /// </summary>
        public long PlanId;

        /// <summary>
        /// The execution type.
        /// </summary>
        public PlanExecutionType ExecutionType;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlanKey"/> struct. 
        /// </summary>
        /// <param name="planId">Plan ID</param>
        /// <param name="executionType">Execution Type</param>
        public PlanKey(long planId, PlanExecutionType executionType)
        {
            this.PlanId = planId;
            this.ExecutionType = executionType;
        }

        /// <summary>
        /// Two PlanKeys are considered equal if they have the same PlanID and ExecutionType
        /// </summary>
        /// <param name="other">The PlanKey to compare to</param>
        /// <returns>True if they are equal, false otherwise</returns>
        public bool Equals(PlanKey other) => this.PlanId == other.PlanId && this.ExecutionType.Equals(other.ExecutionType);

        /// <summary>
        /// Override GetHashCode to generate new hash based on PlanId and ExecutionType
        /// </summary>
        /// <returns>New hash code based on PlanId and ExecutionType</returns>
        public override int GetHashCode() => new { this.PlanId, this.ExecutionType }.GetHashCode();

        /// <summary>
        /// Override the Object level Equals to use PlanKey's Equals
        /// </summary>
        /// <param name="obj">obj of type PlanKey</param>
        /// <returns>True if they are equal, false otherwise</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is PlanKey))
            {
                return false;
            }

            PlanKey pk = (PlanKey)obj;

            return this.Equals(pk);
        }

        /// <summary>
        /// Implementing CompareTo to be used in collection dictionary.
        /// </summary>
        /// <param name="obj">The PlanKey to compare to</param>
        /// <returns>0 if both PlanId and ExecutionTypes are equal</returns>
        public int CompareTo(object obj)
        {
            if (obj is PlanKey)
            {
                PlanKey pk = (PlanKey)obj;
                int result = this.PlanId.CompareTo(pk.PlanId);
                if (result == 0)
                {
                    result = this.ExecutionType.CompareTo(pk.ExecutionType);
                }

                return result;
            }
            else
            {
                throw new ArgumentException("Obj is not of type PlanKey");
            }
        }

        /// <summary>
        /// Clear the state of the PlanKey by setting the variables to Invalid values.
        /// </summary>
        public void Clear()
        {
            this.PlanId = QueryStoreConstants.InvalidPlanId;
            this.ExecutionType = PlanExecutionType.Invalid;
        }

        /// <summary>
        /// Check whether the plan key is empty or not by checking for invalid values.
        /// If either one of the plan ID or execution type is invalid, we consider the whole plan key to be invalid.
        /// </summary>
        /// <returns>True if PlanKey contains invalid values, False otherwise</returns>
        public bool IsEmpty() => this.PlanId == QueryStoreConstants.InvalidPlanId || this.ExecutionType == PlanExecutionType.Invalid;
    }
}
