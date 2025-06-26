// QuizManager.cs
using System;
using System.Collections.Generic;
using System.Linq;

namespace CyberSecurity_Chatbot_Part_Three
{
    /// <summary>
    /// Manages the state and logic for a cybersecurity quiz.
    /// </summary>
    public class QuizManager
    {
        private readonly ActivityLogger _activityLogger;
        private List<Question> _allQuestions; // Pool of all available questions
        private List<Question> _currentQuizQuestions; // The specific questions for the current quiz session
        private int _currentQuestionIndex;

        public int Score { get; private set; }
        public int TotalQuestions { get; private set; }
        public bool QuizInProgress { get; private set; }
        public int CurrentQuestionNumber => _currentQuestionIndex + 1;

        public QuizManager(ActivityLogger logger)
        {
            _activityLogger = logger;
            _allQuestions = LoadQuizQuestions(); // Load all questions initially
            _currentQuizQuestions = new List<Question>();
            ResetQuizState();
        }

        /// <summary>
        /// Loads predefined quiz questions.
        /// This pool contains more questions than are strictly used in a single quiz,
        /// ensuring there are options for specific topic selection.
        /// </summary>
        private List<Question> LoadQuizQuestions()
        {
            return new List<Question>
            {
                new Question
                {
                    Text = "What is 'phishing' in cybersecurity?",
                    Options = new List<string>
                    {
                        "A type of malicious software that encrypts files",
                        "An attempt to trick users into revealing sensitive information",
                        "A physical device used for network security",
                        "A method for encrypting data during transmission"
                    },
                    CorrectAnswer = "An attempt to trick users into revealing sensitive information",
                    Explanation = "Phishing is a social engineering technique where attackers try to trick you into revealing sensitive information (like passwords) by pretending to be a trustworthy entity."
                },
                new Question
                {
                    Text = "What is the primary benefit of using a strong, unique password for each online account?",
                    Options = new List<string>
                    {
                        "It makes your internet connection faster.",
                        "It prevents all types of malware.",
                        "It limits the damage if one account's password is stolen.",
                        "It ensures your physical security."
                    },
                    CorrectAnswer = "It limits the damage if one account's password is stolen.",
                    Explanation = "Using unique passwords prevents a 'domino effect' where one compromised password gives hackers access to all your accounts."
                },
                new Question
                {
                    Text = "What does MFA stand for in cybersecurity?",
                    Options = new List<string>
                    {
                        "Malware Free Access",
                        "Multi-Factor Authentication",
                        "Managed Firewall Appliance",
                        "Mobile Fraud Alert"
                    },
                    CorrectAnswer = "Multi-Factor Authentication",
                    Explanation = "MFA, or Multi-Factor Authentication, adds extra layers of security by requiring more than one method of verification to log in."
                },
                new Question
                {
                    Text = "Which of the following is NOT a common type of malware?",
                    Options = new List<string>
                    {
                        "Virus",
                        "Worm",
                        "Firewall", // This is the correct answer
                        "Ransomware"
                    },
                    CorrectAnswer = "Firewall", // Firewall is a defense, not malware
                    Explanation = "A firewall is a network security system that monitors and controls network traffic, not a type of malware. Viruses, worms, and ransomware are all types of malware."
                },
                 new Question
                {
                    Text = "True or False: A VPN (Virtual Private Network) encrypts your internet traffic, enhancing privacy and security.",
                    Options = new List<string> { "True", "False" },
                    CorrectAnswer = "True",
                    Explanation = "A VPN creates a secure, encrypted tunnel for your internet connection, protecting your data from eavesdropping."
                },
                new Question
                {
                    Text = "True or False: It is safe to click on suspicious links in emails if they appear to be from a known company.",
                    Options = new List<string> { "True", "False" },
                    CorrectAnswer = "False",
                    Explanation = "Even if an email appears to be from a known company, always be suspicious of unexpected links. Phishing attempts often impersonate trusted entities."
                },
                new Question
                {
                    Text = "What is Ransomware?",
                    Options = new List<string> {
                        "Software that optimizes system performance",
                        "Malware that encrypts your files and demands payment for decryption",
                        "A tool for secure online shopping",
                        "An application for data backup"
                    },
                    CorrectAnswer = "Malware that encrypts your files and demands payment for decryption",
                    Explanation = "Ransomware holds your data hostage by encrypting it and demanding a ransom."
                },
                new Question
                {
                    Text = "What does DDoS stand for?",
                    Options = new List<string> {
                        "Data Duplication and Delivery System",
                        "Distributed Denial of Service",
                        "Digital Defense Operating System",
                        "Direct Data Output System"
                    },
                    CorrectAnswer = "Distributed Denial of Service",
                    Explanation = "DDoS attacks overwhelm a system with traffic from multiple sources to deny service."
                },
                new Question
                {
                    Text = "What is the process of converting information into a secret code to prevent unauthorized access?",
                    Options = new List<string> {
                        "Decryption",
                        "Compression",
                        "Encryption",
                        "Encoding"
                    },
                    CorrectAnswer = "Encryption",
                    Explanation = "Encryption scrambles data, making it unreadable without the correct key."
                },
                new Question
                {
                    Text = "What does 'Social Engineering' primarily exploit?",
                    Options = new List<string> {
                        "Software vulnerabilities",
                        "Network hardware flaws",
                        "Human psychology and trust",
                        "Operating system backdoors"
                    },
                    CorrectAnswer = "Human psychology and trust",
                    Explanation = "Social engineering manipulates people into giving up confidential information or performing actions, rather than exploiting technical flaws."
                },
                new Question
                {
                    Text = "What is a 'zero-day' vulnerability?",
                    Options = new List<string> {
                        "A vulnerability that has been known for zero days by security experts.",
                        "A newly discovered software vulnerability that hackers can exploit before developers know about it or can patch it.",
                        "A vulnerability found in software that is less than one year old.",
                        "A bug that only affects systems on their first day of operation."
                    },
                    CorrectAnswer = "A newly discovered software vulnerability that hackers can exploit before developers know about it or can patch it.",
                    Explanation = "A zero-day vulnerability is a critical flaw that is unknown to the vendor, allowing attackers to exploit it without a patch being available."
                },
                new Question
                {
                    Text = "Which practice is best for securing your home Wi-Fi network?",
                    Options = new List<string> {
                        "Using the default router password.",
                        "Disabling encryption on your Wi-Fi.",
                        "Changing the default Wi-Fi password and using WPA2/WPA3 encryption.",
                        "Broadcasting your Wi-Fi network name (SSID)."
                    },
                    CorrectAnswer = "Changing the default Wi-Fi password and using WPA2/WPA3 encryption.",
                    Explanation = "Changing default credentials and using strong encryption (WPA2/WPA3) are fundamental steps to secure your Wi-Fi."
                },
                new Question
                {
                    Text = "What is the purpose of a 'backup' in cybersecurity?",
                    Options = new List<string> {
                        "To slow down your computer for better security.",
                        "To create duplicate copies of your data to recover in case of loss or corruption.",
                        "To analyze network traffic for malicious activity.",
                        "To permanently delete sensitive information from your hard drive."
                    },
                    CorrectAnswer = "To create duplicate copies of your data to recover in case of loss or corruption.",
                    Explanation = "Backups are essential for data recovery in scenarios like hardware failure, accidental deletion, or ransomware attacks."
                }
            };
        }

        private void ResetQuizState()
        {
            _currentQuestionIndex = -1;
            Score = 0;
            QuizInProgress = false;
            TotalQuestions = 0;
            _currentQuizQuestions.Clear();
        }

        /// <summary>
        /// Starts a new quiz session with exactly 13 predefined questions covering key cybersecurity topics
        /// as requested by the user, using existing questions from the pool.
        /// </summary>
        public void StartQuiz()
        {
            if (QuizInProgress)
            {
                _activityLogger.Log("Attempted to start a quiz, but one is already in progress.", LogLevel.Warning);
                return;
            }

            ResetQuizState(); // Ensure a clean slate before starting

            List<Question> tempSelectedQuestions = new List<Question>();
            var random = new Random();

            // Explicitly select 13 questions to match the user's requested topics and desired count.
            // Using FirstOrDefault and null checks to prevent crashes if a question isn't found.

            // Core topics (attempt to get one for each, adding to a temporary list)
            var q1 = _allQuestions.FirstOrDefault(q => q.Text.Contains("phishing") && !q.Text.Contains("safe to click"));
            if (q1 != null) tempSelectedQuestions.Add(q1);

            var q2 = _allQuestions.FirstOrDefault(q => q.Text.Contains("primary benefit of using a strong, unique password"));
            if (q2 != null) tempSelectedQuestions.Add(q2);

            var q3 = _allQuestions.FirstOrDefault(q => q.Text.Contains("Social Engineering") && q.Text.Contains("exploit"));
            if (q3 != null) tempSelectedQuestions.Add(q3);

            var q4 = _allQuestions.FirstOrDefault(q => q.Text.Contains("NOT a common type of malware"));
            if (q4 != null) tempSelectedQuestions.Add(q4);

            var q5 = _allQuestions.FirstOrDefault(q => q.Text.Contains("MFA") && q.Text.Contains("stand for"));
            if (q5 != null) tempSelectedQuestions.Add(q5);

            var q6 = _allQuestions.FirstOrDefault(q => q.Text.Contains("DDoS") && q.Text.Contains("stand for"));
            if (q6 != null) tempSelectedQuestions.Add(q6);

            var q7 = _allQuestions.FirstOrDefault(q => q.Text.Contains("VPN (Virtual Private Network)") && q.Text.Contains("encrypts your internet traffic"));
            if (q7 != null) tempSelectedQuestions.Add(q7);

            var q8 = _allQuestions.FirstOrDefault(q => q.Text.Contains("What is Ransomware?"));
            if (q8 != null) tempSelectedQuestions.Add(q8);

            var q9 = _allQuestions.FirstOrDefault(q => q.Text.Contains("process of converting information into a secret code"));
            if (q9 != null) tempSelectedQuestions.Add(q9);

            var q10 = _allQuestions.FirstOrDefault(q => q.Text.Contains("safe to click on suspicious links"));
            if (q10 != null) tempSelectedQuestions.Add(q10);

            var q11 = _allQuestions.FirstOrDefault(q => q.Text.Contains("brute-force"));
            if (q11 != null) tempSelectedQuestions.Add(q11);

            var q12 = _allQuestions.FirstOrDefault(q => q.Text.Contains("zero-day"));
            if (q12 != null) tempSelectedQuestions.Add(q12);

            var q13 = _allQuestions.FirstOrDefault(q => q.Text.Contains("purpose of a 'backup'"));
            if (q13 != null) tempSelectedQuestions.Add(q13);


            _currentQuizQuestions = tempSelectedQuestions.Distinct().ToList(); // Remove duplicates

            // If we don't have exactly 13 questions from specific selections, fill with random ones
            if (_currentQuizQuestions.Count < 13 && _allQuestions.Count >= 13)
            {
                var remainingToAdd = 13 - _currentQuizQuestions.Count;
                var additionalQuestions = _allQuestions
                    .Except(_currentQuizQuestions) // Exclude already selected questions
                    .OrderBy(q => random.Next())   // Randomize remaining questions
                    .Take(remainingToAdd)          // Take enough to reach 13
                    .ToList();
                _currentQuizQuestions.AddRange(additionalQuestions);
                _activityLogger.Log($"Filled quiz with {additionalQuestions.Count} additional random questions.", LogLevel.Info);
            }
            else if (_currentQuizQuestions.Count < 13 && _allQuestions.Count < 13)
            {
                // If total available questions are less than 13, just use all available unique questions
                _currentQuizQuestions = _allQuestions.Distinct().ToList();
                _activityLogger.Log($"Not enough questions in pool to reach 13. Using all {_currentQuizQuestions.Count} unique questions.", LogLevel.Warning);
            }

            // Shuffle the final list of questions to randomize their order for each quiz
            _currentQuizQuestions = _currentQuizQuestions.OrderBy(q => random.Next()).ToList();

            TotalQuestions = _currentQuizQuestions.Count;

            if (TotalQuestions > 0)
            {
                _currentQuestionIndex = 0;
                Score = 0;
                QuizInProgress = true;
                _activityLogger.Log($"Quiz started with {TotalQuestions} questions.", LogLevel.GameEvent);
            }
            else
            {
                _activityLogger.Log("Failed to start quiz: No questions loaded into the current quiz session.", LogLevel.Error);
                ResetQuizState(); // Ensure quiz is marked as not in progress
            }
        }

        public Question? GetCurrentQuestion()
        {
            if (!QuizInProgress || _currentQuestionIndex < 0 || _currentQuestionIndex >= _currentQuizQuestions.Count)
            {
                return null;
            }
            return _currentQuizQuestions[_currentQuestionIndex];
        }

        public bool CheckAnswer(string userAnswer)
        {
            if (!QuizInProgress)
            {
                _activityLogger.Log("Attempted to check answer when no quiz is active.", LogLevel.Error);
                return false;
            }

            var currentQuestion = GetCurrentQuestion();
            if (currentQuestion == null)
            {
                _activityLogger.Log("Attempted to check answer with no current question.", LogLevel.Error);
                return false;
            }

            bool isCorrect = userAnswer.Equals(currentQuestion.CorrectAnswer, StringComparison.OrdinalIgnoreCase);

            if (isCorrect)
            {
                Score++;
                _activityLogger.Log($"Quiz answer correct for Q{CurrentQuestionNumber}. Score: {Score}", LogLevel.GameEvent);
            }
            else
            {
                _activityLogger.Log($"Quiz answer incorrect for Q{CurrentQuestionNumber}. User: '{userAnswer}', Correct: '{currentQuestion.CorrectAnswer}'", LogLevel.GameEvent);
            }
            return isCorrect;
        }

        public void NextQuestion()
        {
            if (QuizInProgress)
            {
                _currentQuestionIndex++;
                if (_currentQuestionIndex >= TotalQuestions)
                {
                    // Quiz has ended, but don't call EndQuiz() here directly,
                    // let MainWindow.xaml.cs handle the UI update after NextQuestion().
                    QuizInProgress = false; // Mark quiz as ended
                    _activityLogger.Log("All quiz questions processed.", LogLevel.GameEvent);
                }
                else
                {
                    _activityLogger.Log($"Advanced to Question {CurrentQuestionNumber}.", LogLevel.GameEvent);
                }
            }
        }

        /// <summary>
        /// Ends the current quiz, calculates the final score, and returns a congratulatory or encouraging message.
        /// </summary>
        /// <returns>A string message with the quiz results.</returns>
        public string EndQuiz()
        {
            // IMPORTANT: QuizInProgress should already be false if called after all questions are processed.
            // This check prevents displaying results for a non-existent quiz state if called out of sequence.
            if (!QuizInProgress && TotalQuestions == 0) // If quiz was never truly started or had no questions
            {
                _activityLogger.Log("EndQuiz called, but no valid quiz was in progress or had questions.", LogLevel.Warning);
                return "No quiz has been completed or is currently in progress."; // More descriptive
            }

            string result = $"Quiz finished! You scored {Score} out of {TotalQuestions}.";

            if (TotalQuestions > 0)
            {
                if ((double)Score / TotalQuestions >= 0.8) // 80% or higher
                {
                    result += " Great job, cybersecurity pro!";
                }
                else if ((double)Score / TotalQuestions >= 0.5) // 50% to 79%
                {
                    result += " Good effort! Keep learning to stay safe online.";
                }
                else // Below 50%
                {
                    result += " Keep learning to stay safe online! Reviewing key concepts might help.";
                }
            }
            else
            {
                result = "Quiz finished, but there were no questions to score."; // Fallback for 0 questions
            }

            _activityLogger.Log($"Quiz finished. Final Score: {Score}/{TotalQuestions}", LogLevel.GameEvent);
            ResetQuizState(); // Reset state for the next quiz
            return result;
        }
    }
}
