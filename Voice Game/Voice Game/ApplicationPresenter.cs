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
        // Main infrastructure objects
        private MainWindow View;
        public GameEngine engine;

        // Trial counting
        private DateTime lastTrial;
        private int _trialCounter;

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

        public int TrialCounter
        {
            get { return _trialCounter; }
            set
            {
                _trialCounter = value;
                OnPropertyChanged("TrialCounter");
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

            // Subscribe to trial completed event
            TrialCounter = 0;
            lastTrial = DateTime.Now;
            engine.TrialComplete += engine_TrialComplete;
        }

        /// <summary>
        /// This is the event handler for the game engine's trial complete event.  It fires
        /// when a trial is finished immediately after the trace has been written to the disk
        /// and is intended to allow this portion of the program to track the frequency of
        /// trials and allow the program to notify the user when he/she should take a break.
        /// </summary>
        /// <param name="sender">the game engine object</param>
        /// <param name="e">event arguments, empty for now</param>
        void engine_TrialComplete(object sender, EventArgs e)
        {
            if ((DateTime.Now - lastTrial).TotalSeconds < Settings.BreakSeconds)
                TrialCounter++;
            else
                TrialCounter = 1;
            lastTrial = DateTime.Now;


            if (TrialCounter == (int)(Settings.BreakCount * 1.1) + 1)
            {
                
                string message = String.Format("Seriously, though...take a break.", TrialCounter);
                Application.Current.Dispatcher.BeginInvoke(new Action(() => Interrupt(message, 5))); 

            }
            else if (TrialCounter == Settings.BreakCount)
            {
                string message = String.Format("Great job! You have labored through {0} trials and you deserve a break!", TrialCounter);
                Application.Current.Dispatcher.BeginInvoke(new Action(() => Interrupt(message, 5))); 

            }
        }

        private void Interrupt(string message, int delay)
        {
            InterruptBox interrupt = new InterruptBox(message, delay);
            interrupt.ShowDialog();
        }
    }
}
