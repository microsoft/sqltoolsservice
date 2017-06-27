//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlTools.ServiceLayer.Scripting.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Scripting
{
    /// <summary>
    /// Implements matching logic to filter scripting objects based on an 
    /// include/exclude criteria.
    /// </summary>
    /// <remarks>
    /// First, objects are included by the include filter.  Then, objects are removed by
    /// the exclude filter.  Matches are made by comparing case insensitive strings for the 
    /// ScriptingObject Type, Schema, and Name properties.  Wildcards '*' are supported for 
    /// the ScriptingObject Schema and Name properties.  Matching on ScriptingObject Type 
    /// property must be an exact match.
    /// 
    /// Examples:
    ///     
    /// Include ScriptingObject { Type = null, Schema = "dbo", Name = null } 
    /// -> matches all objects in the dbo schema.
    ///     
    /// Include ScriptingObject { Type = "Table", Schema = "dbo", Name = null } 
    /// -> matches all tables in the dbo schema.
    /// 
    /// Include ScriptingObject { Type = "Table", Schema = null, Name = "Emp*" } 
    /// -> matches all table names that start with "Emp"
    ///
    /// Include ScriptingObject { Type = "View", Schema = null, Name = "Emp*" } 
    /// Include ScriptingObject { Type = "Table", Schema = null, Name = "Emp*" } 
    /// -> matches all table and views with names that start with "Emp"
    ///
    /// Include ScriptingObject { Type = "Table", Schema = null, Name = null }
    /// Exclude ScriptingObject { Type = null, Schema = "HumanResources", Name = null } 
    /// -> matches all tables except tables in the "HumanResources" schema
    ///
    /// </remarks>
    public static class ScriptingObjectMatcher
    {
        private const string Wildcard = "*";

        /// <summary>
        /// Given a collection of candidate scripting objects, filters the items that match 
        /// based on the passed include and exclude criteria.
        /// </summary>
        /// <param name="includeCriteria">The include object criteria.</param>
        /// <param name="excludeCriteria">The exclude object criteria.</param>
        /// <param name="includeSchemas">The include schema filter.</param>
        /// <param name="excludeSchemas">The exclude schema filter.</param>
        /// <param name="includeTypes">The include type filter.</param>
        /// <param name="excludeTypes">The exclude type filter.</param>
        /// <param name="candidates">The candidate object to filter.</param>
        /// <returns>The matching scripting objects.</returns>
        public static IEnumerable<ScriptingObject> Match(
            ScriptingObject includeCriteria,
            ScriptingObject excludeCriteria,
            string includeSchemas,
            string excludeSchemas,
            string includeTypes,
            string excludeTypes,
            IEnumerable<ScriptingObject> candidates)
        {
            return Match(
                includeCriteria == null ? new ScriptingObject[0] : new[] { includeCriteria },
                excludeCriteria == null ? new ScriptingObject[0] : new[] { excludeCriteria },
                includeSchemas == null ? new List<string>(): new List<string> { includeSchemas },
                excludeSchemas == null ? new List<string>(): new List<string> { excludeSchemas },
                includeTypes == null ? new List<string>(): new List<string> { includeTypes },
                excludeTypes == null ? new List<string>(): new List<string> { excludeTypes },
                candidates);
        }

        /// <summary>
        /// Given a collection of candidate scripting objects, filters the items that match 
        /// based on the passed include and exclude criteria.
        /// </summary>
        /// <param name="includeCriteria">The collection of include object criteria items.</param>
        /// <param name="excludeCriteria">The collection of exclude object criteria items.</param>
        /// <param name="includeSchemas">The collection of include schema items.</param>
        /// <param name="excludeSchemas">The collection of exclude schema items.</param>
        /// <param name="includeTypes">The collection of include type items.</param>
        /// <param name="excludeTypes">The collection of exclude type items.</param>
        /// <param name="candidates">The candidate object to filter.</param>
        /// <returns>The matching scripting objects.</returns>
        public static IEnumerable<ScriptingObject> Match(
            IEnumerable<ScriptingObject> includeCriteria,
            IEnumerable<ScriptingObject> excludeCriteria,
            IEnumerable<string> includeSchemas,
            IEnumerable<string> excludeSchemas,
            IEnumerable<string> includeTypes,
            IEnumerable<string> excludeTypes,
            IEnumerable<ScriptingObject> candidates)
        {
            Validate.IsNotNull("candidates", candidates);
            
            IEnumerable<ScriptingObject> matchedObjects = new List<ScriptingObject>();

            if (includeCriteria != null && includeCriteria.Any())
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

            if (excludeCriteria != null && excludeCriteria.Any())
            {
                foreach (ScriptingObject scriptingObjectCriteria in excludeCriteria)
                {
                    IEnumerable<ScriptingObject> matches = MatchCriteria(scriptingObjectCriteria, matchedObjects);
                    matchedObjects = matchedObjects.Except(matches);
                }
            }

            // Apply additional filters if included.
            matchedObjects = ExcludeSchemaAndOrType(excludeSchemas, excludeTypes, matchedObjects);
            matchedObjects = IncludeSchemaAndOrType(includeSchemas, includeTypes, matchedObjects);

            return matchedObjects;
        }

        private static IEnumerable<ScriptingObject> ExcludeSchemaAndOrType(IEnumerable<string> excludeSchemas, IEnumerable<string> excludeTypes, 
            IEnumerable<ScriptingObject> candidates)
        {
            // Given a list of candidates, we remove any objects that match the excluded schema and/or type.
            IEnumerable<ScriptingObject> remainingObjects = candidates;
            IEnumerable<ScriptingObject> matches = null;

            if (excludeSchemas != null && excludeSchemas.Any())
            {            
                foreach (string exclude_schema in excludeSchemas)
                {
                    matches = MatchCriteria(exclude_schema, (candidate) => { return candidate.Schema; }, candidates);
                    remainingObjects = remainingObjects.Except(matches);
                }
            }

            if (excludeTypes != null && excludeTypes.Any())
            {
                foreach (string exclude_type in excludeTypes)
                {
                    matches = remainingObjects.Where(o => string.Equals(exclude_type, o.Type, StringComparison.OrdinalIgnoreCase));
                    remainingObjects = remainingObjects.Except(matches);           
                }
            }

            return remainingObjects;
        }

        private static IEnumerable<ScriptingObject> IncludeSchemaAndOrType(IEnumerable<string> includeSchemas, IEnumerable<string> includeTypes, 
            IEnumerable<ScriptingObject> candidates)
        {
            // Given a list of candidates, we return a new list of scripting objects that match
            // the schema and/or type filter.
            IEnumerable<ScriptingObject> matchedSchema = new List<ScriptingObject>();
            IEnumerable<ScriptingObject> matchedType = new List<ScriptingObject>();
            IEnumerable<ScriptingObject> matchedObjects = new List<ScriptingObject>();
            IEnumerable<ScriptingObject> matches = null;

            if (includeSchemas != null && includeSchemas.Any())
            {            
                foreach (string include_schema in includeSchemas)
                {
                    matches = MatchCriteria(include_schema, (candidate) => { return candidate.Schema; }, candidates);
                    matchedSchema = matchedSchema.Union(matches);
                }
                matchedObjects = matchedSchema;
            }
            else
            {
                matchedObjects = candidates;
            }

            if (includeTypes != null && includeTypes.Any())
            {
                foreach (string include_type in includeTypes)
                {
                    matches = matchedObjects.Where(o => string.Equals(include_type, o.Type, StringComparison.OrdinalIgnoreCase));
                    matchedType = matchedType.Union(matches);
                }
                matchedObjects = matchedType;
            }

            return matchedObjects;
        }

        private static IEnumerable<ScriptingObject> MatchCriteria(ScriptingObject criteria, IEnumerable<ScriptingObject> candidates)
        {
            Validate.IsNotNull("criteria", criteria);
            Validate.IsNotNull("candidates", candidates);

            IEnumerable<ScriptingObject> matchedObjects = candidates;

            if (!string.IsNullOrWhiteSpace(criteria.Type))
            {
                matchedObjects = matchedObjects.Where(o => string.Equals(criteria.Type, o.Type, StringComparison.OrdinalIgnoreCase));
            }

            matchedObjects = MatchCriteria(criteria.Schema, (candidate) => { return candidate.Schema; }, matchedObjects);
            matchedObjects = MatchCriteria(criteria.Name, (candidate) => { return candidate.Name; }, matchedObjects);

            return matchedObjects;
        }

        private static IEnumerable<ScriptingObject> MatchCriteria(string property, Func<ScriptingObject, string> propertySelector, IEnumerable<ScriptingObject> candidates)
        {
            IEnumerable<ScriptingObject> matchedObjects = candidates;

            if (!string.IsNullOrWhiteSpace(property))
            {
                if (property.Equals(Wildcard, StringComparison.OrdinalIgnoreCase))
                {
                    // Don't filter any objects
                }
                if (property.EndsWith(Wildcard, StringComparison.OrdinalIgnoreCase))
                {
                    matchedObjects = candidates.Where(o => propertySelector(o).StartsWith(
                        propertySelector(o).Substring(0, propertySelector(o).Length - 1),
                        StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    matchedObjects = matchedObjects.Where(o => string.Equals(property, propertySelector(o), StringComparison.OrdinalIgnoreCase));
                }
            }

            return matchedObjects;
        }
    }
}
