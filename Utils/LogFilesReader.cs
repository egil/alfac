using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Assimilated.Alfac.Utils
{
    public static class LogFilesReader
    {
        public static IEnumerable<string> GetEntries(FileInfo logFile)
        {
            // stop iterating if file does not exist
            if (!logFile.Exists)
            {
                Logger.Warning("Log file does not exist. {0}", logFile.FullName);
                yield break;
            }

            string logFileName = logFile.FullName;
            var isCompressed = false;

            // Create a filestreme to the file and test if is compressed
            using (var logFileStream = File.OpenRead(logFileName))
            {
                if (!logFileStream.CanRead)
                {
                    Logger.Warning("Unable to open log file for reading: {0}", logFileName);
                }

                // Check if it is a compressed file (detect .gz files)
                isCompressed = CheckSignature(logFileStream, 3, "1F-8B-08");

                // decompress the log file if it is in a .gz or .zip file
                if (isCompressed)
                {
                    Logger.Info("Decompressing: {0}", logFile.FullName);
                    logFileName = DecompressFile(logFile);
                }
            }

            using (var logFileStream = File.OpenRead(logFileName))
            {
                using (var reader = new StreamReader(logFileStream))
                {
                    string entry;
                    // process all lines in file
                    while ((entry = reader.ReadLine()) != null)
                    {
                        yield return entry;
                    }
                }
            }

            // clean up after iterating. Remove decompressed file after usage
            if (isCompressed)
            {
                File.Delete(logFileName);
            }
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

        private static bool CheckSignature(FileStream fs, int signatureSize, string expectedSignature)
        {
            // jump to the begining of the stream
            fs.Seek(0, SeekOrigin.Begin);

            //if (fs.Length < signatureSize) return false;
            byte[] signature = new byte[signatureSize];
            int bytesRequired = signatureSize;
            int index = 0;
            while (bytesRequired > 0)
            {
                int bytesRead = fs.Read(signature, index, bytesRequired);
                if (bytesRead == 0) break;
                bytesRequired -= bytesRead;
                index += bytesRead;
            }

            // convert to string for comparison
            var actualSignature = BitConverter.ToString(signature);

            return actualSignature == expectedSignature;
        }
    }
}
