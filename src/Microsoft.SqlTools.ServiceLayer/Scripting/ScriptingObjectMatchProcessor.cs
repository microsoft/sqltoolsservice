//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlTools.ServiceLayer.Scripting.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Scripting
{
    /// <summary>
    /// Implementes the matchin logic to filter scripting objects based on
    /// an include/exclude criteria.
    /// </summary>
    /// <remarks>
    /// First, objects are included by the include filter.  Then, objects are removed by
    /// the exclude filter.  Matches are made by comparing case insensitive strings.  
    /// Wildcard '*' are supported for all scripting object fields.
    /// </remarks>
    public static class ScriptingObjectMatchProcessor
    {
        /// <summary>
        /// Given a collection of candidate scripting objects, filters the items that match 
        /// based on the passed include and exclude criteria.
        /// </summary>
        /// <param name="includeCriteria">The include object criteria.</param>
        /// <param name="excludeCriteria">The exclude object criteria.</param>
        /// <param name="candidates">The candidate object to filter.</param>
        /// <returns>The matching scripting objects.</returns>
        public static IEnumerable<ScriptingObject> Match(
            ScriptingObject includeCriteria,
            ScriptingObject excludeCriteria,
            IEnumerable<ScriptingObject> candidates)
        {
            return Match(
                includeCriteria == null ? new ScriptingObject[0] : new[] { includeCriteria },
                excludeCriteria == null ? new ScriptingObject[0] : new[] { excludeCriteria },
                candidates);
        }

        /// <summary>
        /// Given a collection of candidate scripting objects, filters the items that match 
        /// based on the passed include and exclude criteria.
        /// </summary>
        /// <param name="includeCriteria">The collection of include object criteria items.</param>
        /// <param name="excludeCriteria">The collection of exclude object criteria items.</param>
        /// <param name="candidates">The candidate object to filter.</param>
        /// <returns>The matching scripting objects.</returns>
        public static IEnumerable<ScriptingObject> Match(
            IEnumerable<ScriptingObject> includeCriteria,
            IEnumerable<ScriptingObject> excludeCriteria,
            IEnumerable<ScriptingObject> candidates)
        {
            if (candidates == null)
            {
                throw new ArgumentNullException("candidates");
            }

            IEnumerable<ScriptingObject> matchedObjects = new List<ScriptingObject>();

            if (includeCriteria != null && includeCriteria.Count() > 0)
            {
                foreach (ScriptingObject scriptingObjectCriteria in includeCriteria)
                {
                    IEnumerable<ScriptingObject> matches = MatchCriteria(scriptingObjectCriteria, candidates);
                    matchedObjects = matchedObjects.Union(matches);
                }
            }
            else
            {
                matchedObjects = candidates;
            }

            if (excludeCriteria != null)
            {
                foreach (ScriptingObject scriptingObjectCriteria in excludeCriteria)
                {
                    IEnumerable<ScriptingObject> matches = MatchCriteria(scriptingObjectCriteria, candidates);
                    matchedObjects = matchedObjects.Except(matches);
                }
            }

            return matchedObjects;
        }

        private static IEnumerable<ScriptingObject> MatchCriteria(ScriptingObject criteria, IEnumerable<ScriptingObject> databaseObjects)
        {
            IEnumerable<ScriptingObject> matchedObjects = databaseObjects;

            if (!string.IsNullOrWhiteSpace(criteria.Type))
            {
                matchedObjects = matchedObjects.Where(o => string.Equals(criteria.Type, o.Type, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(criteria.Schema))
            {
                if (criteria.Schema.Equals("*", StringComparison.OrdinalIgnoreCase))
                {
                    // Don't filter any objects
                }
                if (criteria.Schema.EndsWith("*", StringComparison.OrdinalIgnoreCase))
                {
                    matchedObjects = matchedObjects.Where(o => o.Schema.StartsWith(
                        criteria.Schema.Substring(0, criteria.Schema.Length - 1), 
                        StringComparison.CurrentCultureIgnoreCase));
                }
                else
                {
                    matchedObjects = matchedObjects.Where(o => string.Equals(criteria.Schema, o.Schema, StringComparison.OrdinalIgnoreCase));
                }
            }

            if (!string.IsNullOrWhiteSpace(criteria.Name))
            {
                if (criteria.Name.Equals("*", StringComparison.OrdinalIgnoreCase))
                {
                    // Don't filter any objects
                }
                else if (criteria.Name.EndsWith("*", StringComparison.OrdinalIgnoreCase))
                {
                    matchedObjects = matchedObjects.Where(o => o.Name.StartsWith(
                        criteria.Name.Substring(0, criteria.Name.Length - 1), 
                        StringComparison.CurrentCultureIgnoreCase));
                }
                else
                {
                    matchedObjects = matchedObjects.Where(o => string.Equals(criteria.Name, o.Name, StringComparison.OrdinalIgnoreCase));
                }
            }

            return matchedObjects;
        }
    }
}
