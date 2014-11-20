using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Voice_Game
{
    /// <summary>
    /// Interaction logic for InterruptBox.xaml
    /// </summary>
    public partial class InterruptBox : Window
    {
        long count = 0;
        long delay = 0;
        bool closeable = false;
        DispatcherTimer timer = new DispatcherTimer();        
        public InterruptBox(string message, int secondsDelay)
        {
            InitializeComponent();
            blockText.Text = message;
            delay = secondsDelay;
            timer.Tick += timer_Tick;
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Start();
        }

        void timer_Tick(object sender, EventArgs e)
        {
            long remaining = delay - count;
            if (remaining <= 0)
            {
                buttonLabel.Text = "Ok";
                closeButton.IsEnabled = true;
            }
            else
                buttonLabel.Text = string.Format("{0}...", remaining);
            count++;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (delay - count > 0)
                e.Cancel = true;
        }

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


    }
}
