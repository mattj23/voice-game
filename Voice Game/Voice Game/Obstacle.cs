using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Voice_Game
{
    public class Obstacle : Notifier
    {
        private double _position;
        private double _top;
        private double _bottom;

        public double Position
        {
            get { return _position; }
            set
            {
                _position = value;
                OnPropertyChanged("Position");
            }
        }
        public double Top
        {
            get { return _top; }
            set
            {
                _top = value;
                OnPropertyChanged("Top");
                OnPropertyChanged("Height");
            }
        }
        public double Bottom
        {
            get { return _bottom; }
            set
            {
                _bottom = value;
                OnPropertyChanged("Bottom");
                OnPropertyChanged("Height");
            }
        }

        [JsonIgnore]
        public double Height
        {
            get { return Top - Bottom; }
        }

        public Obstacle(double top, double bottom, double position)
        {
            this.Top = top;
            this.Bottom = bottom;
            this.Position = position;
        }
    }
}
