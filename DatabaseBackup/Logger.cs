using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DatabaseBackup
{
    public class Logger
    {
        public void LogToFile(string message)
        {
            if (!Directory.Exists("Logs"))
            {
                Directory.CreateDirectory("Logs");
            }

            string path = string.Format("Logs\\Log_{0}.txt", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
            if (!File.Exists(path))
            {
                // Create a file to write to.
                using (StreamWriter textWriter = File.CreateText(path))
                {
                    textWriter.WriteLine(string.Format("[{0}]", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")));
                    textWriter.WriteLine(message);
                }
            }
            else
            {
                // Open the file to read from.
                using (StreamWriter textWriter = File.AppendText(path))
                {
                    textWriter.WriteLine(string.Format("[{0}]", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")));
                    textWriter.WriteLine(message);
                }
            }
        }
    }
}
