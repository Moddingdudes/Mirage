using System.Collections.Generic;
using Mirage.Logging;

namespace Mirage.EditorScripts.Logging
{
    /// <summary>
    /// Removes duplicates and updates log settings from LogFactory
    /// </summary>
    public class LogSettingChecker
    {
        private readonly LogSettingsSO settings;
        private readonly HashSet<string> duplicateChecker = new HashSet<string>();

        public LogSettingChecker(LogSettingsSO settings)
        {
            this.settings = settings;
        }

        public void Refresh()
        {
            this.duplicateChecker.Clear();
            this.RemoveDuplicates();
            this.AddNewFromFactory();
        }

        private void RemoveDuplicates()
        {
            for (var i = 0; i < this.settings.LogLevels.Count; i++)
            {
                var added = this.duplicateChecker.Add(this.settings.LogLevels[i].FullName);
                // is duplicate, remove it
                if (!added)
                {
                    this.settings.LogLevels.RemoveAt(i);
                    i--;
                }
            }
        }

        private void AddNewFromFactory()
        {
            // try add new types
            foreach (var logger in LogFactory.Loggers)
            {
                var added = this.duplicateChecker.Add(logger.Key);
                // is new, add it
                if (added)
                {
                    this.settings.LogLevels.Add(new LogSettingsSO.LoggerSettings(logger.Key, logger.Value.filterLogType));
                }
            }
        }
    }
}
