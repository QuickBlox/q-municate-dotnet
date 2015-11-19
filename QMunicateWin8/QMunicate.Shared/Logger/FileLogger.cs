using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using QMunicate.Core.Logger;
using Quickblox.Sdk.Logger;

namespace QMunicate.Logger
{
    public class FileLogger : ILogger, IQmunicateLogger
    {
        #region Fields

        private const string LogFileName = "Logs.txt";
        private readonly Core.AsyncLock.AsyncLock mutex = new Core.AsyncLock.AsyncLock();

        #endregion

        #region ILogger Members

        public async Task Log(LogLevel logLevel, string message)
        {
#if DEBUG || TEST_RELEASE
            await Log(logLevel.ToString(), message);
#endif
        }

        #endregion

        #region IQmunicateLogger Members

        public async Task Log(QmunicateLogLevel logLevel, string message)
        {
#if DEBUG || TEST_RELEASE
            await Log(logLevel.ToString(), message);
#endif
        }

        #endregion

        #region Private methods

        private async Task Log(string logLevel, string  message)
        {
            try
            {
                await AppendToFile(LogFileName, string.Format("{0} {1}: {2}{3}", DateTime.Now, logLevel, message, Environment.NewLine));
                Debug.WriteLine("{0}: {1}", logLevel, message);
            }
            catch (Exception) { }
        }

        private async Task AppendToFile(string filename, string content)
        {
            byte[] fileBytes = Encoding.UTF8.GetBytes(content.ToCharArray());

            using (await mutex.LockAsync())
            {
                StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists);

                using (var stream = await file.OpenStreamForWriteAsync())
                {
                    stream.Position = stream.Length;
                    stream.Write(fileBytes, 0, fileBytes.Length);
                }
            }
        }

        private async Task<string> ReadFile(string filename)
        {
            StorageFolder local = ApplicationData.Current.LocalFolder;
            Stream stream = await local.OpenStreamForReadAsync(filename);
            string text;

            using (StreamReader reader = new StreamReader(stream))
            {
                text = reader.ReadToEnd();
            }

            return text;
        }

        #endregion
    }
}
