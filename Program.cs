using System;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using Assimilated.Alfac.LogHandlers;
using Assimilated.Alfac.Utils;

namespace Assimilated.Alfac
{
    class Program
    {
        static FileInfo _dbFileName;
        static DirectoryInfo _logs;
        static LogFileType _type;
        static string _logFilter;
        static FileInfo _errorLog;
        static ILogFileHandler _logFileHandler;

        static void Main(string[] args)
        {
            // read in arguments, parse them for easy consumption
            ParseCommandlineArguments(args);

            // Print welcome message
            Console.WriteLine();
            Console.WriteLine("Apatche Log Files to Access Database Converter");
            Console.WriteLine("  Version 0.2 - By: Egil Hansen (http://egilhansen.com)");
            Console.WriteLine();
            Console.WriteLine("Runtime settings:");
            Console.WriteLine();
            Console.WriteLine("  Database File (db):\t {0}", _dbFileName.FullName);            
            Console.WriteLine("  Logs Directory (logs): {0}", _logs.FullName);
            Console.WriteLine("  Log filter (filter):\t {0}", _logFilter);
            Console.WriteLine("  Log File Type (type):  {0}", _logFileHandler.Name);            
            Console.WriteLine("  Error Log (errorlog):  {0}", _errorLog != null ? _errorLog.FullName : "n/a");            
            Console.WriteLine();

            // create target database based on log file type
            var csb = new OleDbConnectionStringBuilder
                          {
                              DataSource = _dbFileName.FullName,
                              Provider = "Microsoft.Jet.OLEDB.4.0"
                          };
            // create database
            CreateAccessDatabase(csb.ToString());

            // parse each log file, add each log entry to database
            Console.WriteLine("Staring processing . . .");
            Console.WriteLine();
            _logFileHandler.AddLogFilesToDatabase(_logs.GetFiles(_logFilter), csb.ToString(), _errorLog);

            Console.WriteLine();
        }

        private static void CreateAccessDatabase(string connectionString)
        {
            var db = new ADOX.Catalog();
            db.Create(connectionString);
            db.Tables.Append(_logFileHandler.GetTable());

            // get active connection if any
            //var connection = db.ActiveConnection as ADODB.Connection;

            // close connection to database if open
            //if (connection != null) connection.Close();

            // release memory
            db = null;
        }

        private static void ParseCommandlineArguments(string[] args)
        {            
            var arguments = new Arguments(args);

            // get target database
            if (arguments["db"] == null)
            {
                Console.WriteLine("Missing 'db' argument. You must specify a target database file.");
                Environment.Exit(1);
            }

            try
            {
                _dbFileName = new FileInfo(arguments["db"]);
            }
            catch (Exception)
            {
                Console.WriteLine("Error in 'db' argument. You must specify a valid target database file.");
                Console.WriteLine("Argument submitted: {0}", arguments["db"]);
                Environment.Exit(1);
            }

            if (_dbFileName.Exists || arguments["overwrite"] != null)
            {
                _dbFileName.Delete();                
            }

            // get apache log file type
            if (arguments["type"] == null || !LogFileType.TryParse(arguments["type"], out _type))
            {
                _type = LogFileType.CommonLogFormat;
            }
            _logFileHandler = LogFileHandlerFactory.Create(_type);

            // get log files
            if (arguments["logs"] == null)
            {
                Console.WriteLine("Missing 'logs' argument. You must specify a directory where the log file are stored.");
                Environment.Exit(1);
            }
            try
            {
                _logs = new DirectoryInfo(arguments["logs"]);               
            }
            catch (Exception)
            {
                Console.WriteLine("Error in 'logs' argument. You must specify a valid directory name.");
                Console.WriteLine("Argument submitted: {0}", arguments["logs"]);
                Environment.Exit(1);
            }
            if (!_logs.Exists || !_logs.GetFiles().Any())
            {
                Console.WriteLine("No log files found at the specified location. Exiting.");
                Console.WriteLine("Argument submitted: {0}", arguments["logs"]);
                Environment.Exit(0);
            }

            _logFilter = arguments["filter"] ?? "*.*";
            _errorLog = new FileInfo(arguments["errorlog"]);
        }
    }
}
