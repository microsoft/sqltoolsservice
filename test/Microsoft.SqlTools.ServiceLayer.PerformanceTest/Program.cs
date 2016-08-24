using System;
using System.Diagnostics;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.PerformanceTest.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using System.Threading;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.SqpTools.ServiceLayer.PerformanceTest
{
    public class Program
    {
        public static Dictionary<string, string> dict = new Dictionary<string, string>();
        public static void Main(string[] args)
        {
            // wait for event registration on testshell side
            Thread.Sleep(1000);

            // steps to create a scenario
            // 1 define scenario
            // 2 get metrics of the scenario 
            // 3 send measures 
            GetTime(Parse_Test);
            SendResult();
       }
        // metrics: running time
        public static void GetTime(Action scenario) {

            var stopwatch = new Stopwatch();

            stopwatch.Start();

            scenario();
            
            stopwatch.Stop();

            dict.Add("Time", stopwatch.ElapsedMilliseconds.ToString());
           
        }

        // scenario 
        public static void Parse_Test() {
            dict.Add("ScenarioName","Parse");
            ParseResult parseResult = Parser.Parse("SELECT * FROM sys.objects");
            return ;
        }

        // send metrics to standard I/O
        public static void SendResult() {

            XElement root = new XElement("Root",
               from keyValue in dict
               select new XElement(keyValue.Key, keyValue.Value)
           );
           
            Console.WriteLine(root.ToString());

            return ;
        }

        

    }
}
