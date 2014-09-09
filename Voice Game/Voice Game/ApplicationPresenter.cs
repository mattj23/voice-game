using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Windows;

using Newtonsoft.Json;

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
        private double _semitone;
        private string _subjectId;

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
            get 
            {
                // If we're not using semitones, we can simply return the frequency
                if (!Settings.UseSemitones)
                    return Frequency.ToString("0.0") + " Hz";

                return string.Format("Semitone: {0:0.0} ({1:0.0} Hz)", Semitone, Frequency);

            }
        }
        public string DecibelLabel
        {
            get { return Decibels.ToString("0.0") + " dB"; }
        }
        public string SubjectId
        {
            get { return _subjectId; }
            set
            {
                _subjectId = value;
                OnPropertyChanged("SubjectId");
                OnPropertyChanged("SubjectLabel");
            }
        }
        public string SubjectLabel
        {
            get { return string.Format("Subject: {0}", _subjectId); }
        }
        public double Semitone
        {
            get { return _semitone; }
            set
            {
                _semitone = value;
                OnPropertyChanged("Semitone");
                OnPropertyChanged("FrequencyLabel");
            }
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
            string settingsFile = "settings.json";
            Settings = new Settings();
            if (!File.Exists(settingsFile))
            {
                Settings.SetDefaults();
                string contents = JsonConvert.SerializeObject(Settings, Formatting.Indented);
                File.WriteAllText(settingsFile, contents);
            }
            Settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(settingsFile));
            //Settings = Settings.Deserialize(settingsFile);

            // Load the subject ID from the defaults
            SubjectId = Voice_Game.Properties.Settings.Default.LastSubjectId;

            // Launch program
            Ball = new Vector();
            Anchor = new Vector();
            engine = new GameEngine(this);
        }
    }
}
