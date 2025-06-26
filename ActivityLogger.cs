// ActivityLogger.cs
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Windows; // ADDED: Required for Thickness

namespace CyberSecurity_Chatbot_Part_Three
{
    public enum LogLevel
    {
        Info,
        Warning,
        Error,
        UserQuery,
        BotResponse,
        GameEvent
    }

    public class ActivityLogger
    {
        private readonly string _logFilePath;
        private readonly RichTextBox _displayBox;
        private readonly Dispatcher _dispatcher;

        public ActivityLogger(string logFilePath, RichTextBox displayBox)
        {
            _logFilePath = logFilePath;
            _displayBox = displayBox;
            _dispatcher = _displayBox.Dispatcher;
            InitializeLogFile();
            LoadExistingLogToDisplay(); // Load existing logs into the RichTextBox on startup
        }

        private void InitializeLogFile()
        {
            try
            {
                if (!File.Exists(_logFilePath))
                {
                    File.Create(_logFilePath).Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Could not initialize log file: {ex.Message}");
            }
        }

        private void LoadExistingLogToDisplay()
        {
            _dispatcher.Invoke(() =>
            {
                _displayBox.Document.Blocks.Clear();
                if (File.Exists(_logFilePath))
                {
                    try
                    {
                        var allLines = File.ReadLines(_logFilePath);
                        foreach (var line in allLines)
                        {
                            LogLevel level = LogLevel.Info;
                            foreach (LogLevel l in Enum.GetValues(typeof(LogLevel)))
                            {
                                if (line.Contains($"[{l.ToString().ToUpperInvariant()}]"))
                                {
                                    level = l;
                                    break;
                                }
                            }
                            AppendMessageToRichTextBox(line, GetColorForLogLevel(level));
                        }
                    }
                    catch (Exception ex)
                    {
                        AppendMessageToRichTextBox($"Error loading existing log: {ex.Message}", Brushes.Red);
                    }
                }
            });
        }

        public void Log(string message, LogLevel level)
        {
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level.ToString().ToUpperInvariant()}]: {message}";

            try
            {
                File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Could not write to log file: {ex.Message} - Message: {logEntry}");
            }

            _dispatcher.Invoke(() =>
            {
                AppendMessageToRichTextBox(logEntry, GetColorForLogLevel(level));
            });
        }

        private void AppendMessageToRichTextBox(string message, Brush color)
        {
            Paragraph p = new Paragraph(new Run(message));
            p.Foreground = color;
            p.Margin = new Thickness(0); // This now works with 'using System.Windows;'
            _displayBox.Document.Blocks.Add(p);
            _displayBox.ScrollToEnd();
        }

        private Brush GetColorForLogLevel(LogLevel level)
        {
            return level switch
            {
                LogLevel.Error => Brushes.Red,
                LogLevel.Warning => Brushes.Yellow,
                LogLevel.UserQuery => Brushes.LightBlue,
                LogLevel.BotResponse => Brushes.LightGreen,
                LogLevel.GameEvent => Brushes.Purple,
                _ => Brushes.White,
            };
        }

        public string GetRecentLogSummary()
        {
            try
            {
                if (!File.Exists(_logFilePath))
                {
                    return "No activity log file found yet.";
                }
                var lastLines = File.ReadLines(_logFilePath).Reverse().Take(10).ToList();
                if (!lastLines.Any())
                {
                    return "Activity log is empty.";
                }
                lastLines.Reverse();
                return "--- Recent Activity Log Summary ---\n" + string.Join("\n", lastLines);
            }
            catch (Exception ex)
            {
                return $"Error reading log summary: {ex.Message}";
            }
        }

        public void ClearLogDisplay()
        {
            _dispatcher.Invoke(() =>
            {
                _displayBox.Document.Blocks.Clear();
                Log("Activity log display cleared.", LogLevel.Info);
            });
        }

        public void ClearLogFile()
        {
            try
            {
                File.WriteAllText(_logFilePath, string.Empty);
                _dispatcher.Invoke(() =>
                {
                    _displayBox.Document.Blocks.Clear();
                });
                Log("Activity log file cleared.", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Log($"ERROR: Could not clear log file: {ex.Message}", LogLevel.Error);
            }
        }

        public void RefreshLogDisplay()
        {
            LoadExistingLogToDisplay();
        }
    }
}