using System;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Assimilated.Alfac.LogFileProcessors;
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
        static FileInfo _executionLog;
        static LogFileProcessor _logFileProcessor;
        private static bool _overwrite;

        static void Main(string[] args)
        {
            // read in arguments, parse them for easy consumption
            ParseCommandlineArguments(args);

            // setup log processor
            _logFileProcessor = LogFileProcessor.Create(_type);
            _logFileProcessor.LogFiles = _logs.GetFiles(_logFilter);

            // set up logger
            Logger.DiskLog = _executionLog;

            // Print welcome message
            Logger.Info();
            Logger.Info(" ##################################################");
            Logger.Info(" # Apatche Log Files to Access Database Converter #");
            Logger.Info(" #                                                #");
            Logger.Info(" # Version 0.3                                    #");
            Logger.Info(" # By: Egil Hansen (http://egilhansen.com)        #");
            Logger.Info(" ##################################################");
            Logger.Info();
            Logger.Info("Runtime settings:");
            Logger.Info();
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Logger.Info("  Database File (db) ................... : {0}", _dbFileName.FullName);
            Logger.Info("  Overwrite Existing DB File (overwrite) : {0}", _overwrite.ToString());
            Logger.Info("  Logs Directory (logs) ................ : {0}", _logs.FullName);
            Logger.Info("  Log filter (filter) .................. : {0}", _logFilter);
            Logger.Info("  Log File Type (type) ................. : {0}", _logFileProcessor.Name);
            Logger.Info("  Runetime Log (runtimelog) ............ : {0}", _executionLog != null ? _executionLog.FullName : "n/a");
            Logger.Info();
            Console.ResetColor();

            // create target database based on log file type
            var csb = new OleDbConnectionStringBuilder { DataSource = _dbFileName.FullName, Provider = "Microsoft.Jet.OLEDB.4.0" };

            // remove existing database file
            if (_dbFileName.Exists && _overwrite)
            {
                _dbFileName.Delete();
            }
            
            // create database
            if (_dbFileName.Exists)
            {
                AddTable(csb);
            }
            else
            {
                CreateAccessDatabase(csb.ToString());
            }

            // parse each log file, add each log entry to database
            Logger.Info("Starting log file processing . . .");
            Logger.LogOffscreen(string.Empty);
            Logger.LogOffscreen("Start time: {0}", DateTime.Now);
            Logger.Info();

            _logFileProcessor.Process(csb.ToString());          
        }

        private static void AddTable(OleDbConnectionStringBuilder csb)
        {
            // check if table exists
            var con = new ADODB.Connection();
            con.Open(csb.ToString());
            var db = new ADOX.Catalog();
            db.ActiveConnection = con;
            try
            {
                var table = db.Tables[_logFileProcessor.TableName];
            }
            catch (COMException)
            {
                db.Tables.Append(_logFileProcessor.GetTable());
            }
            finally
            {
                con.Close();
            }
        }

        private static void CreateAccessDatabase(string connectionString)
        {
            var db = new ADOX.Catalog();
            db.Create(connectionString);
            db.Tables.Append(_logFileProcessor.GetTable());

            // get active connection if any
            var connection = db.ActiveConnection as ADODB.Connection;

            // close connection to database if open
            if (connection != null) connection.Close();

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

            _overwrite = arguments["overwrite"] != null;
            
            // get apache log file type
            if (arguments["type"] == null)
            {
                Console.WriteLine("Missing 'type' argument. You must specify the log files type.");                
                Environment.Exit(1);
            }            

            if(!LogFileType.TryParse(arguments["type"], out _type))
            {
                Console.WriteLine("Error in 'type' argument. You must specify a valid log files type.");
                Console.WriteLine("Argument submitted: {0}", arguments["type"]);
                Environment.Exit(1);
            }

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

            if (arguments["runtimelog"] != null)
            {
                try
                {
                    _executionLog = new FileInfo(arguments["runtimelog"]);
                }
                catch (Exception)
                {
                    Console.WriteLine("Error in 'runtimelog' argument. The specified file is not valid.");
                    Console.WriteLine("Argument submitted: {0}", arguments["db"]);
                    Environment.Exit(1);
                }
            }
        }
    }
}
