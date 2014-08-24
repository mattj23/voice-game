﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;

namespace Voice_Game
{
    [Serializable]
    public class Settings : Notifier
    {
        private double _pitchMinimum;
        private double _pitchMaximum;
        private double _angleMinimum;
        private double _angleMaximum;
        private double _volumeMinimum;
        private double _volumeMaximum;
        private double _stretchMaximum;
        private double _stretchMinimum;
        private double _gravity;
        private double _targetDiameter;
        private double _targetValidDiameter;
        private double _targetX;
        private double _targetY;

        private double _volumeSpan;
        private double _pitchSpan;

        public double TargetValidDiameter
        {
            get { return _targetValidDiameter; }
            set
            {
                _targetValidDiameter = value;
                OnPropertyChanged("TargetValidDiameter");
            }
        }

        public double VolumeSpan
        {
            get { return _volumeSpan; }
            set
            {
                _volumeSpan = value;
                OnPropertyChanged("VolumeSpan");
            }
        }

        public double PitchSpan
        {
            get { return _pitchSpan; }
            set
            {
                _pitchSpan = value;
                OnPropertyChanged("PitchSpan");
            }
        }

        public double TargetShift
        {
            get { return - _targetDiameter / 2.0; }
        }

        public double TargetDiameter
        {
            get { return _targetDiameter; }
            set
            {
                _targetDiameter = value;
                OnPropertyChanged("TargetDiameter");
                OnPropertyChanged("TargetShift");
            }
        }
        public double TargetX
        {
            get { return _targetX; }
            set
            {
                _targetX = value;
                OnPropertyChanged("TargetX");
            }
        }
        public double TargetY
        {
            get { return _targetY; }
            set
            {
                _targetY = value;
                OnPropertyChanged("TargetY");
            }
        }


        public double Gravity
        {
            get { return _gravity; }
            set
            {
                _gravity = value;
                OnPropertyChanged("Gravity");
            }
        }

        public double PitchMinimum
        {
            get { return _pitchMinimum; }
            set
            {
                _pitchMinimum = value;
                OnPropertyChanged("PitchMinimum");
            }
        }

        public double PitchMaximum
        {
            get { return _pitchMaximum; }
            set
            {
                _pitchMaximum = value;
                OnPropertyChanged("PitchMaximum");
            }
        }

        public double AngleMinimum
        {
            get { return _angleMinimum; }
            set
            {
                _angleMinimum = value;
                OnPropertyChanged("AngleMinimum");
            }
        }

        public double AngleMaximum
        {
            get { return _angleMaximum; }
            set
            {
                _angleMaximum = value;
                OnPropertyChanged("AngleMaximum");
            }
        }

        public double VolumeMinimum
        {
            get { return _volumeMinimum; }
            set
            {
                _volumeMinimum = value;
                OnPropertyChanged("VolumeMinimum");
            }
        }

        public double VolumeMaximum
        {
            get { return _volumeMaximum; }
            set
            {
                _volumeMaximum = value;
                OnPropertyChanged("VolumeMaximum");
            }
        }

        public double StretchMaximum
        {
            get { return _stretchMaximum; }
            set
            {
                _stretchMaximum = value;
                OnPropertyChanged("StretchMaximum");
            }
        }

        public double StretchMinimum
        {
            get { return _stretchMinimum; }
            set
            {
                _stretchMinimum = value;
                OnPropertyChanged("StretchMinimum");
            }
        }

        public void SetDefaults()
        {
            this.PitchMaximum = 1000;
            this.PitchMinimum = 75;
            this.AngleMaximum = 90;
            this.AngleMinimum = 0;
            this.StretchMaximum = 1;
            this.StretchMinimum = 0;
            this.VolumeMinimum = -30;
            this.VolumeMaximum = 0;
            this.Gravity = -0.01;
            this.TargetX = 600;
            this.TargetY = 150;
            this.TargetDiameter = 30;
            this.PitchSpan = 200;
            this.VolumeSpan = 20;

        }

        public static void Serialize(string filename, Settings s)
        {
            XmlSerializer ser = new XmlSerializer(typeof(Settings));
            TextWriter writer = new StreamWriter(filename);
            ser.Serialize(writer, s);
            writer.Close();
        }

        public static Settings Deserialize(string filename)
        {
            XmlSerializer ser = new XmlSerializer(typeof(Settings));
            TextReader reader = new StreamReader(filename);
            Settings s = (Settings)ser.Deserialize(reader);
            reader.Close();
            return s;
        }
    }
}
