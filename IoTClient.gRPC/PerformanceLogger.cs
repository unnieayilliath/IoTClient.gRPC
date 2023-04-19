using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTClient.gRPC.Equipment
{
    internal class PerformanceLogger
    {
        /// <summary>
        /// This method writes data to file
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="logs"></param>
        public static void WriteDataToFile(string filePath, List<string> logs)
        {
            try
            {
                // Check if the file exists, and if not, create it
                if (!File.Exists(filePath))
                {
                    using (StreamWriter sw = File.CreateText(filePath))
                    {
                        logs.ForEach(log => sw.WriteLine(log));
                    }
                }
                else
                {
                    // If the file exists, append data to it
                    using (StreamWriter sw = File.AppendText(filePath))
                    {
                        logs.ForEach(log => sw.WriteLine(log));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
