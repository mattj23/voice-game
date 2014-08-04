using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NAudio;
using NAudio.Wave;
using NAudio.Dsp;

namespace Audio_Processor
{
    public class PitchDetector
    {
        // Internal variables set at initialization time
        private int bins;
        private int m;
        private int samplingRate;

        private Complex[] buffer;

        public int startBin;
        public int endBin;
        public double binSpacing;
        public double startFrequency;
        public double[] intensities;

        public int window = 0;

        public double Frequency;
        public double Volume;
        public double Decibels;

        public delegate void ResultHandler(object sender, PitchDetectArgs e);
        public event ResultHandler ResultsComputed;


        public PitchDetector(int sampleRate, int fftBins = 2048)
        {
            // Check to make sure the bins number is a power of two
            if ((fftBins & (fftBins - 1)) != 0)
            {
                throw new ArgumentException("FFT Length must be a power of 2");
            }

            samplingRate = sampleRate;

            // Compute m
            this.m = (int)Math.Log(fftBins, 2.0);
            this.bins = fftBins;
            buffer = new Complex[bins];
            intensities = new double[bins];

            /* Prepare the intensity buffer.  The intensity buffer is only large
             * enough to contain the frequencies which the human voice falls into,
             * conservatively from 50Hz to 1500Hz.  The startFrequency and startBin
             * parameters store the first bin index of interest and the frequency 
             * which that first bin corresponds to.
             */
            double minF = 75;                       // 50 Hz starting frequency
            double maxF = 1000;                     // 1500 Hz ending frequency
            binSpacing = (double)samplingRate / bins;            // The frequency spacing of each bin
            startBin = (int)(minF / binSpacing);
            endBin = (int)(maxF / binSpacing);
            startFrequency = startBin * binSpacing;
            intensities = new double[endBin - startBin + 1];


            /* Initialize the sound hardware wave device */
            InitializeSoundSampler();
        }

        private void InitializeSoundSampler()
        {
            WaveInEvent soundDevice = new WaveInEvent();
            soundDevice.WaveFormat = new WaveFormat(samplingRate, 1);
            soundDevice.BufferMilliseconds = 40;
            soundDevice.DataAvailable += new EventHandler<WaveInEventArgs>(SoundDevice_DataAvailible);
            soundDevice.StartRecording();
        }

        void SoundDevice_DataAvailible(object sender, WaveInEventArgs e)
        {
            byte[] buffer = e.Buffer;
            int frameCount = buffer.Count() / 2;
            float[] floatBuffer = new float[frameCount];

            int bytesRecorded = e.BytesRecorded;
            WaveBuffer wbuffer = new WaveBuffer(buffer);

            double squareSum = 0;
            for (int i = 0; i < frameCount; i++)
            {
                int temp = (int)wbuffer.ShortBuffer[i];
                float value = temp * 0.000030517578125f;
                squareSum += value * value;
                floatBuffer[i] = value;
            }
            double rms = Math.Sqrt(squareSum / frameCount);

            Volume = rms;
            Decibels = 20 * Math.Log10(Volume);

            Compute(floatBuffer);
            Frequency = GetPeak();

            PitchDetectArgs p = new PitchDetectArgs(Frequency, Decibels);
            if (ResultsComputed != null)
                ResultsComputed(this, p);

        }

        public void Compute(float[] frame)
        {
            int frameCount = frame.Count();
            // Prepare the frame for the FFT

            // Overwrite the buffer
            for (int n = 0; n < bins; ++n)
            {
                buffer[n].X = 0;
                buffer[n].Y = 0;
            }

            /* We can only write until we fill up all of the FFT bins, but because we don't necessarily 
             * know whether there are less bins or less frames, we have to determine the write limit. This
             * same functionality could be accomplished with a test inside the write loops, but that would
             * require a test evaluation each loop, which would be silly for something trying to work as
             * quickly as possible.  I think.  I'm not sure what the JIT compiler's optimizations will do.
             */
            int writeLimit = (frameCount < bins) ? frameCount : bins;


            // Use the hamming window if window == 1
            if (window == 1)
                for (int n = 0; n < writeLimit; ++n)
                    buffer[n].X = (float)(frame[n] * FastFourierTransform.HammingWindow(n, frameCount));

            // Use a rectangular window if window == 0
            if (window == 0)
                for (int n = 0; n < writeLimit; ++n)
                    buffer[n].X = frame[n];

            // Perform the transform
            FastFourierTransform.FFT(true, m, buffer);

            // Compute the intensities
            for (int n = startBin; n <= endBin; ++n)
                intensities[n - startBin] = Math.Sqrt(Math.Pow(buffer[n].X, 2) + Math.Pow(buffer[n].Y, 2));

            
        }

        /// <summary>
        /// Get the highest bin value from the intensities array and return the 
        /// frequency associated with that bin number.
        /// </summary>
        /// <returns></returns>
        public double GetPeak()
        {
            double maxIntensity = 0;
            int maxIntensityBin = 0;
            for (int n = 0; n < intensities.Count(); ++n)
            {
                if (intensities[n] > maxIntensity)
                {
                    maxIntensity = intensities[n];
                    maxIntensityBin = n;
                }
            }

            return maxIntensityBin * binSpacing + startFrequency;
        }

        /// <summary>
        /// Save the intensity spectrum to disk
        /// </summary>
        public void SaveSample()
        {
            string[] lines = new string[intensities.Count()];
            for (int n = 0; n < intensities.Count(); ++n)
                lines[n] = string.Format("{0}\t{1}", n * binSpacing + startFrequency, intensities[n]);
            System.IO.File.WriteAllLines("spectrum.txt", lines);
        }



    }
}
