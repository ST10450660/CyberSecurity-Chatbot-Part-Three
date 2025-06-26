// MainWindow.xaml.cs
using System;
using System.IO;
using System.Media; // For SoundPlayer
using System.Windows; // Required for Thickness, MessageBox, Visibility etc.
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media; // For Brush
using System.Linq;
using System.Collections.Generic;
using System.Reflection; // Added for Assembly
using System.Windows.Media.Imaging; // Added for ImageSource (if needed, but not directly used in these changes)

namespace CyberSecurity_Chatbot_Part_Three
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ChatbotLogic _chatbotLogic; // Made readonly
        private readonly ActivityLogger _activityLogger; // Made readonly
        private readonly QuizManager _quizManager; // Made readonly
        private readonly ReminderManager _reminderManager; // Made readonly
        private readonly List<RadioButton> _quizOptionRadioButtons; // Made readonly

        // Flags for initial user details input from UI
        private bool _awaitingNameInputFromUI = false;
        private bool _awaitingTopicInputFromUI = false;

        // Paths for external assets
        private const string ASCII_GREETING_RESOURCE_NAME = "CyberSecurity_Chatbot_Part_Three.ascii_chatbot.txt"; // Full embedded resource name
        private const string GREETING_AUDIO_FILE_PATH = "Greeting.wav"; // Relative path for playback

        public MainWindow()
        {
            InitializeComponent();
            SetupWPFAppearance();

            // Clear the log display before initializing the logger
            richTextBoxLog.Document.Blocks.Clear();
            _activityLogger = new(
                "chatbot_activity.log",
                richTextBoxLog
            ); // Simplified new expression
            App.CurrentLogger = _activityLogger;

            _quizManager = new(_activityLogger); // Simplified new expression
            _reminderManager = new(_activityLogger); // Simplified new expression
            _chatbotLogic = new(_activityLogger); // Simplified new expression

            _chatbotLogic.SetQuizManager(_quizManager);
            _chatbotLogic.SetReminderManager(_reminderManager);

            // Initialize Quiz Radio Buttons List
            _quizOptionRadioButtons = new() // Simplified new expression
            {
                rbOptionA, rbOptionB, rbOptionC, rbOptionD
            };

            // Initial setup including ASCII art, greeting sound, and user details prompt
            SetupInitialApplicationState();

            // Initial refresh of reminder list and quiz UI reset
            RefreshRemindersListView();
            UpdateQuizUI(); // Call UpdateQuizUI explicitly to set the initial quiz tab state
        }

        /// <summary>
        /// Sets up appearance details and event handlers for WPF controls.
        /// </summary>
        private void SetupWPFAppearance()
        {
            // Event handlers for quiz radio buttons
            rbOptionA.Checked += QuizRadioButton_Checked;
            rbOptionB.Checked += QuizRadioButton_Checked;
            rbOptionC.Checked += QuizRadioButton_Checked;
            rbOptionD.Checked += QuizRadioButton_Checked;
            btnTrue.Checked += QuizRadioButton_Checked;
            btnFalse.Checked += QuizRadioButton_Checked;
        }

        /// <summary>
        /// Handles click events for navigation buttons, switching between tabs.
        /// </summary>
        private void TabButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button clickedButton)
            {
                string tabName = clickedButton.Name.Replace("btn", "tab"); // e.g., btnChat -> tabChat
                TabItem? targetTab = tabControlMain.FindName(tabName) as TabItem;

                if (targetTab is not null) // Simplified null check
                {
                    tabControlMain.SelectedItem = targetTab;
                    HandleTabSelection(targetTab.Header?.ToString() ?? string.Empty); // Trigger specific tab logic, added null coalescing
                    _activityLogger.Log($"Switched to {targetTab.Header} tab via button.", LogLevel.Info);
                }
            }
        }

        /// <summary>
        /// Performs actions specific to the newly selected tab.
        /// </summary>
        /// <param name="tabHeader">The header text of the selected tab.</param>
        private void HandleTabSelection(string tabHeader)
        {
            switch (tabHeader)
            {
                case "Chat":
                    textBoxUserInput.Focus();
                    break;
                case "Quiz":
                    UpdateQuizUI(); // Ensure quiz UI is up-to-date
                    break;
                case "Reminders":
                    txtReminderDescription.Focus(); // Set focus to reminder input
                    RefreshRemindersListView(); // Auto-refresh reminders when tab is selected
                    break;
                case "NLP Tools":
                    NlpInputTextBox.Focus(); // Set focus to NLP input
                    break;
                case "Activity Log":
                    richTextBoxLog.ScrollToEnd(); // Scroll to the end of the log
                    break;
            }
        }

        /// <summary>
        /// Displays a message in the chat display RichTextBox with specified color.
        /// This is the primary method used for all chatbot and user messages.
        /// </summary>
        /// <param name="message">The message text.</param>
        /// <param name="color">The color of the message text.</param>
        private void DisplayChatbotMessage(string message, Brush color)
        {
            Dispatcher.Invoke(() =>
            {
                Paragraph paragraph = new(); // Simplified new expression
                Run run = new(message + Environment.NewLine); // Simplified new expression
                run.Foreground = color;
                paragraph.Inlines.Add(run);
                richTextBoxChatDisplay.Document.Blocks.Add(paragraph);
                richTextBoxChatDisplay.ScrollToEnd();
            });
        }

        /// <summary>
        /// Displays a user message in the chat display RichTextBox with default white color.
        /// </summary>
        /// <param name="message">The message text.</param>
        private void DisplayUserMessage(string message)
        {
            DisplayChatbotMessage(message, Brushes.White);
        }

        /// <summary>
        /// Handles the click event for the Send button in the Chat tab.
        /// </summary>
        private void BtnSend_Click(object sender, RoutedEventArgs e) // Renamed for naming convention
        {
            ProcessUserInput();
        }

        /// <summary>
        /// Handles the KeyDown event for the user input TextBox in the Chat tab, specifically for Enter key.
        /// </summary>
        private void TextBoxUserInput_KeyDown(object sender, KeyEventArgs e) // Renamed for naming convention
        {
            if (e.Key == Key.Enter)
            {
                ProcessUserInput();
                e.Handled = true; // Mark event as handled to prevent further processing
            }
        }

        /// <summary>
        /// Processes the user's input from the chat TextBox, handling initial setup, commands, and general queries.
        /// </summary>
        private void ProcessUserInput()
        {
            string userInput = textBoxUserInput.Text.Trim();
            textBoxUserInput.Clear(); // Clear input field immediately

            if (string.IsNullOrEmpty(userInput))
            {
                DisplayChatbotMessage("Please type something!", Brushes.Red);
                _activityLogger.Log("User attempted to send empty message.", LogLevel.Warning);
                return;
            }

            DisplayUserMessage($"You: {userInput}");
            _activityLogger.Log($"User: {userInput}", LogLevel.UserQuery);

            // Handle application exit command
            if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                DisplayChatbotMessage($"Bye for now, {_chatbotLogic.UserName}! Stay awesome and stay safe online!", Brushes.Red);
                _activityLogger.Log("Chatbot exited.", LogLevel.Info);
                Application.Current.Shutdown();
                return;
            }

            // --- Handle initial user details input ---
            if (_awaitingNameInputFromUI)
            {
                _chatbotLogic.UserName = userInput;
                _activityLogger.Log($"User name set to: {_chatbotLogic.UserName}", LogLevel.Info);
                _awaitingNameInputFromUI = false;
                _awaitingTopicInputFromUI = true; // Now ask for favorite topic
                DisplayChatbotMessage($"\nNice to meet you, {_chatbotLogic.UserName}! Let's become cybersecurity pros together!\nWhat's your favorite topic in cybersecurity? (Passwords, Phishing, VPNs, etc.):", Brushes.Cyan);
                return;
            }
            else if (_awaitingTopicInputFromUI)
            {
                _chatbotLogic.FavoriteTopic = userInput.Trim();
                _activityLogger.Log($"Favorite topic set to: {_chatbotLogic.FavoriteTopic}", LogLevel.Info);
                _chatbotLogic.SaveUserDetails(); // Save details after topic is set
                DisplayChatbotMessage($"Love it! '{_chatbotLogic.FavoriteTopic}' is super important. I'll keep that in mind.", Brushes.Cyan);

                // After setting topic, prompt for feeling if not already responded to
                if (!_chatbotLogic.HasRespondedToFeeling)
                {
                    DisplayChatbotMessage($"\nBefore we get rolling, {_chatbotLogic.UserName}, how do you feel about cybersecurity? Curious, excited, worried, a bit frustrated?", Brushes.Cyan);
                }
                _awaitingTopicInputFromUI = false; // Important: Set this to false after processing
                return;
            }

            // --- Process general commands via NLP Logic ---
            string commandResponse = _chatbotLogic.ProcessNlpCommand(userInput);
            if (!string.IsNullOrEmpty(commandResponse))
            {
                DisplayChatbotMessage(commandResponse, Brushes.LightYellow);
                _activityLogger.Log($"Bot (NLP Command): {commandResponse}", LogLevel.BotResponse);

                // Update UI based on NLP command
                if (commandResponse.Contains("Reminder added", StringComparison.OrdinalIgnoreCase) || commandResponse.Contains("marked as completed", StringComparison.OrdinalIgnoreCase) || commandResponse.Contains("deleted", StringComparison.OrdinalIgnoreCase))
                {
                    RefreshRemindersListView();
                }
                if (commandResponse.Contains("Here are your current reminders", StringComparison.OrdinalIgnoreCase))
                {
                    tabControlMain.SelectedItem = tabReminders; // Switch to reminders tab
                }
                if (commandResponse.Contains("full log", StringComparison.OrdinalIgnoreCase) || commandResponse.Contains("summary log", StringComparison.OrdinalIgnoreCase))
                {
                    tabControlMain.SelectedItem = tabActivityLog;
                    _activityLogger.RefreshLogDisplay(); // Ensure log is updated in UI
                }
                if (commandResponse.Contains("Starting a quiz for you", StringComparison.OrdinalIgnoreCase))
                {
                    tabControlMain.SelectedItem = tabQuiz;
                    // The quiz UI update is handled by the quiz manager methods directly (via UpdateQuizUI)
                }
                return;
            }

            // --- Handle user feeling input (only if not already responded to) ---
            if (!_chatbotLogic.HasRespondedToFeeling &&
                (userInput.Contains("curious", StringComparison.OrdinalIgnoreCase) || userInput.Contains("excited", StringComparison.OrdinalIgnoreCase) ||
                 userInput.Contains("worried", StringComparison.OrdinalIgnoreCase) || userInput.Contains("frustrated", StringComparison.OrdinalIgnoreCase) ||
                 userInput.Contains("fine", StringComparison.OrdinalIgnoreCase) || userInput.Contains("good", StringComparison.OrdinalIgnoreCase) ||
                 userInput.Contains("okay", StringComparison.OrdinalIgnoreCase) || userInput.Contains("neutral", StringComparison.OrdinalIgnoreCase)))
            {
                List<ColoredMessage> feelingResponseParts = _chatbotLogic.SetUserFeeling(userInput.ToLower());
                foreach (var part in feelingResponseParts)
                {
                    DisplayChatbotMessage(part.Text, part.Color);
                }
                _chatbotLogic.HasRespondedToFeeling = true;
                // Only prompt for topics if a specific topic wasn't just handled by ProcessNlpCommand
                if (string.IsNullOrEmpty(commandResponse))
                {
                    DisplayChatbotMessage($"\nWhat would you like to chat about, {_chatbotLogic.UserName}? (You can ask about Passwords, Phishing, VPNs, etc.):", Brushes.Cyan);
                }
                return;
            }

            // --- Regular chatbot conversation ---
            List<ColoredMessage> botResponseParts = _chatbotLogic.HandleUserQuery(userInput);
            foreach (var part in botResponseParts)
            {
                DisplayChatbotMessage(part.Text, part.Color);
            }
            _activityLogger.Log($"Bot: {string.Join(Environment.NewLine, botResponseParts.Select(p => p.Text))}", LogLevel.BotResponse);
        }

        /// <summary>
        /// Plays a voice greeting from a WAV file.
        /// </summary>
        private void PlayVoiceGreeting()
        {
            string fullAudioPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, GREETING_AUDIO_FILE_PATH);

            if (File.Exists(fullAudioPath))
            {
                try
                {
                    using (SoundPlayer player = new(fullAudioPath)) // Simplified new expression
                    {
                        player.Play();
                    }
                    _activityLogger.Log($"Greeting audio '{GREETING_AUDIO_FILE_PATH}' played.", LogLevel.Info);
                }
                catch (Exception ex)
                {
                    DisplayChatbotMessage($"[ERROR] Unable to play sound: {ex.Message}", Brushes.Red);
                    _activityLogger.Log($"Error playing greeting sound: {ex.Message}", LogLevel.Error);
                }
            }
            else
            {
                DisplayChatbotMessage($"[ERROR] Voice greeting file '{GREETING_AUDIO_FILE_PATH}' not found. Make sure it's in the application's executable directory.", Brushes.Red);
                _activityLogger.Log($"Greeting sound file '{GREETING_AUDIO_FILE_PATH}' not found.", LogLevel.Warning);
            }
        }

        /// <summary>
        /// Displays the ASCII chatbot image from an embedded resource (ascii_chatbot.txt).
        /// </summary>
        private void DisplayAsciiImage()
        {
            string asciiArt = "";
            try
            {
                Assembly? assembly = Assembly.GetExecutingAssembly();
                if (assembly is null) // Simplified null check
                {
                    DisplayChatbotMessage("[ERROR] Could not get executing assembly for ASCII art.", Brushes.Red);
                    _activityLogger.Log("Error: Could not get executing assembly for ASCII art.", LogLevel.Error);
                    return;
                }

                using (Stream? stream = assembly.GetManifestResourceStream(ASCII_GREETING_RESOURCE_NAME))
                {
                    if (stream is null) // Simplified null check
                    {
                        DisplayChatbotMessage($"[ERROR] Embedded resource '{ASCII_GREETING_RESOURCE_NAME}' (chatbot image) not found. Check 'Build Action' for ascii_chatbot.txt.", Brushes.Red);
                        _activityLogger.Log($"Error: Embedded resource '{ASCII_GREETING_RESOURCE_NAME}' (chatbot image) not found. Check 'Build Action' for ascii_chatbot.txt.", LogLevel.Error);
                        return;
                    }
                    using (StreamReader reader = new(stream)) // Simplified new expression
                    {
                        asciiArt = reader.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                DisplayChatbotMessage($"[ERROR] Could not load ASCII chatbot image from embedded resource: {ex.Message}", Brushes.Red);
                _activityLogger.Log($"Error loading ASCII chatbot image from embedded resource: {ex.Message}", LogLevel.Error);
                return;
            }

            DisplayChatbotMessage(asciiArt, Brushes.LightBlue);
        }

        /// <summary>
        /// Sets up the initial state of the application, including ASCII art, audio, and user details prompt.
        /// </summary>
        private void SetupInitialApplicationState()
        {
            richTextBoxChatDisplay.Document.Blocks.Clear(); // Clear chat display

            DisplayAsciiImage();

            DisplayChatbotMessage("Welcome to the Cybersecurity Awareness Chatbot!", Brushes.Cyan);
            DisplayChatbotMessage("I’m here to help you learn about online safety in a fun and friendly way.", Brushes.Cyan);

            PlayVoiceGreeting();

            // Load user details or prompt for them. This will always ask for the name on startup as per your request.
            _chatbotLogic.LoadUserDetails(); // Removed unnecessary assignment of a value to 'savedDetails'

            // Always start by asking for the user's name
            DisplayChatbotMessage("\nHey there, what’s your name?", Brushes.Green);
            _awaitingNameInputFromUI = true;
            _activityLogger.Log("Application started. Awaiting user name input.", LogLevel.Info);
            textBoxUserInput.Focus();
        }

        // --- Reminders Tab Handlers ---

        /// <summary>
        /// Refreshes the display of reminders in the ListView.
        /// </summary>
        private void RefreshRemindersListView()
        {
            lstReminders.ItemsSource = null; // Clear to force refresh
            lstReminders.ItemsSource = _reminderManager.GetAllReminders();
            SetReminderActionButtonsEnabled(false); // Disable buttons until an item is selected
            _activityLogger.Log("Reminders list UI refreshed.", LogLevel.Info);
        }

        /// <summary>
        /// Handles the click event for the Add Reminder button.
        /// </summary>
        private void BtnAddReminder_Click(object sender, RoutedEventArgs e) // Renamed for naming convention
        {
            string description = txtReminderDescription.Text.Trim();
            DateTime? reminderDate = dpReminderDate.SelectedDate;

            if (string.IsNullOrWhiteSpace(description))
            {
                MessageBox.Show("Please enter a reminder description.", "Input Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _reminderManager.AddReminder(description, reminderDate);
            RefreshRemindersListView();
            txtReminderDescription.Clear();
            dpReminderDate.SelectedDate = null;
            DisplayChatbotMessage($"Reminder added: '{description}'{(reminderDate.HasValue ? $" (Due: {reminderDate.Value.ToShortDateString()})" : "")}", Brushes.LightGreen);
            _activityLogger.Log($"User added reminder: '{description}'", LogLevel.UserQuery);
        }

        /// <summary>
        /// Handles the click event for the Clear Reminder Inputs button.
        /// </summary>
        private void BtnClearReminderInputs_Click(object sender, RoutedEventArgs e) // Renamed for naming convention
        {
            txtReminderDescription.Clear();
            dpReminderDate.SelectedDate = null;
            lstReminders.SelectedItem = null; // Deselect any item
            SetReminderActionButtonsEnabled(false);
        }

        /// <summary>
        /// Handles the SelectionChanged event for the Reminders ListBox, enabling/disabling action buttons.
        /// </summary>
        private void LstReminders_SelectionChanged(object sender, SelectionChangedEventArgs e) // Renamed for naming convention
        {
            SetReminderActionButtonsEnabled(lstReminders.SelectedItem is not null); // Simplified null check
        }

        /// <summary>
        /// Enables or disables the Mark Complete and Delete buttons for reminders.
        /// </summary>
        /// <param name="isEnabled">True to enable, false to disable.</param>
        private void SetReminderActionButtonsEnabled(bool isEnabled)
        {
            btnMarkReminderComplete.IsEnabled = isEnabled;
            btnDeleteReminder.IsEnabled = isEnabled;

            if (isEnabled && lstReminders.SelectedItem is Reminder selectedReminder)
            {
                btnMarkReminderComplete.IsEnabled = !selectedReminder.IsCompleted;
            }
        }

        /// <summary>
        /// Handles the click event for the Mark Reminder Complete button.
        /// </summary>
        private void BtnMarkReminderComplete_Click(object sender, RoutedEventArgs e) // Renamed for naming convention
        {
            if (lstReminders.SelectedItem is Reminder selectedReminder) // Simplified pattern matching
            {
                if (_reminderManager.MarkReminderCompleted(selectedReminder.Id))
                {
                    RefreshRemindersListView();
                    DisplayChatbotMessage($"Reminder marked as completed: '{selectedReminder.Description}'", Brushes.LightGreen);
                    _activityLogger.Log($"User marked reminder complete: '{selectedReminder.Description}'", LogLevel.UserQuery);
                }
                else
                {
                    MessageBox.Show("Could not mark reminder as complete.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            SetReminderActionButtonsEnabled(false); // Reset buttons after action
        }

        /// <summary>
        /// Handles the click event for the Delete Reminder button.
        /// </summary>
        private void BtnDeleteReminder_Click(object sender, RoutedEventArgs e) // Renamed for naming convention
        {
            if (lstReminders.SelectedItem is Reminder selectedReminder) // Simplified pattern matching
            {
                if (MessageBox.Show($"Are you sure you want to delete '{selectedReminder.Description}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    if (_reminderManager.DeleteReminder(selectedReminder.Id))
                    {
                        RefreshRemindersListView();
                        DisplayChatbotMessage($"Reminder deleted: '{selectedReminder.Description}'", Brushes.Orange);
                        _activityLogger.Log($"User deleted reminder: '{selectedReminder.Description}'", LogLevel.UserQuery);
                    }
                    else
                    {
                        MessageBox.Show("Could not delete reminder.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            SetReminderActionButtonsEnabled(false); // Reset buttons after action
        }

        // --- Quiz Tab Logic ---

        /// <summary>
        /// Handles the Checked event for quiz radio buttons to enable the Submit Answer button.
        /// </summary>
        private void QuizRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (_quizManager.QuizInProgress)
            {
                btnSubmitAnswer.Visibility = Visibility.Visible;
                btnSubmitAnswer.IsEnabled = true;
            }
        }

        /// <summary>
        /// Resets the Quiz UI to its initial state (before a quiz starts).
        /// </summary>
        private void ResetQuizUI()
        {
            lblQuestion.Text = "Click 'Start Quiz' to begin!";
            lblFeedback.Text = "";
            lblFeedback.Visibility = Visibility.Collapsed;
            lblScore.Text = "Score: 0";
            lblScore.Visibility = Visibility.Collapsed;

            // Hide and uncheck all option radio buttons
            foreach (var rb in _quizOptionRadioButtons)
            {
                rb.IsChecked = false;
                rb.Visibility = Visibility.Collapsed; // Hide by default
                rb.IsEnabled = true;
            }
            btnTrue.IsChecked = false;
            btnFalse.IsChecked = false;
            btnTrue.IsEnabled = true;
            btnFalse.IsEnabled = true;
            pnlTrueFalse.Visibility = Visibility.Collapsed; // Hide T/F panel by default


            btnSubmitAnswer.Visibility = Visibility.Collapsed;
            btnNextQuestion.Visibility = Visibility.Collapsed;
            btnStartQuiz.Visibility = Visibility.Visible;
            pnlQuizOptions.Visibility = Visibility.Visible; // Ensure the panel is visible for options to show
        }

        /// <summary>
        /// Displays the current quiz question and options.
        /// This method is called by UpdateQuizUI when a question is available.
        /// </summary>
        private void DisplayQuestion()
        {
            Question? currentQ = _quizManager.GetCurrentQuestion();

            if (currentQ is null) // Simplified null check
            {
                // If there's no current question but quiz is in progress, it means the quiz just ended.
                // This path should ideally not be hit if NextQuestion handles the end state correctly.
                // As a fallback, ensure the UI is reset if we somehow get here without a question.
                ResetQuizUI();
                _activityLogger.Log("DisplayQuestion called with no current question, resetting UI.", LogLevel.Warning);
                return;
            }

            lblQuestion.Text = $"Question {_quizManager.CurrentQuestionNumber}/{_quizManager.TotalQuestions}:\n\n" + currentQ.Text;
            lblFeedback.Text = "";
            lblFeedback.Visibility = Visibility.Collapsed;
            btnSubmitAnswer.Visibility = Visibility.Visible;
            btnNextQuestion.Visibility = Visibility.Collapsed;
            lblScore.Text = $"Score: {_quizManager.Score}";
            lblScore.Visibility = Visibility.Visible;

            // Reset radio button states and visibility
            foreach (var rb in _quizOptionRadioButtons)
            {
                rb.IsChecked = false;
                rb.Visibility = Visibility.Collapsed; // Hide by default
                rb.IsEnabled = true;
            }
            btnTrue.IsChecked = false;
            btnFalse.IsChecked = false;
            btnTrue.IsEnabled = true;
            btnFalse.IsEnabled = true;
            pnlTrueFalse.Visibility = Visibility.Collapsed; // Hide T/F panel by default

            if (currentQ.Options is not null && currentQ.Options.Count > 0) // Simplified null and count check
            {
                // Check if it's a True/False question based on options content
                if (currentQ.Options.Count == 2 &&
                    currentQ.Options[0].Equals("True", StringComparison.OrdinalIgnoreCase) &&
                    currentQ.Options[1].Equals("False", StringComparison.OrdinalIgnoreCase))
                {
                    btnTrue.Content = currentQ.Options[0];
                    btnFalse.Content = currentQ.Options[1];
                    pnlTrueFalse.Visibility = Visibility.Visible;
                }
                else // Regular multiple choice
                {
                    for (int i = 0; i < currentQ.Options.Count; i++)
                    {
                        if (i < _quizOptionRadioButtons.Count)
                        {
                            _quizOptionRadioButtons[i].Content = currentQ.Options[i];
                            _quizOptionRadioButtons[i].Visibility = Visibility.Visible; // Make visible
                        }
                    }
                }
            }

            btnSubmitAnswer.IsEnabled = false; // Disable submit until an option is checked
        }

        /// <summary>
        /// Handles the click event for the Start Quiz button in the Quiz tab.
        /// </summary>
        private void BtnStartQuiz_Click(object sender, RoutedEventArgs e) // Renamed for naming convention
        {
            ResetQuizUI(); // Clear previous quiz results and prepare UI for a new quiz
            _quizManager.StartQuiz();
            _activityLogger.Log("WPF Quiz started.", LogLevel.GameEvent);
            UpdateQuizUI(); // Display the first question and update buttons
            DisplayChatbotMessage("Starting a quiz for you! Look at the 'Quiz' tab to begin.", Brushes.Cyan); // Inform in chat
        }

        /// <summary>
        /// Handles the click event for the Submit Answer button in the Quiz tab.
        /// </summary>
        private void BtnSubmitAnswer_Click(object sender, RoutedEventArgs e) // Renamed for naming convention
        {
            string userAnswer = string.Empty;
            Question? currentQ = _quizManager.GetCurrentQuestion();
            if (currentQ is null) // Simplified null check
            {
                MessageBox.Show("Error: No current quiz question available.", "Quiz Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _activityLogger.Log("Attempted to submit answer with no current question.", LogLevel.Error);
                return; // <-- Added return here to prevent NullReferenceException
            }

            // Determine user's selected answer
            if (pnlTrueFalse.Visibility == Visibility.Visible) // True/False question
            {
                if (btnTrue.IsChecked == true)
                {
                    userAnswer = "True";
                }
                else if (btnFalse.IsChecked == true)
                {
                    userAnswer = "False";
                }
            }
            else // Multiple choice question
            {
                foreach (RadioButton rb in _quizOptionRadioButtons)
                {
                    if (rb.IsChecked == true && rb.Visibility == Visibility.Visible)
                    {
                        userAnswer = rb.Content?.ToString() ?? string.Empty;
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(userAnswer))
            {
                MessageBox.Show("Please select an answer!", "No Answer Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool isCorrect = _quizManager.CheckAnswer(userAnswer);

            if (isCorrect)
            {
                lblFeedback.Foreground = Brushes.LightGreen;
                lblFeedback.Text = "Correct!";
            }
            else
            {
                lblFeedback.Foreground = Brushes.OrangeRed;
                lblFeedback.Text = $"Incorrect. The correct answer was: {currentQ.CorrectAnswer}";
            }
            lblFeedback.Visibility = Visibility.Visible;
            lblScore.Text = $"Score: {_quizManager.Score} / {_quizManager.TotalQuestions}"; // Update score display

            btnSubmitAnswer.Visibility = Visibility.Collapsed;
            btnNextQuestion.Visibility = Visibility.Visible; // Show next button
            _activityLogger.Log($"WPF Quiz answer submitted. Correct: {isCorrect}, User Answer: '{userAnswer}', Correct Answer: '{currentQ.CorrectAnswer}'", LogLevel.GameEvent);

            // Disable radio buttons after answer submission
            foreach (var rb in _quizOptionRadioButtons)
            {
                rb.IsEnabled = false;
            }
            btnTrue.IsEnabled = false;
            btnFalse.IsEnabled = false;
        }

        /// <summary>
        /// Handles the click event for the Next Question button in the Quiz tab.
        /// </summary>
        private void BtnNextQuestion_Click(object sender, RoutedEventArgs e) // Renamed for naming convention
        {
            if (_quizManager.QuizInProgress) // Only proceed if a quiz is active
            {
                _quizManager.NextQuestion(); // This advances the internal question index in QuizManager

                if (_quizManager.QuizInProgress) // If QuizManager still says quiz is in progress (i.e., there's a next question)
                {
                    UpdateQuizUI(); // Display the next question
                    _activityLogger.Log($"Advanced to Question {_quizManager.CurrentQuestionNumber} (if available).", LogLevel.GameEvent);
                }
                else // QuizManager's QuizInProgress is now false, meaning the quiz has ended
                {
                    // The QuizManager.EndQuiz() was already called internally by _quizManager.NextQuestion()
                    // So, just get the final message and display results.
                    string finalMessage = _quizManager.EndQuiz(); // Call EndQuiz() to get the final message and ensure state is properly reset
                    ShowQuizResults(finalMessage); // Display the final results UI
                    _activityLogger.Log($"WPF Quiz finished. Final Score displayed: {_quizManager.Score}/{_quizManager.TotalQuestions}", LogLevel.GameEvent);
                }
            }
        }

        /// <summary>
        /// Updates the Quiz UI elements based on the current quiz state.
        /// </summary>
        private void UpdateQuizUI()
        {
            if (_quizManager.QuizInProgress)
            {
                DisplayQuestion(); // Display current question if quiz is active
            }
            // Removed the else { ResetQuizUI(); } block here.
            // ResetQuizUI is now only called when BtnStartQuiz_Click is invoked,
            // allowing results to persist until a new quiz is started.
        }

        /// <summary>
        /// Displays the final quiz results.
        /// </summary>
        /// <param name="finalMessage">The final message string to display.</param>
        private void ShowQuizResults(string finalMessage) // Changed signature to accept message
        {
            // Display final message prominently
            lblQuestion.Text = finalMessage;
            lblQuestion.Foreground = Brushes.LightCyan; // Ensure color is good for visibility

            // Clear and hide per-question feedback
            lblFeedback.Text = "";
            lblFeedback.Visibility = Visibility.Collapsed;

            // Display final score, ensuring it's visible
            lblScore.Text = $"Final Score: {_quizManager.Score} / {_quizManager.TotalQuestions}";
            lblScore.Visibility = Visibility.Visible;
            lblScore.Foreground = Brushes.LightGreen; // Set color for readability

            // Hide all quiz options
            _quizOptionRadioButtons.ForEach(rb => { rb.IsChecked = false; rb.Visibility = Visibility.Collapsed; });
            pnlTrueFalse.Visibility = Visibility.Collapsed; // Hide True/False buttons too

            // Hide action buttons
            btnSubmitAnswer.Visibility = Visibility.Collapsed;
            btnNextQuestion.Visibility = Visibility.Collapsed;

            // Show Start Quiz button to allow a new quiz
            btnStartQuiz.Visibility = Visibility.Visible;

            // Show the MessageBox for immediate user attention
            MessageBox.Show(finalMessage, "Quiz Results", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // --- NLP Tools Tab Handlers ---

        /// <summary>
        /// Handles the click event for the Analyze NLP button.
        /// </summary>
        private void AnalyzeNlp_Click(object sender, RoutedEventArgs e) // Renamed for naming convention
        {
            try
            {
                string textToAnalyze = NlpInputTextBox.Text.Trim(); // Get text from the new NLP input box
                if (string.IsNullOrEmpty(textToAnalyze))
                {
                    NlpOutputTextBlock.Text = "Please enter text for NLP simulation.";
                    NlpOutputTextBlock.Foreground = Brushes.OrangeRed;
                    _activityLogger.Log("NLP simulation attempted with empty input.", LogLevel.Warning);
                    return;
                }

                string nlpAnalysis = _chatbotLogic.PerformNLPSimulation(textToAnalyze);
                NlpOutputTextBlock.Text = nlpAnalysis;
                NlpOutputTextBlock.Foreground = Brushes.LightYellow;
                _activityLogger.Log($"Performed NLP simulation on: '{textToAnalyze}'", LogLevel.Info);
                NlpInputTextBox.Clear();
            }
            catch (Exception ex)
            {
                NlpOutputTextBlock.Text = $"An error occurred during NLP analysis: {ex.Message}\n{ex.StackTrace}";
                NlpOutputTextBlock.Foreground = Brushes.Red;
                _activityLogger.Log($"Error during NLP simulation: {ex.Message}", LogLevel.Error);
            }
        }

        // --- Activity Log Tab Handlers ---

        /// <summary>
        /// Handles the click event for the Clear Display button in the Activity Log tab.
        /// </summary>
        private void BtnClearLogDisplay_Click(object sender, RoutedEventArgs e) // Renamed for naming convention
        {
            _activityLogger.ClearLogDisplay();
        }

        /// <summary>
        /// Handles the click event for the Clear Log File button in the Activity Log tab.
        /// </summary>
        private void BtnClearLogFile_Click(object sender, RoutedEventArgs e) // Renamed for naming convention
        {
            _activityLogger.ClearLogFile();
        }
    }
}
