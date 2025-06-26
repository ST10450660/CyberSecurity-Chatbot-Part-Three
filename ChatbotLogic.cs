// ChatbotLogic.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace CyberSecurity_Chatbot_Part_Three
{
    /// <summary>
    /// Represents structured information about a cybersecurity topic, including
    /// an initial explanation, practical tips, and detailed explanations for deeper dives.
    /// </summary>
    public class TopicInfo
    {
        public string InitialExplanation { get; set; } = string.Empty;
        public List<string> Tips { get; set; } = new();
        public List<string> DetailedExplanations { get; set; } = new();
    }

    /// <summary>
    /// Represents a part of a chatbot message with its associated color.
    /// </summary>
    public class ColoredMessage
    {
        public string Text { get; set; }
        public Brush Color { get; set; }

        public ColoredMessage(string text, Brush color)
        {
            Text = text;
            Color = color;
        }
    }

    /// <summary>
    /// Core logic for the cybersecurity chatbot, handling user queries,
    /// managing conversational state, and integrating with other managers (Quiz, Reminder, Logger).
    /// </summary>
    public partial class ChatbotLogic
    {
        private readonly ActivityLogger _activityLogger;
        private QuizManager? _quizManager;
        private ReminderManager? _reminderManager;


        public string UserName { get; set; } = string.Empty;
        public string FavoriteTopic { get; set; } = string.Empty;

        public bool AwaitingNameInput { get; set; } = false;
        public bool AwaitingTopicInput { get; set; } = false;
        public bool HasRespondedToFeeling { get; set; } = false;
        private string _userFeeling = string.Empty;

        private readonly Dictionary<string, TopicInfo> _cybersecurityTopics;
        private string? _currentTopicDiscussed;
        private int _currentExplanationIndex;
        private bool _awaitingMoreQuestionsConfirmation;

        private readonly List<string> _availableTopics;

        private const string UserDetailsFileName = "userDetails.json";

        private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };

        // Define a set of colors for tips to cycle through
        private readonly Brush[] _tipColors = {
            Brushes.Pink,
            Brushes.LightPink,
            Brushes.HotPink,
            Brushes.DeepPink
        };

        public ChatbotLogic(ActivityLogger logger)
        {
            _activityLogger = logger;
            _cybersecurityTopics = new(StringComparer.OrdinalIgnoreCase);
            LoadCybersecurityTopics();
            _availableTopics = _cybersecurityTopics.Keys.ToList();
            ResetTopicExplanationState();
        }

        public void SetQuizManager(QuizManager quizManager)
        {
            _quizManager = quizManager;
        }

        public void SetReminderManager(ReminderManager reminderManager)
        {
            _reminderManager = reminderManager;
        }

        private void ResetTopicExplanationState()
        {
            _currentTopicDiscussed = null;
            _currentExplanationIndex = 0;
            _awaitingMoreQuestionsConfirmation = false;
        }

        public void SetUserDetails(string userName, string favoriteTopic)
        {
            UserName = userName;
            FavoriteTopic = favoriteTopic;
            AwaitingNameInput = false;
            AwaitingTopicInput = false;
            HasRespondedToFeeling = false;
            ResetTopicExplanationState();
        }

        public List<ColoredMessage> SetUserFeeling(string feeling)
        {
            _userFeeling = feeling;
            HasRespondedToFeeling = true;
            ResetTopicExplanationState();
            return GetResponseToFeeling();
        }

        public List<ColoredMessage> GetResponseToFeeling()
        {
            string responseText;
            if (string.IsNullOrEmpty(_userFeeling))
            {
                responseText = "I appreciate you sharing that! How can I help you explore cybersecurity?";
            }
            else
            {
                responseText = _userFeeling.ToLowerInvariant() switch
                {
                    var f when f.Contains("curious", StringComparison.OrdinalIgnoreCase) || f.Contains("excited", StringComparison.OrdinalIgnoreCase)
                        => $"That's fantastic, {UserName}! Curiosity is the best way to learn. What topic sparks your interest the most?",
                    var f when f.Contains("worried", StringComparison.OrdinalIgnoreCase) || f.Contains("frustrated", StringComparison.OrdinalIgnoreCase) || f.Contains("overwhelmed", StringComparison.OrdinalIgnoreCase)
                        => $"I understand, {UserName}. Cybersecurity can feel overwhelming. Don't worry, I'm here to simplify it and help you feel more confident. What's bothering you the most?",
                    var f when f.Contains("fine", StringComparison.OrdinalIgnoreCase) || f.Contains("good", StringComparison.OrdinalIgnoreCase) || f.Contains("okay", StringComparison.OrdinalIgnoreCase) || f.Contains("neutral", StringComparison.OrdinalIgnoreCase)
                        => $"Alright, {UserName}! Let's make learning about cybersecurity engaging and valuable for you. What would you like to dive into?",
                    _ => $"Thanks for sharing, {UserName}! No matter how you feel, learning about cybersecurity is always a smart move. How can I help you today?"
                };
            }
            return new List<ColoredMessage> { new ColoredMessage(responseText, Brushes.Green) };
        }

        private void LoadCybersecurityTopics()
        {
            _cybersecurityTopics["password"] = new()
            {
                InitialExplanation = "A strong password is your first line of defense online. It should be long, complex, and unique for each account.",
                Tips = new()
                {
                    "Aim for at least 12-16 characters, mixing uppercase, lowercase, numbers, and symbols.",
                    "Use a unique password for every account. If one is compromised, others remain safe.",
                    "Consider a 'passphrase' – a sentence that's easy for you to remember but hard to guess.",
                    "Use a reputable password manager to generate and securely store complex passwords."
                },
                DetailedExplanations = new()
                {
                    "1. Length & Complexity: A strong password isn't just a word; it's a string of characters difficult for others to guess or for computers to crack through brute-force attacks. The longer and more varied it is (mixing letters, numbers, symbols), the stronger it becomes.",
                    "2. Uniqueness: A strong password is one that is used for only one account. Reusing passwords is like having one key for your house, car, and office – if a hacker gets that one key, they have access to everything. Unique passwords limit the damage of a breach.",
                    "3. Memorability vs. Security: While you need to remember your password, making it too simple for memorization often makes it insecure. The best approach is to use a password manager, which remembers complex, unique passwords for you, or to create a 'passphrase' – a sentence that's long and unique but easy for you to recall."
                }
            };
            _cybersecurityTopics["passwords"] = _cybersecurityTopics["password"];

            _cybersecurityTopics["phishing"] = new()
            {
                InitialExplanation = "Phishing is a type of online fraud where attackers try to trick you into revealing sensitive information by pretending to be a trustworthy entity.",
                Tips = new()
                {
                    "Always verify the sender's email address and look for inconsistencies.",
                    "Hover over links before clicking to see the true URL. If it looks suspicious, don't click!",
                    "Be wary of urgent or threatening language demanding immediate action.",
                    "Never provide sensitive information in response to unsolicited emails or messages."
                },
                DetailedExplanations = new()
                {
                    "1. Impersonation & Deception: Phishing is fundamentally about trickery. Attackers impersonate legitimate organizations (like banks, tech companies, or government agencies) to deceive you into believing their communication is genuine, thereby lowering your guard.",
                    "2. Data Theft: The primary goal of most phishing attacks is to steal sensitive information such as usernames, passwords, credit card numbers, or other personal data. This data can then be used for identity theft, financial fraud, or to gain access to your accounts.",
                    "3. Social Engineering Tactic: Phishing is a form of social engineering, meaning it exploits human psychology (like fear, urgency, curiosity, or desire for a good deal) rather than technical vulnerabilities. It preys on your trust and willingness to comply."
                }
            };

            _cybersecurityTopics["vpn"] = new()
            {
                InitialExplanation = "A VPN (Virtual Private Network) creates a secure, encrypted connection over a less secure network, like the internet, protecting your online privacy.",
                Tips = new()
                {
                    "Use a VPN when connecting to public Wi-Fi to protect your data from potential eavesdropping.",
                    "Choose a reputable VPN provider with a strong no-logs policy.",
                    "Ensure your VPN is always active when you want to protect your privacy.",
                    "Remember, a VPN encrypts your connection but doesn't protect you from malware or phishing if you click malicious links."
                },
                DetailedExplanations = new()
                {
                    "1. Encrypted Tunnel: A VPN works by creating an encrypted 'tunnel' between your device and a VPN server. All your internet traffic passes through this tunnel, making it unreadable to anyone trying to intercept it, like your Internet Service Provider (ISP) or hackers on public Wi-Fi.",
                    "2. IP Address Masking: When you use a VPN, your real IP address is hidden and replaced with the IP address of the VPN server. This makes it much harder for websites, advertisers, or other entities to track your online activities back to your physical location or identity.",
                    "3. Secure Remote Access: Beyond personal privacy, VPNs are widely used by businesses to provide secure remote access to their internal networks for employees. This allows staff to work from anywhere while maintaining a secure connection to company resources."
                }
            };
            _cybersecurityTopics["vpns"] = _cybersecurityTopics["vpn"];

            _cybersecurityTopics["malware"] = new()
            {
                InitialExplanation = "Malware is a general term for malicious software designed to damage, disrupt, or gain unauthorized access to computer systems.",
                Tips = new()
                {
                    "Install and regularly update reputable antivirus/anti-malware software.",
                    "Be cautious about opening email attachments from unknown senders.",
                    "Avoid downloading software from untrusted websites or unofficial app stores.",
                    "Regularly back up your important files to an external drive or cloud storage."
                },
                DetailedExplanations = new()
                {
                    "1. Umbrella Term for Threats: Malware is an overarching term for any software intentionally designed to cause harm. It includes various types like viruses (self-replicating), worms (spreads across networks), Trojans (disguises as legitimate software), spyware (monitors activity), and ransomware (encrypts data for ransom).",
                    "2. System Compromise: The primary purpose of malware is to compromise a system's integrity, confidentiality, or availability. This can range from stealing data and spying on users to completely disabling a computer or network.",
                    "3. Delivery Mechanisms: Malware typically spreads through infected email attachments, malicious websites (drive-by downloads), infected USB drives, compromised software downloads, or by exploiting vulnerabilities in operating systems and applications. Understanding these methods helps in prevention."
                }
            };

            _cybersecurityTopics["firewall"] = new()
            {
                InitialExplanation = "A firewall acts as a security barrier between your computer/network and the outside internet, controlling incoming and outgoing traffic.",
                Tips = new()
                {
                    "Ensure your operating system's built-in firewall is always enabled.",
                    "For home networks, your router typically has a built-in hardware firewall – ensure it's configured correctly.",
                    "Regularly review your firewall rules to ensure they are appropriate for your needs.",
                    "Be cautious when prompted to disable your firewall, especially by unknown sources."
                },
                DetailedExplanations = new()
                {
                    "1. Network Traffic Filter: At its simplest, a firewall is a network security system that monitors and controls incoming and outgoing network traffic based on predetermined security rules. It's like a gatekeeper for your network connection.",
                    "2. Protection Against Unauthorized Access: The primary purpose of a firewall is to prevent unauthorized access to your private network or computer from the internet, and to prevent unauthorized outbound connections from your system.",
                    "3. Hardware vs. Software: Firewalls can be implemented as hardware devices (like those built into your home router or dedicated appliances in businesses) or as software running on your computer (like Windows Defender Firewall). Both are crucial layers of defense."
                }
            };

            _cybersecurityTopics["mfa"] = new()
            {
                InitialExplanation = "MFA (Multi-Factor Authentication) adds an extra layer of security to your accounts by requiring two or more verification factors.",
                Tips = new()
                {
                    "Enable MFA on all critical accounts (email, banking, social media, cloud storage).",
                    "Prefer authenticator apps (e.g., Google Authenticator, Authy) over SMS codes, as SMS can be intercepted.",
                    "Keep your recovery codes in a safe, offline place.",
                    "Be wary of 'MFA fatigue' attacks where attackers repeatedly send MFA requests hoping you'll approve by mistake."
                },
                DetailedExplanations = new()
                {
                    "1. Layered Security: MFA goes beyond just a password by requiring additional 'proof' of identity. This significantly increases security because even if a hacker steals your password, they still need a second factor (which they likely don't have) to access your account.",
                    "2. Multiple Authentication Factors: MFA typically combines at least two of these categories: \n    - Something you know: (e.g., password, PIN) \n    - Something you have: (e.g., smartphone for an app code, hardware security key, ATM card) \n    - Something you are: (e.g., fingerprint, facial scan, voice recognition).",
                    "3. Protecting Against Credential Theft: MFA is one of the most effective ways to protect against credential stuffing attacks (where stolen username/password pairs are tried on many sites) and phishing, as it renders stolen passwords useless without the additional factor."
                }
            };
            _cybersecurityTopics["multi-factor authentication"] = _cybersecurityTopics["mfa"];
            _cybersecurityTopics["2fa"] = _cybersecurityTopics["mfa"];

            _cybersecurityTopics["ransomware"] = new()
            {
                InitialExplanation = "Ransomware is a type of malware that encrypts your files or locks your computer, then demands a ransom to restore access.",
                Tips = new()
                {
                    "Regularly back up your important files to an external drive or secure cloud storage.",
                    "Do NOT pay the ransom; there's no guarantee of decryption, and it funds criminal activities.",
                    "Keep your operating system and all software updated to patch vulnerabilities.",
                    "Be extremely cautious with suspicious emails and attachments, as these are common infection vectors."
                },
                DetailedExplanations = new()
                {
                    "1. Data Hostage: Ransomware holds your data hostage by encrypting it with a key only the attacker possesses. It then demands a payment (ransom), usually in cryptocurrency, in exchange for the decryption key, promising to unlock your files.",
                    "2. Disruptive & Financially Damaging: Beyond data loss, ransomware can cause significant operational disruption for individuals and businesses, leading to downtime, lost productivity, and potentially severe financial losses, even if the ransom is paid.",
                    "3. Infection Vectors & Prevention: Ransomware commonly spreads through phishing emails (malicious links or infected attachments), exploiting vulnerabilities in unpatched software, or through malicious advertising. Strong backups, updated software, and user vigilance are the best defenses."
                }
            };

            _cybersecurityTopics["ddos"] = new()
            {
                InitialExplanation = "DDoS (Distributed Denial of Service) is a cyberattack where multiple compromised systems overwhelm a target server or website with traffic.",
                Tips = new()
                {
                    "If a website is unusually slow or unavailable, it might be under a DDoS attack (wait for the service provider to resolve it).",
                    "For website owners, implement DDoS protection services (e.g., Cloudflare) to filter malicious traffic.",
                    "Keep your home network devices secure to prevent them from becoming part of a botnet.",
                    "Be aware that sudden, inexplicable internet slowness could be a sign of a local DDoS attack or network congestion."
                },
                DetailedExplanations = new()
                {
                    "1. Service Disruption: The primary goal of a DDoS attack is to make an online service, website, or network resource unavailable to its legitimate users by overwhelming it with a flood of internet traffic. It's like too many cars trying to use a single lane on a highway, causing a jam.",
                    "2. Distributed Nature: Unlike a simple Denial of Service (DoS) attack from one source, a DDoS attack uses multiple, geographically dispersed compromised computers (often called a 'botnet'). This makes it much harder to block the attack by simply filtering one source IP address.",
                    "3. Resource Exhaustion: DDoS attacks work by exhausting the target's resources – bandwidth, CPU, memory, or network connections. This prevents the server from responding to legitimate requests, effectively taking the service offline."
                }
            };

            _cybersecurityTopics["encryption"] = new()
            {
                InitialExplanation = "Encryption is the process of converting information into a secret code to prevent unauthorized access, making it unreadable without a key.",
                Tips = new()
                {
                    "Always look for 'HTTPS' and a padlock icon in your browser's address bar for secure, encrypted website connections.",
                    "Consider encrypting your hard drive (e.g., BitLocker for Windows, FileVault for macOS) to protect data if your device is lost/stolen.",
                    "Use encrypted messaging apps for sensitive conversations.",
                    "Understand that strong encryption is fundamental to online privacy and security."
                },
                DetailedExplanations = new()
                {
                    "1. Data Scrambling: Encryption transforms readable data (plaintext) into an unreadable format (ciphertext) using an algorithm and a secret key. Without the correct key, the ciphertext appears as gibberish, protecting its confidentiality.",
                    "2. Confidentiality & Integrity: The main purpose of encryption is to ensure confidentiality – only authorized parties can access the information. Some forms of encryption also provide data integrity, ensuring the data hasn't been tampered with during transmission or storage.",
                    "3. Types & Applications: There are two main types: symmetric (same key for encryption and decryption, faster) and asymmetric (public and private keys, used for secure communication like HTTPS). Encryption is used everywhere: secure websites (HTTPS), encrypted messaging, VPNs, disk encryption, and digital signatures."
                }
            };

            _cybersecurityTopics["social engineering"] = new()
            {
                InitialExplanation = "Social engineering is the psychological manipulation of people into performing actions or divulging confidential information, relying on deception rather than technical hacking.",
                Tips = new()
                {
                    "Be skeptical of unsolicited communications, especially those demanding urgent action or personal info.",
                    "Always verify the identity of the person/organization contacting you through official channels.",
                    "Think before you click: don't open suspicious attachments or click unknown links.",
                    "Don't be afraid to say 'no' or to hang up if something feels off."
                },
                DetailedExplanations = new()
                {
                    "1. Human Hacking: Social engineering is often called 'human hacking' because it exploits human psychology, trust, and common behaviors rather than technical vulnerabilities in software or systems. Attackers manipulate victims into making security mistakes.",
                    "2. Deception & Impersonation: Common tactics involve impersonation (pretending to be someone trustworthy like IT support, a bank, or a colleague), pretexting (creating a believable but false scenario to gain information), and baiting (luring victims with tempting offers).",
                    "3. Goal is Information or Access: The ultimate goal is usually to gain access to systems, steal sensitive information (passwords, financial details), or convince the victim to perform an action (like transferring money or installing malware). Phishing, vishing, and smishing are common delivery methods for social engineering attacks."
                }
            };

            _cybersecurityTopics["software updates"] = new()
            {
                InitialExplanation = "Regularly updating your software and operating system is crucial as updates often include security patches that fix vulnerabilities.",
                Tips = new()
                {
                    "Enable automatic updates for your operating system (Windows, macOS, Linux).",
                    "Keep your web browsers (Chrome, Firefox, Edge) and all applications updated.",
                    "Don't ignore update notifications – they often contain critical security fixes.",
                    "Regularly restart your devices to ensure updates are fully applied."
                }
                ,
                DetailedExplanations = new()
                {
                    "1. Patching Vulnerabilities: Software updates frequently contain 'security patches.' These are fixes for newly discovered flaws or weaknesses (vulnerabilities) in the code that hackers could exploit to gain unauthorized access, install malware, or compromise your system. Ignoring updates leaves these doors open.",
                    "2. Improved Performance & Features: Beyond security, updates also bring bug fixes, improve system stability, enhance performance, and often introduce new features or improve existing ones. It's about keeping your software running optimally and securely.",
                    "3. Broad Application: This applies to virtually all software you use: your operating system (Windows, macOS, Linux), web browsers, antivirus programs, office suites (Microsoft Office, LibreOffice), mobile apps, and even firmware for your router or smart devices. Each piece of software is a potential entry point if not kept current."
                }
            };

            _cybersecurityTopics["public wi-fi"] = new()
            {
                InitialExplanation = "Public Wi-Fi networks are convenient but often unsecured. Be cautious when using them for sensitive activities.",
                Tips = new()
                {
                    "Avoid accessing sensitive accounts (banking, email, online shopping) on public Wi-Fi.",
                    "Always use a reputable VPN if you must perform sensitive tasks on public Wi-Fi.",
                    "Ensure your device's firewall is active.",
                    "Disable automatic Wi-Fi connections to unknown networks on your devices."
                },
                DetailedExplanations = new()
                {
                    "1. Lack of Encryption: Many public Wi-Fi networks (especially free ones) do not encrypt the traffic between your device and the hotspot. This means anyone else on the same network using simple tools could 'eavesdrop' on your data, seeing what you're Browse or even sensitive information.",
                    "2. Man-in-the-Middle Attacks: Attackers can set up fake Wi-Fi hotspots (known as 'Evil Twins') that look legitimate. When you connect to these, the attacker intercepts all your traffic, acting as a 'man in the middle' to steal credentials or inject malware.",
                    "3. Malware & Vulnerability Exploitation: Public Wi-Fi environments are prime targets for attackers looking to exploit unpatched software vulnerabilities on connected devices or to distribute malware. The open nature makes it easier for malicious actors to scan for vulnerable devices."
                }
            };
        }

        private List<ColoredMessage> GetConfusionResponse()
        {
            List<ColoredMessage> responseParts = new();
            StringBuilder response = new("I'm still learning! Could you please rephrase that?");
            response.AppendLine("\n\nYou can ask me about topics like:");
            var random = new Random();
            var suggestedTopics = _availableTopics.OrderBy(_ => random.Next()).Take(13).ToList();
            foreach (var topic in suggestedTopics)
            {
                response.AppendLine($"- {topic}");
            }
            responseParts.Add(new ColoredMessage(response.ToString(), Brushes.Orange));

            StringBuilder commands = new();
            commands.AppendLine("\nOr try commands like:");
            commands.AppendLine("- Start quiz / End quiz");
            commands.AppendLine("- List reminders");
            commands.AppendLine("- Add reminder: [description] on [YYYY-MM-DD]");
            commands.AppendLine("- Mark reminder [ID] complete");
            commands.AppendLine("- Delete reminder [ID]");
            commands.AppendLine("- Show full log / Show summary log");
            commands.AppendLine("- Save my details / Reset user details");
            commands.AppendLine("- Perform NLP simulation: [your text]");
            commands.AppendLine("- Hello / Goodbye");
            responseParts.Add(new ColoredMessage(commands.ToString(), Brushes.LightYellow));

            return responseParts;
        }

        public UserDetails? LoadUserDetails()
        {
            if (File.Exists(UserDetailsFileName))
            {
                try
                {
                    string json = File.ReadAllText(UserDetailsFileName);
                    UserDetails? loadedDetails = JsonSerializer.Deserialize<UserDetails>(json);
                    if (loadedDetails != null)
                    {
                        UserName = loadedDetails.UserName;
                        FavoriteTopic = loadedDetails.FavoriteTopic;
                        _activityLogger.Log("User details loaded successfully.", LogLevel.Info);
                        return loadedDetails;
                    }
                }
                catch (Exception ex)
                {
                    _activityLogger.Log($"Error loading user details: {ex.Message}", LogLevel.Error);
                }
            }
            _activityLogger.Log("User details file not found or could not be loaded.", LogLevel.Info);
            return null;
        }

        public void SaveUserDetails()
        {
            try
            {
                UserDetails userDetails = new UserDetails
                {
                    UserName = this.UserName,
                    FavoriteTopic = this.FavoriteTopic
                };
                string json = JsonSerializer.Serialize(userDetails, _jsonSerializerOptions);
                File.WriteAllText(UserDetailsFileName, json);
                _activityLogger.Log("User details saved successfully.", LogLevel.Info);
            }
            catch (Exception ex)
            {
                _activityLogger.Log($"Error saving user details: {ex.Message}", LogLevel.Error);
            }
        }

        public string ResetUserDetails()
        {
            UserName = string.Empty;
            FavoriteTopic = string.Empty;
            AwaitingNameInput = true;
            AwaitingTopicInput = false;
            HasRespondedToFeeling = false;
            ResetTopicExplanationState();

            try
            {
                if (File.Exists(UserDetailsFileName))
                {
                    File.Delete(UserDetailsFileName);
                    _activityLogger.Log("User details file deleted.", LogLevel.Info);
                }
                return "Your user details have been reset. What's your name?";
            }
            catch (Exception ex)
            {
                _activityLogger.Log($"Error deleting user details file: {ex.Message}", LogLevel.Error);
                return $"Error resetting user details: {ex.Message}. Please try again.";
            }
        }

        public string ProcessNlpCommand(string userInput)
        {
            string lowerInput = userInput.ToLowerInvariant();

            // --- Quiz Commands ---
            if (lowerInput.Contains("start quiz"))
            {
                if (_quizManager == null) return "Quiz manager not initialized. Please report this error.";
                _quizManager.StartQuiz();
                return "Starting a quiz for you! Head over to the 'Quiz' tab to begin answering questions.";
            }
            if (lowerInput.Contains("end quiz"))
            {
                if (_quizManager == null) return "Quiz manager not initialized. Please report this error.";
                return _quizManager.EndQuiz();
            }

            // --- Reminder Commands ---
            Match addReminderMatch = Regex.Match(lowerInput, @"add reminder:\s*(.+?)(?:\s+on\s+(\d{4}-\d{2}-\d{2}))?\s*$", RegexOptions.IgnoreCase);
            if (addReminderMatch.Success)
            {
                if (_reminderManager == null) return "Reminder manager not initialized. Please report this error.";
                string description = addReminderMatch.Groups[1].Value.Trim();

                if (string.IsNullOrWhiteSpace(description))
                {
                    return "Please provide a description for the reminder. Example: 'add reminder: call mom on 2025-12-25'.";
                }

                DateTime? dueDate = null;
                if (addReminderMatch.Groups[2].Success)
                {
                    if (DateTime.TryParse(addReminderMatch.Groups[2].Value, out DateTime parsedDate))
                    {
                        dueDate = parsedDate;
                    }
                    else
                    {
                        return "I couldn't understand the date format. Please use YYYY-MM-DD (e.g., 'add reminder: call mom on 2025-12-25').";
                    }
                }
                _reminderManager.AddReminder(description, dueDate);
                return $"Reminder added: '{description}'{(dueDate.HasValue ? $" (Due: {dueDate.Value.ToShortDateString()})" : "")}. Check the 'Reminders' tab!";
            }

            if (lowerInput.Contains("list reminders"))
            {
                if (_reminderManager == null) return "Reminder manager not initialized. Please report this error.";
                var reminders = _reminderManager.GetAllReminders();
                if (!reminders.Any())
                {
                    return "You don't have any reminders yet. Try adding one with 'add reminder: [description]'.";
                }

                StringBuilder sb = new StringBuilder("Here are your current reminders:\n");
                foreach (var r in reminders)
                {
                    string status = r.IsCompleted ? "[COMPLETED]" : "[PENDING]";
                    string dueDate = r.DueDate.HasValue ? $" (Due: {r.DueDate.Value.ToShortDateString()})" : "";
                    sb.AppendLine($"- ID: {r.Id} {status} {r.Description}{dueDate}");
                }
                sb.AppendLine("You can mark one complete with 'mark reminder [ID] complete' or delete with 'delete reminder [ID]'.");
                return sb.ToString();
            }

            Match markCompleteMatch = Regex.Match(lowerInput, @"mark reminder\s+(\d+)\s+complete", RegexOptions.IgnoreCase);
            if (markCompleteMatch.Success)
            {
                if (_reminderManager == null) return "Reminder manager not initialized. Please report this error.";
                if (int.TryParse(markCompleteMatch.Groups[1].Value, out int id))
                {
                    if (_reminderManager.MarkReminderCompleted(id))
                    {
                        return $"Reminder ID {id} marked as completed. Well done!";
                    }
                    else
                    {
                        return $"Couldn't find reminder with ID {id} or it was already completed.";
                    }
                }
                return "I need a valid reminder ID to mark it complete. Example: 'mark reminder 5 complete'.";
            }

            Match deleteReminderMatch = Regex.Match(lowerInput, @"delete reminder\s+(\d+)", RegexOptions.IgnoreCase);
            if (deleteReminderMatch.Success)
            {
                if (_reminderManager == null) return "Reminder manager not initialized. Please report this error.";
                if (int.TryParse(deleteReminderMatch.Groups[1].Value, out int id))
                {
                    if (_reminderManager.DeleteReminder(id))
                    {
                        return $"Reminder ID {id} has been deleted.";
                    }
                    else
                    {
                        return $"Couldn't find reminder with ID {id}.";
                    }
                }
                return "I need a valid reminder ID to delete it. Example: 'delete reminder 3'.";
            }

            // --- User Details Commands ---
            if (lowerInput.Contains("save my details"))
            {
                if (string.IsNullOrEmpty(UserName) || string.IsNullOrEmpty(FavoriteTopic))
                {
                    return "I need your name and favorite topic before I can save your details. Tell me your name, then your favorite topic.";
                }
                SaveUserDetails();
                return "Your current details have been saved!";
            }

            if (lowerInput.Contains("reset user details"))
            {
                return ResetUserDetails();
            }

            // --- Activity Log Commands ---
            if (lowerInput.Contains("show full log") || lowerInput.Contains("activity log"))
            {
                return "Switching to the 'Activity Log' tab to show you the full log. You can also clear it there.";
            }
            if (lowerInput.Contains("show summary log") || lowerInput.Contains("log summary"))
            {
                return _activityLogger.GetRecentLogSummary();
            }
            if (lowerInput.Contains("clear log display"))
            {
                _activityLogger.ClearLogDisplay();
                return "The activity log display has been cleared.";
            }
            if (lowerInput.Contains("clear log file"))
            {
                _activityLogger.ClearLogFile();
                return "The activity log file has been cleared.";
            }

            return string.Empty; // No command handled
        }

        public List<ColoredMessage> HandleUserQuery(string userInput)
        {
            string lowerInput = userInput.ToLowerInvariant();
            List<ColoredMessage> responseParts = new();

            if (_awaitingMoreQuestionsConfirmation)
            {
                if (lowerInput.Contains("yes") || lowerInput.Contains("yep") || lowerInput.Contains("sure") || lowerInput.Contains("continue") || lowerInput.Contains("go on") || lowerInput.Contains("tell me more"))
                {
                    return GetNextDetailedExplanation();
                }
                else if (lowerInput.Contains("no") || lowerInput.Contains("nope") || lowerInput.Contains("stop") || lowerInput.Contains("enough"))
                {
                    ResetTopicExplanationState();
                    responseParts.Add(new ColoredMessage($"Alright, {UserName}! We can always revisit '{_currentTopicDiscussed}' later. Goodbye for now! Stay safe online!", Brushes.Red));
                    return responseParts;
                }
                else
                {
                    responseParts.Add(new ColoredMessage("Please respond with 'yes' or 'no' if you want to continue the explanation.", Brushes.Orange));
                    responseParts.AddRange(GetConfusionResponse());
                    return responseParts;
                }
            }

            if (lowerInput.Contains("hello") || lowerInput.Contains("hi") || lowerInput.Contains("hey"))
            {
                responseParts.Add(new ColoredMessage($"Hello there, {UserName}! How can I assist you with cybersecurity today?", Brushes.Cyan));
                return responseParts;
            }

            if (lowerInput.Contains("bye") || lowerInput.Contains("goodbye"))
            {
                responseParts.Add(new ColoredMessage($"Goodbye, {UserName}! Stay safe online!", Brushes.Red));
                return responseParts;
            }

            foreach (var topicEntry in _cybersecurityTopics)
            {
                if (lowerInput.Contains(topicEntry.Key))
                {
                    ResetTopicExplanationState();
                    _currentTopicDiscussed = topicEntry.Key;
                    _currentExplanationIndex = 0;

                    responseParts.Add(new ColoredMessage(topicEntry.Value.InitialExplanation, Brushes.Purple));

                    if (topicEntry.Value.Tips.Any())
                    {
                        responseParts.Add(new ColoredMessage("\nHere are some quick tips:", Brushes.Pink));
                        for (int i = 0; i < topicEntry.Value.Tips.Count; i++)
                        {
                            Brush tipColor = _tipColors[i % _tipColors.Length];
                            responseParts.Add(new ColoredMessage($"- {topicEntry.Value.Tips[i]}", tipColor));
                        }
                    }

                    // Corrected access: use topicEntry.Value directly here
                    if (topicEntry.Value.DetailedExplanations != null && topicEntry.Value.DetailedExplanations.Any())
                    {
                        responseParts.Add(new ColoredMessage("\nWould you like to know more about this topic? (Yes/No)", Brushes.Cyan));
                        _awaitingMoreQuestionsConfirmation = true;
                    }
                    _activityLogger.Log($"Provided initial explanation for topic: '{topicEntry.Key}'", LogLevel.BotResponse);
                    return responseParts;
                }
            }

            _activityLogger.Log($"Chatbot did not understand user query: '{userInput}'", LogLevel.Info);
            return GetConfusionResponse();
        }

        private List<ColoredMessage> GetNextDetailedExplanation()
        {
            List<ColoredMessage> responseParts = new();

            if (_currentTopicDiscussed != null && _cybersecurityTopics.TryGetValue(_currentTopicDiscussed, out TopicInfo? topicInfo))
            {
                if (topicInfo.DetailedExplanations != null && _currentExplanationIndex < topicInfo.DetailedExplanations.Count)
                {
                    string explanation = topicInfo.DetailedExplanations[_currentExplanationIndex];
                    _currentExplanationIndex++;
                    _activityLogger.Log($"Provided detailed explanation {_currentExplanationIndex} for '{_currentTopicDiscussed}'", LogLevel.BotResponse);

                    responseParts.Add(new ColoredMessage(explanation, Brushes.MediumPurple));

                    if (_currentExplanationIndex < topicInfo.DetailedExplanations.Count)
                    {
                        _awaitingMoreQuestionsConfirmation = true;
                        responseParts.Add(new ColoredMessage("\nAnything else you'd like to know about this, or should we move on? (Yes/No)", Brushes.Cyan));
                    }
                    else
                    {
                        _awaitingMoreQuestionsConfirmation = false;
                        ResetTopicExplanationState();
                        responseParts.Add(new ColoredMessage($"\nThat covers the detailed explanations for '{_currentTopicDiscussed}'. What else can I help you with, {UserName}?", Brushes.Cyan));
                    }
                    return responseParts;
                }
            }
            ResetTopicExplanationState();
            responseParts.Add(new ColoredMessage("I've covered all the details I have on that topic. What else can I help you with?", Brushes.Cyan));
            return responseParts;
        }

        public string PerformNLPSimulation(string textToAnalyze)
        {
            StringBuilder analysis = new StringBuilder();
            string lowerText = textToAnalyze.ToLowerInvariant();

            analysis.AppendLine("Simulated NLP Analysis:");
            analysis.AppendLine($"Original Text: \"{textToAnalyze}\"");
            analysis.AppendLine($"Lowercase Text: \"{lowerText}\"");

            analysis.AppendLine("\n--- Intent Detection ---");
            if (lowerText.Contains("hello") || lowerText.Contains("hi") || lowerText.Contains("hey"))
                analysis.AppendLine("- Detected Intent: Greeting");
            else if (lowerText.Contains("goodbye") || lowerText.Contains("bye") || lowerText.Contains("exit"))
                analysis.AppendLine("- Detected Intent: Farewell");
            else if (lowerText.Contains("start quiz"))
                analysis.AppendLine("- Detected Intent: Start Quiz");
            else if (lowerText.Contains("end quiz"))
                analysis.AppendLine("- Detected Intent: End Quiz");
            else if (lowerText.Contains("add reminder"))
                analysis.AppendLine("- Detected Intent: Add Reminder");
            else if (lowerText.Contains("list reminders"))
                analysis.AppendLine("- Detected Intent: List Reminders");
            else if (lowerText.Contains("mark reminder") && lowerText.Contains("complete"))
                analysis.AppendLine("- Detected Intent: Mark Reminder Complete");
            else if (lowerText.Contains("delete reminder"))
                analysis.AppendLine("- Detected Intent: Delete Reminder");
            else if (lowerText.Contains("save my details"))
                analysis.AppendLine("- Detected Intent: Save User Details");
            else if (lowerText.Contains("reset user details"))
                analysis.AppendLine("- Detected Intent: Reset User Details");
            else if (lowerText.Contains("log") || lowerText.Contains("activity"))
                analysis.AppendLine("- Detected Intent: View Activity Log");
            else if (lowerText.Contains("how are you") || lowerText.Contains("how do you feel"))
                analysis.AppendLine("- Detected Intent: Ask About Bot's State/Feeling");
            else if (lowerText.Contains("tell me more") || lowerText.Contains("go on") || lowerText.Contains("continue"))
                analysis.AppendLine("- Detected Intent: Continue Explanation");
            else if (lowerText.Contains("yes") && _awaitingMoreQuestionsConfirmation)
                analysis.AppendLine("- Detected Intent: Affirmative (in context of explanation)");
            else if (lowerText.Contains("no") && _awaitingMoreQuestionsConfirmation)
                analysis.AppendLine("- Detected Intent: Negative (in context of explanation)");
            else
                analysis.AppendLine("- Detected Intent: General Query / Unrecognized");


            analysis.AppendLine("\n--- Entity Extraction (Topics) ---");
            bool topicFound = false;
            foreach (var topicKey in _cybersecurityTopics.Keys)
            {
                if (lowerText.Contains(topicKey))
                {
                    analysis.AppendLine($"- Identified Topic Entity: '{topicKey}'");
                    topicFound = true;
                }
            }
            if (!topicFound)
            {
                analysis.AppendLine("- No specific cybersecurity topic entity detected.");
            }

            Match reminderIdMatch = Regex.Match(lowerText, @"\b(?:mark|delete)\s+reminder\s+(\d+)\b");
            if (reminderIdMatch.Success)
            {
                analysis.AppendLine($"- Identified Reminder ID: '{reminderIdMatch.Groups[1].Value}'");
            }
            Match reminderDateMatch = Regex.Match(lowerText, @"on\s+(\d{4}-\d{2}-\d{2})");
            if (reminderDateMatch.Success)
            {
                analysis.AppendLine($"- Identified Reminder Date: '{reminderDateMatch.Groups[1].Value}'");
            }

            analysis.AppendLine("\n--- Sentiment Analysis (Basic) ---");
            if (lowerText.Contains("curious") || lowerText.Contains("excited") || lowerText.Contains("good") || lowerText.Contains("happy"))
                analysis.AppendLine("- Detected Sentiment: Positive");
            else if (lowerText.Contains("worried") || lowerText.Contains("frustrated") || lowerText.Contains("overwhelmed") || lowerText.Contains("bad"))
                analysis.AppendLine("- Detected Sentiment: Negative");
            else
                analysis.AppendLine("- Detected Sentiment: Neutral/Unknown");

            return analysis.ToString();
        }
    }
}