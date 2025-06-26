// ReminderManager.cs
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json; // For JSON serialization/deserialization

namespace CyberSecurity_Chatbot_Part_Three
{
    /// <summary>
    /// Manages adding, listing, completing, and deleting user reminders.
    /// Reminders are persisted to a JSON file.
    /// </summary>
    public class ReminderManager
    {
        private readonly ActivityLogger _activityLogger;
        private ObservableCollection<Reminder> _reminders; // Use ObservableCollection for UI binding
        private readonly string _remindersFilePath = "reminders.json";
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };

        public ReminderManager(ActivityLogger logger)
        {
            _activityLogger = logger;
            _reminders = new ObservableCollection<Reminder>();
            LoadReminders();
        }

        /// <summary>
        /// Gets an ObservableCollection of all current reminders.
        /// This is suitable for binding directly to UI elements.
        /// </summary>
        /// <returns>An ObservableCollection of Reminder objects.</returns>
        public ObservableCollection<Reminder> GetAllReminders()
        {
            // Always return a new ObservableCollection based on a sorted list
            // to ensure UI updates reflect sorting correctly without re-binding everything.
            return new ObservableCollection<Reminder>(
                _reminders.OrderBy(r => r.IsCompleted)
                          .ThenBy(r => r.DueDate.HasValue ? r.DueDate.Value : DateTime.MaxValue)
                          .ToList()
            );
        }

        /// <summary>
        /// Adds a new reminder.
        /// </summary>
        /// <param name="description">The description of the reminder.</param>
        /// <param name="dueDate">Optional due date for the reminder.</param>
        public void AddReminder(string description, DateTime? dueDate = null)
        {
            int newId = _reminders.Any() ? _reminders.Max(r => r.Id) + 1 : 1;
            Reminder newReminder = new Reminder
            {
                Id = newId,
                Description = description,
                CreationDate = DateTime.Now,
                DueDate = dueDate,
                IsCompleted = false
            };
            _reminders.Add(newReminder); // Add to ObservableCollection
            SaveReminders();
            _activityLogger.Log($"Added reminder: ID {newId}, Description: '{description}'{(dueDate.HasValue ? $" Due: {dueDate.Value.ToShortDateString()}" : "")}", LogLevel.Info);
        }

        /// <summary>
        /// Marks a reminder as completed.
        /// </summary>
        /// <param name="id">The ID of the reminder to mark as complete.</param>
        /// <returns>True if the reminder was found and marked, false otherwise.</returns>
        public bool MarkReminderCompleted(int id)
        {
            var reminder = _reminders.FirstOrDefault(r => r.Id == id);
            if (reminder != null && !reminder.IsCompleted)
            {
                reminder.IsCompleted = true; // Update the property in the collection
                // Since it's an ObservableCollection and we modify an item, UI should update.
                // However, sorting needs a refresh, so SaveReminders then LoadReminders or recreate the collection.
                SaveReminders();
                _activityLogger.Log($"Reminder ID {id} marked completed.", LogLevel.Info);
                return true;
            }
            _activityLogger.Log($"Attempted to mark reminder ID {id} complete, but it was not found or already completed.", LogLevel.Warning);
            return false;
        }

        /// <summary>
        /// Deletes a reminder by its ID.
        /// </summary>
        /// <param name="id">The ID of the reminder to delete.</param>
        /// <returns>True if the reminder was found and deleted, false otherwise.</returns>
        public bool DeleteReminder(int id)
        {
            var reminder = _reminders.FirstOrDefault(r => r.Id == id);
            if (reminder != null)
            {
                _reminders.Remove(reminder); // Remove from ObservableCollection
                SaveReminders();
                _activityLogger.Log($"Reminder ID {id} deleted.", LogLevel.Info);
                return true;
            }
            _activityLogger.Log($"Attempted to delete reminder ID {id}, but it was not found.", LogLevel.Warning);
            return false;
        }

        /// <summary>
        /// Loads reminders from the JSON file.
        /// </summary>
        private void LoadReminders()
        {
            if (File.Exists(_remindersFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_remindersFilePath);
                    var loadedReminders = JsonSerializer.Deserialize<List<Reminder>>(json);
                    if (loadedReminders != null)
                    {
                        // Clear existing and add loaded items to ObservableCollection
                        _reminders.Clear();
                        foreach (var reminder in loadedReminders.OrderBy(r => r.Id))
                        {
                            _reminders.Add(reminder);
                        }
                        _activityLogger.Log("Reminders loaded successfully.", LogLevel.Info);
                    }
                }
                catch (Exception ex)
                {
                    _activityLogger.Log($"Error loading reminders: {ex.Message}", LogLevel.Error);
                }
            }
            else
            {
                _activityLogger.Log("Reminders file not found. Starting with no reminders.", LogLevel.Info);
            }
        }

        /// <summary>
        /// Saves the current list of reminders to the JSON file.
        /// </summary>
        private void SaveReminders()
        {
            try
            {
                string json = JsonSerializer.Serialize(_reminders.ToList(), _jsonSerializerOptions); // Serialize the underlying list
                File.WriteAllText(_remindersFilePath, json);
                _activityLogger.Log("Reminders saved successfully.", LogLevel.Info);
            }
            catch (Exception ex)
            {
                _activityLogger.Log($"Error saving reminders: {ex.Message}", LogLevel.Error);
            }
        }
    }
}