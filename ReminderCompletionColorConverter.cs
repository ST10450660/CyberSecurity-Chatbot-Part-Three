using System;

using System.Globalization;

using System.Windows.Data;

using System.Windows.Media;



namespace CyberSecurity_Chatbot_Part_Three

{

    public class ReminderCompletionColorConverter : IValueConverter

    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)

        {

            if (value is bool isCompleted)

            {

                // Return Gray for completed, LightGreen for pending

                return isCompleted ? Brushes.Gray : Brushes.LightGreen;

            }

            return Brushes.LightGreen; // Default to LightGreen if value is not a boolean

        }



        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)

        {

            throw new NotImplementedException();

        }

    }

}