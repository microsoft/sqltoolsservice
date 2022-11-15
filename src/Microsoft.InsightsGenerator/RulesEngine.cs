﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.InsightsGenerator
{
    public class RulesEngine
    {
        public static List<Template> Templates;

        public static List<string> TopListHashHeaders = new List<string>{ "#top", "#averageSlice", "#topPerslice" , "#bottom"};

        public RulesEngine()
        {
            Templates = GetTemplates();
        }

        public static ColumnHeaders TemplateParser(string templateContent)
        {

            ColumnHeaders ch = new ColumnHeaders();
            var processedText = Regex.Replace(templateContent, @",|\\n", "");
            ch.Template = templateContent;

            List<string> keyvalue = processedText.Split(' ').Select(s => s.Trim()).ToList();

            foreach (string s in keyvalue)
            {
                if (s.StartsWith("#"))
                {
                    string headers = s.Substring(1, s.Length - 1);
                    if (headers.StartsWith("#"))
                    {
                        ch.DoubleHashValues.Add( "##" + headers.Substring(1, headers.Length - 1));
                    }
                    else
                    {
                        if (headers != "placeHolder")
                        {
                            ch.SingleHashValues.Add("#" + headers);
                        }
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
        public static string FindMatchedTemplate(List<List<string>> singleHashHeaders, DataArray columnInfo)
        {
            var resultTemplate = new StringBuilder();
            Templates ??= GetTemplates();
            var headersWithSingleHash = GetTopHeadersWithHash(singleHashHeaders);

            foreach (var template in Templates)
            {
                var columnHeaders = TemplateParser(template.Content);
                var singleHashFromTemplate = columnHeaders.SingleHashValues.Distinct();

                var isMatched = true;

                // all the values from template needs to be found in the input from SigGen
                foreach (var hashFromTemplate in singleHashFromTemplate)
                {
                    if (!headersWithSingleHash.Contains(hashFromTemplate.ToLower()))
                    {
                        isMatched = false;
                        break;
                    }
                }
                if (isMatched)
                {
                    // Replace # and ## values in template with actual values
                    resultTemplate.AppendLine(ReplaceHashesInTemplate(singleHashHeaders, columnInfo, template) + "\n");
                }  
            }

            // No matched Template found
            return resultTemplate.ToString();
        }

        private static string ReplaceHashesInTemplate(List<List<string>>singleHashList, DataArray columnInfo, Template template)
        {
            StringBuilder modifiedTemp = new StringBuilder(template.Content);

            // Replace single hash values
            foreach (var line in singleHashList)
            {
                // Example of a single list (line): 
                // "top", "3", " input (value) %OfValue ", " input (value) %OfValue ", " input (value) %OfValue "
                var headerInputs = line.ToArray();
                string header = "#" + headerInputs[0];
                
                if (TopListHashHeaders.Contains(header))
                {
                    if(!modifiedTemp.ToString().Contains(header))
                    {
                        continue;
                    }
                    //First replace the header with the second value in the list

                    modifiedTemp.Replace(header, headerInputs[1]);
                    StringBuilder remainingStr = new StringBuilder();
                    for (int i = 2; i < headerInputs.Length; i++)
                    {
                        // Append all the rest of the elemet in the array separated by new line
                        remainingStr.AppendLine(headerInputs[i]);
                    }
                    modifiedTemp.Replace("#placeHolder", remainingStr.ToString());
                }
                else 
                {
                    modifiedTemp.Replace("#" + headerInputs[0], headerInputs[1]);
                }
            }

            // Replace double hash values
            var transformedColumnArray = columnInfo.TransformedColumnNames.ToArray();
            var columnArray = columnInfo.ColumnNames.ToArray();

            for (int p = 0; p < columnInfo.TransformedColumnNames.Length; p++)
            {
                modifiedTemp.Replace("##" + transformedColumnArray[p], columnArray[p]);
            }

            return modifiedTemp.ToString();
        }

        private static List<string> GetTopHeadersWithHash(List<List<string>> singleHashHeaders)
        {
            var topHeaderList = new List<string>();
            foreach (var list in singleHashHeaders)
            {
                topHeaderList.Add("#" + list.First().ToLower());
            }
            return topHeaderList;
        }

        /// <summary>
        /// Retrieve all the templates and template ids
        /// </summary>
        /// <returns>All the template & id comnbination </returns>
        public static List<Template> GetTemplates()
        {
            var templateHolder = new List<Template>();
            string assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string assemblyDirectoryPath = System.IO.Path.GetDirectoryName(assemblyPath);
            string templateFilePath = Path.Combine(assemblyDirectoryPath, "Templates", "templates.txt");

            using (StreamReader streamReader = new StreamReader(templateFilePath, Encoding.UTF8))
            {
                int temId = 0;
                var wholeText = streamReader.ReadToEnd();
                var templateArray = wholeText.Split(new[] { "Template " }, StringSplitOptions.None).ToList();
                
                foreach (var line in templateArray.Where(r => r != string.Empty))
                {
                    var parts = line.Split(new[] { Environment.NewLine }, StringSplitOptions.None).ToList();
                    
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
