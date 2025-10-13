namespace MagicLeap.SetupTool.Editor
{
    using System;
    using System.IO;
    using UnityEngine;
    using UnityEditor;

    [InitializeOnLoad]
    public class EditorLogAppender
    {
        private static string logFilePath;

        static EditorLogAppender()
        {
            // Initialize log file path
            string logsDirectory = Path.Combine(Application.dataPath, "Logs");
            if (!Directory.Exists(logsDirectory))
            {
                Directory.CreateDirectory(logsDirectory);
            }

            string timeStamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            logFilePath = Path.Combine(logsDirectory, $"EditorLog_{timeStamp}.txt");

            // Append a session start message
            AppendLog("Unity Editor session started at " + DateTime.Now);

            // Register the log message callback
            Application.logMessageReceived += HandleLog;
        }

        private static void HandleLog(string logString, string stackTrace, LogType type)
        {
            AppendLog($"{DateTime.Now}: [{type}] {logString}\n{stackTrace}");
        }

        private static void AppendLog(string message)
        {
            try
            {
                File.AppendAllText(logFilePath, message + Environment.NewLine+ Environment.NewLine);
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to write to log file: " + ex.Message);
            }
        }
    }

}