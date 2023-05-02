//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
#region Using directives

using System.Collections;
using System.Globalization;

using Microsoft.SqlServer.Management.Smo;

#endregion

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
	/// <summary>
	/// String comparer that uses the case sensitivity and other settings
	/// from a particular SQL collation
	/// </summary>
#if DEBUG || EXPOSE_MANAGED_INTERNALS
    public
#else
    internal
#endif
    class SqlCollationSensitiveStringComparer : IComparer
	{
		private CompareOptions compareOptions;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="sqlCollation">The name of the SQL collation, like ALGERIAN_CI_AI</param>
		public SqlCollationSensitiveStringComparer(string sqlCollation)
		{
			if (sqlCollation != null && sqlCollation.Length != 0)
			{
				this.compareOptions = SqlSupport.GetCompareOptionsFromCollation(sqlCollation);
			}
			else
			{
				this.compareOptions = CompareOptions.Ordinal;
			}						
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="compareOptions">The CompareOptions for the SQL collation</param>
		public SqlCollationSensitiveStringComparer(CompareOptions compareOptions)
		{
			this.compareOptions = compareOptions;
		}

		/// <summary>
		/// Compare two strings
		/// </summary>
		/// <param name="x">The first string to compare</param>
		/// <param name="y">The second string to compare</param>
		/// <returns>Less than zero if x is less than y, 0 if x equals y, greater than zero if x is greater than y</returns>
		public int Compare(object x, object y)
		{
			if (null == x && null == y)
			{
				return 0;
			}
			else if (null != x && null == y)
			{
				return 1;
			}
			else if (null == x && null != y)
			{
				return -1;
			}
			else
			{
				return CultureInfo.InvariantCulture.CompareInfo.Compare((string) x, (string) y, compareOptions);
			}
		}
	}
}
