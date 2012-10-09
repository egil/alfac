using System.IO;

namespace Assimilated.Alfac.LogHandlers
{
    public interface ILogFileHandler
    {
        ADOX.Table GetTable();
        string LogFormat { get; }
        string TableName { get; }
        string Name { get; }
        LogFileType LogFileType { get; }
        void AddLogFilesToDatabase(FileInfo[] logs, string connectionString, FileInfo errorLogFileName = null);
    }
}
