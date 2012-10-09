using System;

namespace Assimilated.Alfac.LogHandlers
{
    public static class LogFileHandlerFactory
    {
        public static ILogFileHandler Create(LogFileType type)
        {
            switch (type)
            {
                case LogFileType.CombinedLogFormat:
                    return new CombinedLogFormatHandler();
                    break;
                default:
                    throw new ArgumentException("Unkown log file type.");
            }
        }
    }
}
