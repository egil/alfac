using System;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using ADOX;

namespace Assimilated.Alfac.LogHandlers
{
    public class CombinedLogFormatHandler : ILogFileHandler
    {
        private const string InsertSql =
            "INSERT INTO Access([RemoteHost],[RemoteIdentity],[RemoteUser],[Time],[Method],[URL],[Protocol],[Status],[BytesSent],[Referer],[UserAgent]) " +
            "VALUES(@RemoteHost,@RemoteIdentity,@RemoteUser,@Time,@Method,@URL,@Protocol,@Status,@BytesSent,@Referer,@UserAgent)";

        private const string RegexPattern = @"(\S+) (\S+) (\S+) \[([^:]+):(\d+:\d+:\d+) ([^\]]+)\] ""(\S+) (.+?) (\S+)[ ]*"" (\S+) (\S+) ""([^""]*?)"" ""([^""]*?)""";
        private readonly Regex combinedFormatMatcher = new Regex(RegexPattern, RegexOptions.Compiled);

        public string LogFormat { get { return "\"%h %l %u %t \"%r\" %>s %b\" common"; } }
        public string TableName { get { return "Access"; } }
        public string Name { get { return "Combined Log Format"; } }
        public LogFileType LogFileType { get { return LogFileType.CombinedLogFormat; } }

        public ADOX.Table GetTable()
        {
            var table = new ADOX.Table();
            table.Name = TableName;
            table.Columns.Append("RemoteHost");
            table.Columns.Append("RemoteIdentity");
            table.Columns.Append("RemoteUser");
            table.Columns.Append("Time", DataTypeEnum.adDate);
            table.Columns.Append("Method");
            table.Columns.Append("URL", DataTypeEnum.adLongVarWChar);
            table.Columns.Append("Protocol");
            table.Columns.Append("Status", DataTypeEnum.adSmallInt);
            table.Columns.Append("BytesSent", DataTypeEnum.adInteger);
            table.Columns.Append("Referer", DataTypeEnum.adLongVarWChar);
            table.Columns.Append("UserAgent", DataTypeEnum.adLongVarWChar);

            // allow all columns to be empty
            foreach (ADOX.Column column in table.Columns)
            {
                column.Attributes = ColumnAttributesEnum.adColNullable;
            }

            var rhindex = new ADOX.Index();
            rhindex.Name = "RemoteHost index";
            rhindex.IndexNulls = AllowNullsEnum.adIndexNullsIgnore;
            rhindex.Columns.Append("RemoteHost");
            table.Indexes.Append(rhindex);

            var mindex = new ADOX.Index();
            mindex.Name = "Method index";
            mindex.IndexNulls = AllowNullsEnum.adIndexNullsIgnore;
            mindex.Columns.Append("Method");
            table.Indexes.Append(mindex);

            var tindex = new ADOX.Index();
            tindex.Name = "Time index";
            tindex.IndexNulls = AllowNullsEnum.adIndexNullsIgnore;
            tindex.Columns.Append("Time", DataTypeEnum.adDate);
            table.Indexes.Append(tindex);

            var sindex = new ADOX.Index();
            sindex.Name = "Status index";
            sindex.IndexNulls = AllowNullsEnum.adIndexNullsIgnore;
            sindex.Columns.Append("Status", DataTypeEnum.adSmallInt);
            table.Indexes.Append(sindex);

            return table;
        }

        public void AddLogFilesToDatabase(FileInfo[] logs, string dbcon, FileInfo errorLogFileName = null)
        {
            int errorCountTotal = 0;
            int successCountTotal = 0;

            using (var con = new OleDbConnection(dbcon))
            {
                using (var cmd = con.CreateCommand())
                {
                    cmd.CommandText = InsertSql;

                    // set up paramters
                    var remoteHost = CreateParamater(cmd, "@RemoteHost");
                    var remoteLogname = CreateParamater(cmd, "@RemoteIdentity");
                    var remoteUser = CreateParamater(cmd, "@RemoteUser");
                    var time = CreateParamater(cmd, "@Time");
                    var method = CreateParamater(cmd, "@Method");
                    var url = CreateParamater(cmd, "@URL");
                    var protocol = CreateParamater(cmd, "@Protocol");
                    var status = CreateParamater(cmd, "@Status", DbType.Int16);
                    var bytesSent = CreateParamater(cmd, "@BytesSent", DbType.Int32);
                    var referer = CreateParamater(cmd, "@Referer");
                    var userAgent = CreateParamater(cmd, "@UserAgent");

                    // open the connection
                    if (con.State == ConnectionState.Closed) con.Open();

                    // iterate oer each logfile and add their entries to the database
                    foreach (var log in logs)
                    {
                        var actualFullPath = log.FullName;

                        // unpack log file if zipped/compressed
                        if (log.Extension == ".zip" || log.Extension == ".gz")
                        {
                            Console.WriteLine("Decompressing: {0}", log.FullName);
                            actualFullPath = DecompressFile(log);
                        }

                        int errorCount = 0;
                        int successCount = 0;
                        Console.WriteLine("Processing: {0}", log.FullName);

                        using (TextReader tr = new StreamReader(actualFullPath))
                        {
                            string rawentry;

                            // process all lines in file
                            while ((rawentry = tr.ReadLine()) != null)
                            {
                                var matches = combinedFormatMatcher.Matches(rawentry);

                                if (matches.Count == 1)
                                {
                                    var groups = matches[0].Groups;

                                    // Assign captured values
                                    remoteHost.Value = groups[1].Value != "-" ? groups[1].Value : string.Empty;
                                    remoteLogname.Value = groups[2].Value != "-" ? groups[2].Value : string.Empty;
                                    remoteUser.Value = groups[3].Value != "-" ? groups[3].Value : string.Empty;
                                    time.Value = DateTime.Parse(string.Format("{0} {1} {2}", groups[4].Value, groups[5].Value, groups[6].Value));
                                    method.Value = groups[7].Value;
                                    url.Value = groups[8].Value;
                                    protocol.Value = groups[9].Value;
                                    status.Value = groups[10].Value != "-" ? groups[10].Value : string.Empty;
                                    bytesSent.Value = groups[11].Value != "-" ? groups[11].Value : string.Empty;
                                    referer.Value = groups[12].Value != "-" ? groups[12].Value : string.Empty;
                                    userAgent.Value = groups[13].Value != "-" ? groups[13].Value : string.Empty;

                                    // save to db
                                    try
                                    {
                                        cmd.ExecuteNonQuery();
                                        successCount++;
                                    }
                                    catch (Exception ex)
                                    {
                                        errorCount++;
                                        if (errorLogFileName != null) WriteErrorLogFile(errorLogFileName, ex.Message, rawentry);
                                    }
                                }
                                else
                                {
                                    errorCount++;
                                    if (errorLogFileName != null) WriteErrorLogFile(errorLogFileName, "Error parsing the entry.", rawentry);
                                }

                                // update count                                
                                Console.Write("\r  Added: {0:N0} - Skipped: {1:N0}  ", successCount, errorCount);
                            }
                        }

                        // clean up decompressed file if any
                        if (actualFullPath != log.FullName) File.Delete(actualFullPath);

                        errorCountTotal += errorCount;
                        successCountTotal += successCount;

                        // add a new line between each file
                        Console.WriteLine();
                        Console.WriteLine();
                    }
                }
            }

            Console.WriteLine("Finished adding log files");
            Console.WriteLine("");
            Console.WriteLine("Number of files parsed: {0}", logs.Length);
            Console.WriteLine("Number of entries added: {0}", successCountTotal);
            Console.WriteLine("Number of parse error: {0}", errorCountTotal);
        }

        private static string DecompressFile(FileInfo log)
        {
            string actualFullPath;
            using (var originalFileStream = log.OpenRead())
            {
                actualFullPath = Path.GetTempFileName().Replace(".tmp", ".log");

                using (var decompressedFileStream = File.Create(actualFullPath))
                {
                    using (var decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(decompressedFileStream); 
                    }
                }
            }
            return actualFullPath;
        }

        private static void WriteErrorLogFile(FileInfo errorLogFileName, string title, string rawentry)
        {
            using (TextWriter errlog = new StreamWriter(errorLogFileName.FullName))
            {
                errlog.WriteLine(title);
                errlog.WriteLine(rawentry);
                errlog.WriteLine();
            }
        }

        private static OleDbParameter CreateParamater(OleDbCommand cmd, string name, DbType type = DbType.String)
        {
            var param = cmd.CreateParameter();
            param.IsNullable = true;
            param.DbType = type;
            param.ParameterName = name;
            cmd.Parameters.Add(param);
            return param;
        }
    }
}
