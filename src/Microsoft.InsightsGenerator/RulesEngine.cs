//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.InsightsGenerator
{
    public class RulesEngine
    {
        public static List<Template> Templates;

        public RulesEngine()
        {
            Templates = GetTemplates();
        }

        public static ColumnHeaders TemplateParser(string templateContent)
        {

            ColumnHeaders ch = new ColumnHeaders();
            var processedText = Regex.Replace(templateContent, @",|\\n", "");
            ch.Template = processedText;

            List<string> keyvalue = processedText.Split(' ').Select(s => s.Trim()).ToList();

            foreach (string s in keyvalue)
            {
                if (s.StartsWith("#"))
                {
                    string headers = s.Substring(1, s.Length - 1);
                    if (headers.StartsWith("#"))
                    {
                        ch.DoubleHashValues.Add(headers.Substring(1, headers.Length - 1));
                    }
                    else
                    {
                        ch.SingleHashValues.Add(headers);
                    }
                }
            }
            return ch;
        }

        /// <summary>
        /// Find a matched template based on the single hash values
        /// </summary>
        /// <param name="singleHashHeaders"></param>
        /// <returns></returns>
        public static Template FindMatchedTemplate(List<string> singleHashHeaders, List<string>doubleHashHeaders)
        {
            if (Templates == null)
            {
                Templates = GetTemplates();
            }
            var headersWithSingleHash = AppendPrefix("#", singleHashHeaders);
            var headersWithDoubleHash = AppendPrefix("##", doubleHashHeaders);

            foreach (var template in Templates)
            {
                var columnHeaders = TemplateParser(template.Content);
                var singleHash = columnHeaders.SingleHashValues;

                if (singleHashHeaders.Count == singleHash.Count)
                {
                    if (Enumerable.SequenceEqual(singleHash.OrderBy(s => s), headersWithSingleHash.OrderBy(s => s)))
                    {
                        // Replace # and ## values in template with actual values here befor return
                        return template;
                    }
                }
            }

            // No matched Template found
            return null;
        }

        /// <summary>
        /// Append prefix like # or ## to each element in the list
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="list"></param>
        /// <returns>Modified list</returns>
        private static List<string> AppendPrefix(string prefix, List<string> list)
        {
            var listWithPrefix = new List<string>();
            foreach (var str in list)
            {
                listWithPrefix.Add(prefix + str);
            }
            return listWithPrefix;
        }

        /// <summary>
        /// Retrieve all the templates and template ids
        /// </summary>
        /// <returns>All the template & id comnbination </returns>
        public static List<Template> GetTemplates()
        {
            var templateHolder = new List<Template>();

            int templateId;
            using (StreamReader streamReader = new StreamReader($"Templates/templates.txt", Encoding.UTF8))
            {
                int temId = 0;
                var wholeText = streamReader.ReadToEnd();
                var templateArray = wholeText.Split(new[] { "Template " }, StringSplitOptions.None).ToList();
                
                foreach (var line in templateArray.Where(r => r != string.Empty))
                {
                    var parts = line.Split(new[] { "\r\n" }, StringSplitOptions.None).ToList();
                    
                        temId = int.Parse(parts[0]);
                        templateHolder.Add(new Template(temId, parts[1]));
                }

                return templateHolder;
            }
        }
    }

    /// <summary>
    /// Template container to hold the template id + template body combination
    /// </summary>
    public class Template
    {
        public Template(int id, string content)
        {
            Id = id;
            Content = content;
        }

        public int Id { get; set; }
        public string Content { get; set; }
    }

    public class ColumnHeaders
    {
        public ColumnHeaders()
        {
            SingleHashValues = new List<string>();
            DoubleHashValues = new List<string>();
            Template = null;
        }

        public List<string> SingleHashValues { get; set; }
        public List<string> DoubleHashValues { get; set; }
        public string Template { get; set; }
    }
}

