using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Windows;

namespace Voice_Game
{
    public class ApplicationPresenter : Notifier
    {
        private MainWindow View;
        public GameEngine engine;

        private bool _isAnchorVisible;
        private Vector _ball;
        private Vector _anchor;

        private double _frequency;
        private double _decibels;

        public Visibility AnchorVisibility
        {
            get
            {
                if (IsAnchorVisible)
                    return Visibility.Visible;
                else
                    return Visibility.Hidden;
            }
        }
        public bool IsAnchorVisible
        {
            get { return _isAnchorVisible; }
            set
            {
                _isAnchorVisible = value;
                OnPropertyChanged("IsAnchorVisible");
                OnPropertyChanged("AnchorVisibility");
            }
        }
        public Vector Ball
        {
            get { return _ball; }
            set
            {
                _ball = value;
                OnPropertyChanged("Ball");
            }
        }
        public Vector Anchor
        {
            get { return _anchor; }
            set
            {
                _anchor = value;
                OnPropertyChanged("Anchor");
            }
        }
        public double Frequency
        {
            get { return _frequency; }
            set
            {
                _frequency = value;
                OnPropertyChanged("Frequency");
                OnPropertyChanged("FrequencyLabel");
            }
        }
        public double Decibels
        {
            get { return _decibels; }
            set
            {
                _decibels = value;
                OnPropertyChanged("Decibels");
                OnPropertyChanged("DecibelLabel");
            }
        }
        public string FrequencyLabel
        {
            get { return Frequency.ToString("0.0") + " Hz"; }
        }
        public string DecibelLabel
        {
            get { return Decibels.ToString("0.0") + " dB"; }
        }
        
        // Settings object
        private Settings _settings;

        public Settings Settings
        {
            get { return _settings; }
            set
            {
                _settings = value;
                OnPropertyChanged("Settings");
            }
        }

        public ApplicationPresenter(MainWindow view)
        {
            View = view;

            // Load the settings
            string settingsFile = "settings.xml";
            Settings = new Settings();
            if (!File.Exists(settingsFile))
            {
                Settings.SetDefaults();
                Settings.Serialize(settingsFile, Settings);
            }
            Settings = Settings.Deserialize(settingsFile);



            // Launch program
            Ball = new Vector();
            Anchor = new Vector();
            engine = new GameEngine(this);
        }
    }
}
