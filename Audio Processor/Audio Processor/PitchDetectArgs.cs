using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Audio_Processor
{
    public class PitchDetectArgs : EventArgs
    {
        public double Frequency { get; set; }
        public double Decibels { get; set; }

        public PitchDetectArgs(double _freq, double _deci)
        {
            Frequency = _freq;
            Decibels = _deci;
        }
    }
}
