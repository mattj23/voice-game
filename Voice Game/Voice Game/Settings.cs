using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;

namespace Voice_Game
{
    [Serializable]
    public class Settings
    {
        public double PitchMinimum;
        public double PitchMaximum;
        public double AngleMinimum;
        public double AngleMaximum;

        public double VolumeMinimum;
        public double VolumeMaximum;
        public double StretchMaximum;
        public double StretchMinimum;

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
