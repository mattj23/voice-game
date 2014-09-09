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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Voice_Game
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ApplicationPresenter presenter;
        public MainWindow()
        {
            InitializeComponent();

            // Create and set the application presenter
            presenter = new ApplicationPresenter(this);
            this.DataContext = presenter;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                if (presenter.engine.GetMode() == GameEngine.GameMode.StandBy)
                {
                    presenter.engine.StartAiming();
                    return;
                }

                if (presenter.engine.GetMode() == GameEngine.GameMode.Aiming && presenter.Settings.ReleaseMethod == 2 )
                {
                    presenter.engine.TriggerLaunch();
                    return;
                }
            }

            if (e.Key == Key.N)
            {
                EnterTestSubject();
                e.Handled = true;
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                // presenter.engine.TriggerLaunch();
            }
        }

        private void EnterTestSubject()
        {
            InputBox.Visibility = System.Windows.Visibility.Visible;
            InputTextBox.Focus();
            InputTextBox.Text = string.Empty;
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            InputBoxFinalize();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            // NoButton Clicked! Let's hide our InputBox.
            InputBox.Visibility = System.Windows.Visibility.Collapsed;

            // Clear InputBox.
            InputTextBox.Text = String.Empty;
        }

        private void InputBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                InputBoxFinalize();
            }
            if (e.Key == Key.Escape)
            {
                InputTextBox.Text = string.Empty;
                InputBox.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void InputBoxFinalize()
        {
            // YesButton Clicked! Let's hide our InputBox and handle the input text.
            InputBox.Visibility = System.Windows.Visibility.Collapsed;

            // Do something with the Input
            String input = InputTextBox.Text;
            if (!string.IsNullOrEmpty(input.Trim()))
            {
                presenter.SubjectId = input;
                Voice_Game.Properties.Settings.Default.LastSubjectId = input;
                Voice_Game.Properties.Settings.Default.Save();
            }
            // Clear InputBox.
            InputTextBox.Text = String.Empty;
        }
    }
}
