using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Kusto.ServiceLayer.DataSource
{
    public class DstsAuthenticationManager
    {
        private static readonly Lazy<DstsAuthenticationManager> _instance = new Lazy<DstsAuthenticationManager>();

        public static DstsAuthenticationManager Instance => _instance.Value;

        private static ConcurrentDictionary<string, Tuple<DateTime, string>> _tokens;

        public DstsAuthenticationManager()
        {
            _tokens = new ConcurrentDictionary<string, Tuple<DateTime, string>>();
        }

        public string GetDstsAuthToken(string clusterName, string databaseName)
        {
            if (TryGetDstsAuthToken(clusterName, databaseName, out string token))
            {
                return token;
            }

            return LoadDstsAuthToken(clusterName, databaseName);
        }

        private bool TryGetDstsAuthToken(string clusterName, string databaseName, out string token)
        {
            token = string.Empty;
            
            string key = $"{clusterName} {databaseName}";
            if (_tokens.ContainsKey(key))
            {
                // check if the token has expired
                if (_tokens[key].Item1 > DateTime.Now)
                {
                    token = _tokens[key].Item2;
                    return true;
                }

                // remove token to be set
                _tokens.TryRemove(key, out _);
            }

            return false;
        }

        private string LoadDstsAuthToken(string clusterName, string databaseName)
        {
            using (var process = new Process())
            {
                process.StartInfo.FileName = @"C:\Git\dSTSTokenApp\dSTSTokenApp\bin\Debug\dSTSTokenApp.exe";
                process.StartInfo.Arguments = $"{clusterName} {databaseName}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.Start();

                StreamReader reader = process.StandardOutput;
                string token = reader.ReadToEnd();

                process.WaitForExit();

                _tokens[$"{clusterName} {databaseName}"] = new Tuple<DateTime, string>(DateTime.Now.AddHours(23), token);
                return token;
            }
        }
    }
}