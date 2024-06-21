using System;
using System.Collections.Generic;
using System.IO;

namespace CustomPlugin
{
    class GamePlayMetaDataLogger {

        private string LogPath;
        private List<string> Logs; 
        private bool EnableLogging = true;

        public GamePlayMetaDataLogger(string LogPath)
        {
            this.LogPath = LogPath;
            Logs = new List<string>();
        }

        public void Log(string text) 
        {
            if (EnableLogging)
            { 
                Logs.Add(text);
            }  
        }

        public void ActivateLogger() 
        {
            EnableLogging = true;
        }

        public void DeactivateLogger()
        {
            EnableLogging = false;
        }

        public void SaveLogtoFile()
        {
            using (FileStream fs = new FileStream(LogPath, FileMode.Create))
            {
            }
            foreach (string line in Logs)
            {
                WriteLineToFile(LogPath, line);
            }
        }

        private void WriteLineToFile(string filePath, string line)
        {
            try
            {
                // Append the line to the file or create the file if it doesn't exist
                using (StreamWriter writer = new StreamWriter(filePath, true))
                {
                    writer.WriteLine(line);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }


    }
}

