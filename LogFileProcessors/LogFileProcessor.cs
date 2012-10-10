using System;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Text.RegularExpressions;
using Assimilated.Alfac.LogHandlers;
using Assimilated.Alfac.Utils;

namespace Assimilated.Alfac.LogFileProcessors
{
    public abstract class LogFileProcessor
    {
        private FileInfo[] _logFiles = new FileInfo[0];
        public FileInfo[] LogFiles
        {
            get { return _logFiles; }
            set { _logFiles = value; }
        }

        protected abstract string InsertSqlExpression { get; }
        protected abstract string TokenizerRegexPattern { get; }
        public abstract string TableName { get; }
        public abstract string Name { get; }
        public abstract LogFileType LogFileType { get; }
        public abstract ADOX.Table GetTable();      

        public void Process(string databaseConnectionString)
        {
            // Gather statictics 
            int errorCountTotal = 0;
            int successCountTotal = 0;

            using (var con = new OleDbConnection(databaseConnectionString))
            {
                using (var cmd = con.CreateCommand())
                {
                    // Configure the command object
                    cmd.CommandText = InsertSqlExpression;
                    AddSqlParamters(cmd);

                    // open the connection
                    if (con.State == ConnectionState.Closed) con.Open();

                    // instnatiate the regex object we will use to
                    // tokenize each logfile entry
                    var tokenizer = new Regex(TokenizerRegexPattern, RegexOptions.Compiled);

                    // iterate oer each logfile and add their entries to the database                    
                    foreach (var logFile in LogFiles)
                    {
                        Logger.Info("Processing: {0}", logFile.FullName);

                        int errorCount = 0;
                        int successCount = 0;

                        foreach (var entry in LogFilesReader.GetEntries(logFile))
                        {
                            var entryTokens = tokenizer.Match(entry);

                            if (entryTokens.Success)
                            {
                                SetSqlParamaterValues(entryTokens.Groups);

                                // save to db
                                try
                                {
                                    cmd.ExecuteNonQuery();
                                    successCount++;
                                }
                                catch (Exception ex)
                                {
                                    Logger.Info();
                                    Logger.Error("Error saving entry to database.");
                                    Logger.Error(ex.Message);
                                    Logger.Info();
                                    errorCount++;

                                    // break out, end processing
                                    return;
                                }
                            }
                            else
                            {
                                errorCount++;
                                Logger.Info();
                                Logger.Error("Error parsing a entry in log file: {0}, line {1}", logFile.FullName, successCount + errorCount);
                                Logger.Info();
                            }

                            // update progress count                                                            
                            Logger.UpdateProcessCounter("\r  Added: {0:N0} - Skipped: {1:N0}  ",
                                successCount,
                                errorCount);
                        }

                        // log to file
                        Logger.LogOffscreen("  Added: {0:N0} - Skipped: {1:N0}", successCount, errorCount);

                        errorCountTotal += errorCount;
                        successCountTotal += successCount;

                        // add a new line between each file
                        Logger.Info();
                        Logger.Info();
                    }
                }
            }

            Logger.Info("Finished adding log files:");
            Logger.Info();
            Logger.Info("  Number of files parsed: {0}", LogFiles.Length);
            Logger.Info("  Number of entries added: {0}", successCountTotal);
            Logger.Info("  Number of parse error: {0}", errorCountTotal);
        }

        protected abstract void SetSqlParamaterValues(GroupCollection groups);
        protected abstract void AddSqlParamters(OleDbCommand cmd);

        /// <summary>
        /// Create an instance of a log file processor, 
        /// that matches the LogFileType argument.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static LogFileProcessor Create(LogFileType type)
        {
            switch (type)
            {
                case LogFileType.CombinedLogFormat:
                    return new CombinedLogFormatProcessor();
                    break;
                default:
                    throw new ArgumentException("Unknown log file type.");
            }
        }
    }

}
