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
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                // presenter.engine.TriggerLaunch();
            }
        }
    }
}
