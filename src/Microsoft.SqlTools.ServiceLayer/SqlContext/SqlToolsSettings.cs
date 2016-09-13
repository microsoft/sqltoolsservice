using System.IO;
using Microsoft.SqlTools.EditorServices.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SqlContext
{
    /// <summary>
    /// Class for serialization and deserialization of the settings the SQL Tools Service needs.
    /// </summary>
    public class SqlToolsSettings
    {
        public SqlToolsSettings()
        {
            this.ScriptAnalysis = new ScriptAnalysisSettings();
            this.QueryExecutionSettings = new QueryExecutionSettings();
        }

        public bool EnableProfileLoading { get; set; }

        public ScriptAnalysisSettings ScriptAnalysis { get; set; }

        public void Update(SqlToolsSettings settings, string workspaceRootPath)
        {
            if (settings != null)
            {
                this.EnableProfileLoading = settings.EnableProfileLoading;
                this.ScriptAnalysis.Update(settings.ScriptAnalysis, workspaceRootPath);
            }
        }

        public QueryExecutionSettings QueryExecutionSettings { get; set; }
    }

    /// <summary>
    /// Sub class for serialization and deserialization of script analysis settings
    /// </summary>
    public class ScriptAnalysisSettings
    {
        public bool? Enable { get; set; }

        public string SettingsPath { get; set; }

        public ScriptAnalysisSettings()
        {
            this.Enable = true;
        }

        public void Update(ScriptAnalysisSettings settings, string workspaceRootPath)
        {
            if (settings != null)
            {
                this.Enable = settings.Enable;

                string settingsPath = settings.SettingsPath;

                if (string.IsNullOrWhiteSpace(settingsPath))
                {
                    settingsPath = null;
                }
                else if (!Path.IsPathRooted(settingsPath))
                {
                    if (string.IsNullOrEmpty(workspaceRootPath))
                    {
                        // The workspace root path could be an empty string
                        // when the user has opened a SqlTools script file
                        // without opening an entire folder (workspace) first.
                        // In this case we should just log an error and let
                        // the specified settings path go through even though
                        // it will fail to load.
                        Logger.Write(
                            LogLevel.Error,
                            "Could not resolve Script Analyzer settings path due to null or empty workspaceRootPath.");
                    }
                    else
                    {
                        settingsPath = Path.GetFullPath(Path.Combine(workspaceRootPath, settingsPath));
                    }
                }

                this.SettingsPath = settingsPath;
            }
        }
    }
}
