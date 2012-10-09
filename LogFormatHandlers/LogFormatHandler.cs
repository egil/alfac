using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assimilated.Alfac.Utils;

namespace Assimilated.Alfac.LogFormatHandlers
{
    public abstract class LogFormatHandler
    {
        protected string TokenizerRegexPattern { get; set; }
        protected string ConnectionString { get; set; }
        public abstract string TableName { get; protected set; }
        public abstract ADOX.Table GetTable();

        protected LogFormatHandler(string connectionString)
        {
            ConnectionString = connectionString;
        }

        protected abstract OleDbCommand CreateCommand(OleDbConnection connection);

        //public void AddToDatabase(IEnumerable<FileInfo> logFiles)
        //{
        //    int errorCountTotal = 0;
        //    int successCountTotal = 0;

        //    using (var con = new OleDbConnection(ConnectionString))
        //    {
        //        using (var cmd = CreateCommand(con))
        //        {
        //            // open the connection
        //            if (con.State == ConnectionState.Closed) con.Open();

        //            var tokenizer = new LogEntryTokenizer(TokenizerRegexPattern);
        //            var filesReader = new LogFilesReader(logFiles);
        //            foreach (var entry in filesReader.GetEntires())
        //            {
        //                AddEntry(cmd, entry);
                        
        //                // save to db
        //                try
        //                {
        //                    cmd.ExecuteNonQuery();
        //                    successCount++;
        //                }
        //                catch (Exception ex)
        //                {
        //                    errorCount++;
        //                    if (errorLogFileName != null) WriteErrorLogFile(errorLogFileName, ex.Message, rawentry);
        //                }
        //            }

        //            // iterate oer each logfile and add their entries to the database
        //            foreach (var log in )
        //            {
        //                var actualFullPath = log.FullName;

        //                // unpack log file if zipped/compressed
        //                if (log.Extension == ".zip" || log.Extension == ".gz")
        //                {
        //                    Console.WriteLine("Decompressing: {0}", log.FullName);
        //                    actualFullPath = DecompressFile(log);
        //                }

        //                int errorCount = 0;
        //                int successCount = 0;
        //                Console.WriteLine("Processing: {0}", log.FullName);

        //                using (TextReader tr = new StreamReader(actualFullPath))
        //                {
        //                    string rawentry;

        //                    // process all lines in file
        //                    while ((rawentry = tr.ReadLine()) != null)
        //                    {
        //                        var matches = combinedFormatMatcher.Matches(rawentry);

        //                        if (matches.Count == 1)
        //                        {
        //                            var groups = matches[0].Groups;

        //                            // Assign captured values
        //                            remoteHost.Value = groups[1].Value != "-" ? groups[1].Value : string.Empty;
        //                            remoteLogname.Value = groups[2].Value != "-" ? groups[2].Value : string.Empty;
        //                            remoteUser.Value = groups[3].Value != "-" ? groups[3].Value : string.Empty;
        //                            time.Value = DateTime.Parse(string.Format("{0} {1} {2}", groups[4].Value, groups[5].Value, groups[6].Value));
        //                            method.Value = groups[7].Value;
        //                            url.Value = groups[8].Value;
        //                            protocol.Value = groups[9].Value;
        //                            status.Value = groups[10].Value != "-" ? groups[10].Value : string.Empty;
        //                            bytesSent.Value = groups[11].Value != "-" ? groups[11].Value : string.Empty;
        //                            referer.Value = groups[12].Value != "-" ? groups[12].Value : string.Empty;
        //                            userAgent.Value = groups[13].Value != "-" ? groups[13].Value : string.Empty;

        //                            // save to db
        //                            try
        //                            {
        //                                cmd.ExecuteNonQuery();
        //                                successCount++;
        //                            }
        //                            catch (Exception ex)
        //                            {
        //                                errorCount++;
        //                                if (errorLogFileName != null) WriteErrorLogFile(errorLogFileName, ex.Message, rawentry);
        //                            }
        //                        }
        //                        else
        //                        {
        //                            errorCount++;
        //                            if (errorLogFileName != null) WriteErrorLogFile(errorLogFileName, "Error parsing the entry.", rawentry);
        //                        }

        //                        // update count                                
        //                        Console.Write("\r  Added: {0:N0} - Skipped: {1:N0}  ", successCount, errorCount);
        //                    }
        //                }

        //                // clean up decompressed file if any
        //                if (actualFullPath != log.FullName) File.Delete(actualFullPath);

        //                errorCountTotal += errorCount;
        //                successCountTotal += successCount;

        //                // add a new line between each file
        //                Console.WriteLine();
        //                Console.WriteLine();
        //            }
        //        }
        //    }

        //    Console.WriteLine("Finished adding log files");
        //    Console.WriteLine("");
        //    Console.WriteLine("Number of files parsed: {0}", logs.Length);
        //    Console.WriteLine("Number of entries added: {0}", successCountTotal);
        //    Console.WriteLine("Number of parse error: {0}", errorCountTotal);
        //}

        protected abstract void AddEntry(OleDbCommand cmd, string entry);
    }
}
