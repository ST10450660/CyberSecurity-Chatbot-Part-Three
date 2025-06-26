// Reminder.cs
using System;

namespace CyberSecurity_Chatbot_Part_Three
{
    /// <summary>
    /// Represents a single user reminder.
    /// </summary>
    public class Reminder
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime CreationDate { get; set; }
        public DateTime? DueDate { get; set; } // Nullable DateTime for optional due dates
        public bool IsCompleted { get; set; }
    }
}