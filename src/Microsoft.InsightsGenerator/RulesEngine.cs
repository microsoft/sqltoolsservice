using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;
using System.IO;

namespace Microsoft.InsightsGenerator
{
    public class RulesEngine
    {
        public class ColumnHeaders
        {
            public List<string> singleHashValues;
            public List<string> doubleHashValues;
            public string template;
        }


        public static ColumnHeaders TemplateParser(string templateFile)
        {
            StreamReader file = new StreamReader($"{templateFile}");
            string line = null;
            string templateText = null;
            ColumnHeaders ch = new ColumnHeaders();
            ch.singleHashValues = new List<string>();
            ch.doubleHashValues = new List<string>();
            while (!file.EndOfStream)
            {
                line = file.ReadLine();
                templateText = line;
                ch.template = templateText;
                List<string> keyvalue = line.Split(' ').Select(s => s.Trim()).ToList();
                foreach (string s in keyvalue)
                {
                    if (s.StartsWith("#"))
                    {
                        string headers = s.Substring(1, s.Length - 1);
                        if (headers.StartsWith("#"))
                        {
                            ch.doubleHashValues.Add(headers.Substring(1, headers.Length - 1));
                        }
                        else
                        {
                            ch.singleHashValues.Add(headers);
                        }
                    }
                    if (s.Contains("tempId"))
                    {
                        Debug.WriteLine(s);
                    }
                }

            }

            return ch;
        }

        public static Dictionary<int, string> RulesGeneratorFromTemplate()
        {
            Dictionary<int, string> Rules_templateID = new Dictionary<int, string>();
            string rules = null;
            ColumnHeaders header = TemplateParser(@"template_16.txt");
            return Rules_templateID;
        }

        public static Dictionary<int, string> RulesGeneratorFromInput(string singleHashHeaders, string doublehashHeaders)
        {

            string rules = null;
            //rulesgenerator
            return rules;
        }
        public static string RulesChecking(string singleHashHeaders, string doublehashHeaders)
        {
            string template = null;
            //if(singleHashHeaders!=null && doublehashHeaders!=null && RulesGeneratorFromInput(singleHashHeaders, doublehashHeaders).Equals(RulesGeneratorFromTemplate()){
            //from dictionary mapping select template id
            //}

            return template;
        }

        public static void Main(string[] args)
        {
            //calling InsightsGenerator
            //RulesChecking(input from Insights generater);
            //return;
        }
    }
}

