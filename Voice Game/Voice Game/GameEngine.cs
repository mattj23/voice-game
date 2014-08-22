using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

using Audio_Processor;

namespace Voice_Game
{
    public class GameEngine
    {
        private ApplicationPresenter presenter;

        // Game Mode enumeration
        enum GameMode {StandBy, Aiming, InFlight, Terminal, PostFlight};

        // Boolean read/write operations are atomic in C#, so this does not need to be 
        // wrapped to be thread-safe
        private bool isRunning = true;

        private bool triggerLaunch = false;
        private bool startAiming = false;

        // Element Position Variables
        private Vector ball = new Vector();
        private Vector anchor = new Vector(60, 75, 0);
        private Vector velocity = new Vector();

        private double angle = 10;
        private double stretch = .5;
        
        // Pitch input objects
        private Object pitchLock = new Object();
        private double Frequency;
        private double Decibels;
        
        // Pitch Reference
        double pitchReference = 0;
        double pitchRange = 200;

        // Voice trace
        List<Tuple<long, double, double>> trace = new List<Tuple<long, double, double>>();

        // Simulation Mode
        GameMode mode;

        public void TriggerLaunch()
        {
            if (mode == GameMode.Aiming)
                triggerLaunch = true;
        }

        public void StartAiming()
        {
            if (mode == GameMode.StandBy)
                startAiming = true;
        }

        private double GetAngle(double frequency)
        {
            // Perform the angle and stretch interpolations
            double fraction = (frequency - pitchReference) / pitchRange;
            return fraction * 45 + 45;
        }

        private double GetStretch(double volume)
        {
            double fraction = (volume - presenter.Settings.VolumeMinimum) / (presenter.Settings.VolumeMaximum - presenter.Settings.VolumeMinimum);
            stretch = fraction * (presenter.Settings.StretchMaximum - presenter.Settings.StretchMinimum) + presenter.Settings.StretchMinimum;
            if (stretch < 0)
                stretch = 0;
            return stretch;
        }

        private void GameLoop()
        {
            double dt = 1000 / 60.0;
            System.Diagnostics.Stopwatch clock = new System.Diagnostics.Stopwatch();
            System.Diagnostics.Stopwatch traceClock = new System.Diagnostics.Stopwatch();
            clock.Start();

            Vector target = new Vector(presenter.Settings.TargetX, presenter.Settings.TargetY, 0);

            while (isRunning)
            {
                if (clock.Elapsed.Milliseconds < dt)
                {
                    Thread.Sleep(5);
                    continue;
                }
                clock.Stop();
                clock.Reset();
                clock.Start();

                if (mode == GameMode.StandBy)
                {
                    /* On the startAiming flag we know that the user has depressed the spacebar key. We 
                     * capture the current voice volume and pitch and will use them as references during
                     * the aiming process. */
                    if (startAiming)
                    {
                        startAiming = false;
                        mode = GameMode.Aiming;
                        pitchReference = Frequency;
                        trace = new List<Tuple<long, double, double>>();
                        traceClock.Reset();
                        traceClock.Start();
                    }

                    // Update the pitch and volume data
                    if (Decibels > presenter.Settings.VolumeMinimum)
                        presenter.Frequency = Frequency;
                    else
                        presenter.Frequency = 0;
                    presenter.Decibels = Decibels;
                }

                if (mode == GameMode.Aiming)
                {
                    // Position ball on slingshot
                    Vector drawn = -50 * stretch * new Vector(1, 0, 0);
                    Vector temp = drawn.RotateAboutZ(angle * Math.PI / 180.0);
                    ball = anchor + temp;

                    angle = GetAngle(Frequency);
                    stretch = GetStretch(Decibels);

                    // Update the pitch and volume data
                    if (Decibels > presenter.Settings.VolumeMinimum)
                        presenter.Frequency = Frequency;
                    else
                    {
                        presenter.Frequency = 0;
                        triggerLaunch = true;
                    }
                    presenter.Decibels = Decibels;

                    // Store the trace
                    trace.Add(new Tuple<long,double,double>(traceClock.ElapsedMilliseconds, Frequency, Decibels));

                    presenter.IsAnchorVisible = true;

                    if (triggerLaunch)
                    {
                        triggerLaunch = false;
                        mode = GameMode.InFlight;
                        
                        // Grab the data from three frames ago
                        int targetFrame = trace.Count() - 10;
                        if (targetFrame < 0)
                            targetFrame = 0;
                        angle = GetAngle(trace[targetFrame].Item2);
                        stretch = GetStretch(trace[targetFrame].Item3);

                        // Prepare the initial velocity
                        velocity = stretch * new Vector(1, 0, 0);
                        velocity = velocity.RotateAboutZ(angle * Math.PI / 180.0);
                    }
                }

                if (mode == GameMode.InFlight)
                {
                    presenter.IsAnchorVisible = false;
                    velocity.Y += presenter.Settings.Gravity;
                    ball = ball + (dt * velocity);
                    
                    // Check if we've gone off screen
                    if (ball.X > 700 || ball.X < 0 || ball.Y > 5000 || ball.Y < 20)
                    {
                        mode = GameMode.Terminal;
                    }

                    // Check if we've passed through the target
                    if ((ball - target).Length < presenter.Settings.TargetDiameter / 2.0)
                    {
                        mode = GameMode.Terminal;
                    }
                }

                /* The terminal phase of the flight is a single sub-frame time window in which
                 * the program can clean up any one-time operations that need to happen after
                 * the projectile flight ends.  The trace can be saved to file and cleared, and
                 * any other things that need to happen before going back to StandBy or PostFlight
                 * states can be performed.  The terminal mode immediately sets the engine mode
                 * to something other than Terminal to prevent this block from being run again. */
                if (mode == GameMode.Terminal)
                {
                    mode = GameMode.StandBy;
                    
                    // Write out the trace
                    List<string> output = new List<string>();
                    output.Add("time (ms), pitch (Hz), volume (dB)");
                    for (int i = 0; i < trace.Count(); ++i)
                        output.Add(string.Format("{0}, {1}, {2}", trace[i].Item1, trace[i].Item2, trace[i].Item3));
                    string filename = string.Format("Trace {0:yyyy-MM-dd_hh-mm-ss-tt}.csv", DateTime.Now);
                    File.WriteAllLines(filename, output);



                    trace = new List<Tuple<long, double, double>>();
                }

                presenter.Ball.X = ball.X;
                presenter.Ball.Y = ball.Y;
            }

        }

        private void DetectionEngine()
        {
            PitchDetector detector = new PitchDetector(22050, 32768);
            detector.ResultsComputed += VoiceInfoAvailible;
        }

        public void VoiceInfoAvailible(object sender, PitchDetectArgs e)
        {
            lock (pitchLock)
            {
                Frequency = e.Frequency;
                Decibels = e.Decibels;
            }
        }

        public GameEngine(ApplicationPresenter _presenter)
        {
            presenter = _presenter;

            // Place Anchor
            presenter.Anchor = anchor;

            // Prepare the simulation starting conditions;
            mode = GameMode.StandBy;
            angle = 45;
            stretch = .5;

            // Start the pitch detection engine
            DetectionEngine();

            // Initialize the engine thread
            Thread gameThread = new Thread(GameLoop);
            gameThread.IsBackground = true;
            gameThread.Name = "GameThread";
            gameThread.Priority = ThreadPriority.AboveNormal;
            gameThread.Start();

        }
    }
}
