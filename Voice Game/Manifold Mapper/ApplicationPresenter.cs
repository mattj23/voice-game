using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.ComponentModel;

using Voice_Game;
using Newtonsoft.Json;



namespace Manifold_Mapper
{
    public class ApplicationPresenter : Notifier
    {
        private MainWindow view;
        private Settings _settings;

        private BackgroundWorker worker;

        private WriteableBitmap _bitmap;
        private BitmapImage _image;
        public Settings Settings
        {
            get { return _settings; }
            set
            {
                _settings = value;
                OnPropertyChanged("Settings");
            }
        }
        public WriteableBitmap Bitmap
        {
            get { return _bitmap; }
            set
            {
                _bitmap = value;
                OnPropertyChanged("Bitmap");
            }
        }
        public BitmapImage Image
        {
            get { return _image; }
            set
            {
                _image = value;
                OnPropertyChanged("Image");
            }
        }

        private ConcurrentBag<Tuple<double, double, double, string>> Results;

        System.Diagnostics.Stopwatch clock = new System.Diagnostics.Stopwatch();

        public void ComputeBitmap()
        {
            MemoryStream stream = new MemoryStream(File.ReadAllBytes("Untitled.bmp"));
            BitmapImage img = new BitmapImage();
            img.BeginInit();
            img.StreamSource = stream;
            img.EndInit();
            BitmapSource s = new FormatConvertedBitmap(img, PixelFormats.Rgb24, null, 0);
            Bitmap = new WriteableBitmap(s);

            /*
            uint[] pixels = new uint[1];
            pixels[0] = (uint)((0 << 24) + (0 << 16) + (0 << 8) + 255);
            Bitmap.WritePixels(new Int32Rect(0, 0, 1, 1), pixels, 500 * 4, 0);
             */
        }

        public void ParallelCompute()
        {
            Results = new ConcurrentBag<Tuple<double, double, double, string>>();

            int width = Bitmap.PixelWidth;
            int height = Bitmap.PixelHeight;
            int rawStride = width * PixelFormats.Rgb24.BitsPerPixel;
            
            double angleStep = 2 * (Settings.AngleMaximum - Settings.AngleMinimum) / width;
            double stretchStep = (Settings.StretchMaximum - Settings.StretchMinimum) / height;
            Parallel.For(0, width, (x) =>
                {
                    for (int y = 0; y < height; ++y)
                    {
                        if (y == 500)
                            System.Diagnostics.Debug.WriteLine("Y is 500!");

                            //System.Diagnostics.Debug.WriteLine(string.Format("{0},{1}", x, y));

                        double angle = Settings.AngleMinimum + (angleStep * x);
                        double stretch = Settings.StretchMaximum - (stretchStep * y);

                        GameEngine engine = new GameEngine(Settings, angle, stretch);
                        double fraction = (Math.Abs(engine.cpaOutput) / 600.0);

                        if (fraction > 1)
                            fraction = 1;
                        Color c = new Color();

                        // Normal manifold
                        c.R = (byte)((1 - fraction) * 255);
                        c.G = (byte)((1 - fraction) * 255);
                        if (engine.outcomeOutput == "hit")
                            c.B = 255;

                        if (engine.outcomeOutput == "obstacle")
                        {
                            c.R = 0;
                            c.G = 0;
                            c.B = 0;
                        }

                        int ax = x;
                        int ay = y;

                        Results.Add(new Tuple<double, double, double, string>(angle, stretch, engine.cpaOutput, engine.outcomeOutput));
                        // worker.ReportProgress(0, new Tuple<int, int, Color>(x, y, c));
                        view.Dispatcher.BeginInvoke(new Action( () =>
                            {
                                byte[] pixels = new byte[3];
                                pixels[0] = c.R;
                                pixels[1] = c.G;
                                pixels[2] = c.B;
                                Bitmap.WritePixels(new Int32Rect(ax, ay, 1, 1), pixels, rawStride, 0);
                            }), null);
                        
                        
                    }
                });
            List<string> writeOut = new List<string>();
            writeOut.Add("angle, stretch, cpa, outcome");
            foreach (Tuple<double, double, double, string> t in Results)
            {
                writeOut.Add(string.Format("{0}, {1}, {2}, {3}", t.Item1, t.Item2, t.Item3, t.Item4));
            }
            File.WriteAllLines("solution_manifold.txt", writeOut);
            //Console.Write(string.Join("\n", writeOut));
            view.Close();
        }

        public void StartWorker()
        {
            Results = new ConcurrentBag<Tuple<double, double, double, string>>();
            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            worker.RunWorkerAsync(new Tuple<int,int>(Bitmap.PixelWidth, Bitmap.PixelHeight));
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            List<string> writeOut = new List<string>();
            writeOut.Add("angle, stretch, cpa, outcome");
            foreach (Tuple<double, double, double, string> t in Results)
            {
                writeOut.Add(string.Format("{0}, {1}, {2}, {3}", t.Item1, t.Item2, t.Item3, t.Item4));
            }
            File.WriteAllLines("solution_manifold.txt", writeOut);
            view.Close();

            //clock.Stop();
            //MessageBox.Show(clock.ElapsedMilliseconds.ToString());
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            
            Tuple<int, int, Color> t = e.UserState as Tuple<int, int, Color>;
            int rawStride = Bitmap.PixelWidth * PixelFormats.Rgb24.BitsPerPixel;
            byte[] pixels = new byte[3];
            pixels[0] = t.Item3.R;
            pixels[1] = t.Item3.G;
            pixels[2] = t.Item3.B;
            Bitmap.WritePixels(new Int32Rect(t.Item1, t.Item2, 1, 1), pixels, rawStride, 0);
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            int width = (e.Argument as Tuple<int, int>).Item1;
            int height = (e.Argument as Tuple<int, int>).Item2;

            double angleStep = (Settings.AngleMaximum - Settings.AngleMinimum) / width;
            double stretchStep = (Settings.StretchMaximum - Settings.StretchMinimum) / height;

            for (int x = 0; x < width; ++x)
            {
                for (int y = 0; y < height; ++y)
                {
                    double angle = Settings.AngleMinimum + (angleStep * x);
                    double stretch = Settings.StretchMaximum - (stretchStep * y);

                    GameEngine engine = new GameEngine(Settings, angle, stretch);
                    double fraction = (Math.Abs(engine.cpaOutput) / 600.0);

                    if (fraction > 1)
                        fraction = 1;
                    Color c = new Color();

                    // Normal manifold
                    c.R = (byte)((1-fraction) * 255);
                    c.G = (byte)((1-fraction) * 255);
                    if (engine.outcomeOutput == "hit")
                        c.B = 255;

                    if (engine.outcomeOutput == "obstacle")
                    {
                        c.R = 0;
                        c.G = 0;
                        c.B = 0;
                    }
                    Results.Add(new Tuple<double,double,double,string>(angle, stretch, engine.cpaOutput, engine.outcomeOutput));
                    worker.ReportProgress(0, new Tuple<int, int, Color>(x, y, c));
                }
            }
        }

        public ApplicationPresenter(MainWindow _view)
        {
            view = _view;

            // Load the settings 
            string settingsFile = "settings.json";
            if (!File.Exists(settingsFile))
            {
                throw new Exception("No settings file!");
            }
            Settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(settingsFile));
            if (Settings.Anchor == null)
                Settings.Anchor = new Voice_Game.Vector(60, 75, 0);

            //GameEngine engine = new GameEngine(Settings, 26.9668518038805, 1.80445138558691);

            //System.Diagnostics.Debug.WriteLine(engine.cpaOutput.ToString());
            ComputeBitmap();
            //StartWorker();
            
            clock.Start();
            ParallelCompute();
            //StartWorker();

        }
    }
}
