// App.xaml.cs
using System.Windows;

namespace CyberSecurity_Chatbot_Part_Three
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // Static property to hold a reference to the ActivityLogger, making it accessible application-wide.
        public static ActivityLogger? CurrentLogger { get; set; }
    }
}
