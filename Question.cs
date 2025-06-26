// Question.cs
using System.Collections.Generic;

namespace CyberSecurity_Chatbot_Part_Three
{
    /// <summary>
    /// Represents a single quiz question with its options and correct answer.
    /// Renamed from QuizQuestion to match MainWindow's usage.
    /// </summary>
    public class Question
    {
        public string Text { get; set; } = string.Empty; // Changed from QuestionText to Text
        public List<string> Options { get; set; } = new();
        public string CorrectAnswer { get; set; } = string.Empty; // Changed from CorrectAnswerIndex to CorrectAnswer (string)
        public string Explanation { get; set; } = string.Empty; // Explanation for the answer
    }
}