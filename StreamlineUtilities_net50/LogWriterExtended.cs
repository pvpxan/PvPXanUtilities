using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace StreamlineUtilities
{
    // START LogWriterWPF_Class ---------------------------------------------------------------------------------------------------------
    public static class LogWriterExtended
    {
        // This class allows for displaying of messages before they are logged with LogWriterConsole

        // Define the active current window.
        // -----------------------------------------------------------------------
        private static Window getCurrentWindow()
        {
            if (Application.Current == null)
            {
                return null;
            }

            Window currentWindow = null;

            try
            {
                currentWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive);
            }
            catch
            {
                // Nothing can really be done here.
            }

            try
            {
                if (Application.Current.Windows.Count > 0)
                {
                    currentWindow = Application.Current.Windows[0];
                }
            }
            catch
            {
                // Nothing can really be done here.
            }

            return currentWindow;
        }

        private static void displayMessage(string log, MessageBoxImage messageBoxImage, Window window = null)
        {
            string message = log;
            string caption = "Error...";
            switch (messageBoxImage)
            {
                case MessageBoxImage.Error:
                    caption = "Error...";
                    break;

                case MessageBoxImage.Exclamation:
                    caption = "Important...";
                    break;

                case MessageBoxImage.Information:
                    caption = "Information...";
                    break;

                case MessageBoxImage.None:
                    caption = "Message...";
                    break;

                case MessageBoxImage.Question:
                    caption = "Question...";
                    break;

                default:
                    break;
            }

            if (Application.Current == null)
            {
                openMessageBox(message, caption, messageBoxImage, window);
            }
            else
            {
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    openMessageBox(message, caption, messageBoxImage, window);
                });
            }
        }

        private static void openMessageBox(string message, string caption, MessageBoxImage messageBoxImage, Window window = null)
        {
            if (window == null)
            {
                window = getCurrentWindow();
            }

            if (window != null)
            {
                MessageBox.Show(window, message, caption, MessageBoxButton.OK, messageBoxImage);
            }
            else
            {
                MessageBox.Show(message, caption, MessageBoxButton.OK, messageBoxImage);
            }
        }

        // -----------------------------------------------------------------------
        public static void LogDisplay(string log, MessageBoxImage messageBoxImage)
        {
            LogWriter.LogEntry(log);
            displayMessage(log, messageBoxImage);
        }

        public static void LogDisplay(string log, MessageBoxImage messageBoxImage, Window window)
        {
            LogWriter.LogEntry(log);
            displayMessage(log, messageBoxImage, window);
        }

        // -----------------------------------------------------------------------
        public static void ExceptionDisplay(string log, Exception ex, bool showFull)
        {
            LogWriter.Exception(log, ex);
            exceptionDisplay(log, ex, showFull);
        }

        public static void ExceptionDisplay(string log, Exception ex, bool showFull, Window window)
        {
            LogWriter.Exception(log, ex);
            exceptionDisplay(log, ex, showFull, window);
        }

        private static void exceptionDisplay(string log, Exception ex, bool showFull, Window window = null)
        {
            string message = log + Environment.NewLine + Convert.ToString(ex);

            if (showFull == false)
            {
                message = log;
            }

            displayMessage(message, MessageBoxImage.Error, window);
        }

        // TODO: More for this later.
        // -----------------------------------------------------------------------
        //public static void LogFailure()
        //{

        //}
    }
    // END LogWriterWPF_Class -----------------------------------------------------------------------------------------------------------
}
