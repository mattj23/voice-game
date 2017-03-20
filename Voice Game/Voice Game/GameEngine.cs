using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

using Audio_Processor;
using Newtonsoft.Json;


namespace Voice_Game
{
    public class GameEngine
    {
        private ApplicationPresenter presenter;

        // Simulation mode stuff
        public bool SimulationMode = false;
        private bool simulationStarted = false;
        public double cpaOutput;
        public string outcomeOutput;

        // Game engine events
        public delegate void TrialCompleteEventHandler(object sender, EventArgs e);
        public event TrialCompleteEventHandler TrialComplete;

        // Game Mode enumeration
        public enum GameMode {StandBy, Aiming, InFlight, Terminal, PostFlight, Pause};

        // Boolean read/write operations are atomic in C#, so this does not need to be 
        // wrapped to be thread-safe
        private bool isRunning = true;

        private bool triggerLaunch = false;
        private bool startAiming = false;

        private SynchronizationContext mainContext;

        // Element Position Variables
        private Vector ball = new Vector();
        private Vector anchor = new Vector();
        private Vector target = new Vector();
        private Vector velocity = new Vector();

        private Settings settings;
        private double angle = 10;
        private double stretch = .5;
        
        // Pitch input objects
        private Object pitchLock = new Object();
        private double Frequency;
        private double Decibels;
        
        // Pitch Reference
        double pitchReference = 0;
        double volumeReference = 0;

        // Voice trace
        private int limboFrameCount = 0;
        List<Tuple<long, double, double, double>> trace = new List<Tuple<long, double, double, double>>();

        // Simulation Mode
        GameMode mode;

        public void TriggerLaunch()
        {
            if (mode == GameMode.Aiming)
            {
                triggerLaunch = true;
                presenter.Frequency = 0;
            }
        }

        public GameMode GetMode()
        {
            return mode;
        }

        public void SetMode(GameMode m)
        {
            mode = m;
        }

        public void StartAiming()
        {
            if (mode == GameMode.StandBy)
                startAiming = true;
        }

        private double GetAngle(double frequency)
        {
            double fraction = 0;

            // Are we using semitones? If so we need to calculate them
            if (settings.UseSemitones)
            {
                // ST = 12*log2(x/reference) ... email from Jarrad August 27, 2014
                presenter.Semitone = 12 * Math.Log(frequency / pitchReference, 2);
                fraction = presenter.Semitone / settings.SemitoneSpan;
            }
            else
            {
                // The given frequency is turned into a fraction which will range from -1 to +1 based 
                // on the PitchSpan setting parameter.
                fraction = (frequency - settings.PitchMinimum) / settings.PitchSpan;
            }
            
            // Set limits on the fraction
            if (fraction < -1) fraction = -1;
            if (fraction > 1) fraction = 1;

            // Now the fraction is turned into an angle based on even interplation between the setting
            // parameters for AngleMaximum and AngleMinimum
            return fraction * (settings.AngleMaximum - settings.AngleMinimum) + (settings.AngleMaximum + settings.AngleMinimum) / 2.0;
        }

        private double GetStretch(double volume)
        {
            // The given volume is turned into a fraction which will range from 0 to 1 based on the 
            // VolumeSpan setting parameter
            double fraction = (volume - volumeReference) / settings.VolumeSpan;
            if (fraction < 0) fraction = 0;
            if (fraction > 1) fraction = 1;

            // Now the fraction is turned into a stretch value based on an even interpolation between
            // StretchMinimum and StretchMaximum
            return fraction * (settings.StretchMaximum - settings.StretchMinimum) + settings.StretchMinimum;
        }


        TrajectoryResult ComputeTrajectory(double releaseAngle, double releaseStretch)
        {
            double dt = (1000 / 60.0) - 1;
            var start = anchor.Clone();
            
            // Set the ball starting position
            ball = anchor.Clone();

            // Prepare the initial velocity
            velocity = releaseStretch* new Vector(1, 0, 0);
            velocity = velocity.RotateAboutZ(releaseAngle * Math.PI / 180.0);

            // Intialize the closest point of approach
            double distance = (target - ball).Length;
            if (ball.Y < target.Y)
                distance *= -1;
            double closestApproach = distance;

            var lastBall = ball.Clone();

            TrajectoryResult result = new TrajectoryResult
            {
                IsHittingTarget = false,
                Links = new List<TrajectoryLink> { new TrajectoryLink { X1 = lastBall.X, X2 = ball.X, Y1 = lastBall.Y, Y2 = ball.Y}}
            };

            // Simulation loop
            while (true)
            {

                velocity.Y += (settings.Gravity * (dt / 20.0));
                ball = ball + (dt * velocity);

                // Check if we've collided with the obstacle
                if (ball.X > settings.Obstacle.Position &&
                    ball.X < settings.Obstacle.Position + 30 &&
                    ball.Y > settings.Obstacle.Bottom &&
                    ball.Y < settings.Obstacle.Top)
                {
                    result.IsHittingTarget = false;
                    break;
                }

                // Check if we've gone off screen (miss)
                if (ball.X > settings.FieldWidth || ball.X < 0 || ball.Y > 15000 || ball.Y < 20)
                {
                    result.IsHittingTarget = false;
                    break;
                }

                // Check if we've passed through the target by checking to see how far the 
                // target is from the line segment between the last ball position and the 
                // current ball position.
                Vector flight = ball - lastBall;
                Vector targetRelative = target - lastBall;
                double scalarProjection = Vector.Dot(flight.Unit(), targetRelative);

                // If the scalar projection is less than zero or greater than one, the  
                // projection on to the flight vector does not lie on the segment between
                // the two flight positions. Thus we can simply check to see if the ball is 
                // within the valid target distance from the target center in the latest 
                // frame.
                if (scalarProjection > flight.Length || scalarProjection < 0)
                {
                    // Compute the new closest approach
                    distance = (target - ball).Length;
                    if (ball.Y < target.Y)
                        distance *= -1;
                    if (Math.Abs(distance) < Math.Abs(closestApproach))
                        closestApproach = distance;


                    if ((ball - target).Length < settings.TargetValidDiameter / 2.0)
                    {
                        result.IsHittingTarget = true;
                        break;
                    }
                }

                // If the scalar projection is between zero and one then it means the flight
                // vector includes the closest distance to the target. We can then check and
                // see if the closest point of approach was within the target valid diameter.
                else
                {
                    Vector closestPoint = scalarProjection * flight.Unit();

                    // Compute the new closest approach
                    distance = (target - closestPoint).Length;
                    if (ball.Y < target.Y)
                        distance *= -1;
                    if (Math.Abs(distance) < Math.Abs(closestApproach))
                        closestApproach = distance;

                    if ((targetRelative - closestPoint).Length < settings.TargetValidDiameter / 2.0)
                    {
                        ball = closestPoint + lastBall;
                        result.IsHittingTarget = true;
                        break;
                    }
                }

                result.Links.Add(new TrajectoryLink { X1 = lastBall.X, X2 = ball.X, Y1 = lastBall.Y, Y2 = ball.Y });
                // Set the last position for the ball to now be equal to the current
                // ball position.
                lastBall = ball.Clone();
            }
            result.ClosestApproach = closestApproach;
            return result;
        }

        private void GameLoop()
        {
            double dt = (1000 / 60.0) - 1;
            System.Diagnostics.Stopwatch clock = new System.Diagnostics.Stopwatch();
            System.Diagnostics.Stopwatch traceClock = new System.Diagnostics.Stopwatch();
            clock.Start();

            // Prepare the target and ball last-position vectors
            // Vector target = presenter.Settings.Target;
            Vector lastBall = new Vector();

            // Prepare the tracking variables
            double closestApproach = 0;
            string outcome = "";
            double releasePitch = 0;
            double releaseVolume = 0;
            long releaseTime = 0;
            Decibels = -100;

            double releaseAngle = 0;
            double releaseStretch = 0;

            while (isRunning)
            {
                /* The following timer stalls the program for a given period of time (currently aimed
                 * for 16ms) before the game loop continues through another pass.  In the case that 
                 * the engine is in SimulationMode and thus not actually feeding an output window, we
                 * do not care about the visual timing of the engine and would actually prefer the 
                 * simulation runs at the fastest speed possible, thus we skip this step. */
                if (!SimulationMode)
                {
                    if (clock.Elapsed.Milliseconds < dt)
                    {
                        Thread.Sleep(1);
                        continue;
                    }
                    clock.Stop();
                    clock.Reset();
                    clock.Start();
                }

                if (mode == GameMode.StandBy)
                {
                    // Update the pitch and volume data
                    if (Decibels > settings.VolumeMinimum)
                        presenter.Frequency = Frequency;
                    else
                        presenter.Frequency = 0;
                    presenter.Decibels = Decibels;
                    presenter.Score = 0;

                    // If the StartMethod is 1 (volume detection) we will trigger the startAiming flag
                    // if the decibels exceeed the volume minimum, and the next run through the loop the
                    // program will capture the reference values and begin the trace clock.
                    if (settings.StartMethod == 1 && Decibels > settings.VolumeMinimum)
                    {
                        limboFrameCount++;
                        if (limboFrameCount > settings.StartFrameCount)
                        {
                            limboFrameCount = 0;
                            startAiming = true;
                        }
                    }
                    else
                        limboFrameCount = 0;

                    /* On the startAiming flag we know that the user has depressed the spacebar key. We 
                     * capture the current voice volume and pitch and will use them as references during
                     * the aiming process. */
                    if (startAiming)
                    {
                        startAiming = false;
                        mode = GameMode.Aiming;
                        pitchReference = Frequency;

                        if (settings.VolumeMethod == 1)
                            volumeReference = settings.VolumeMinimum;
                        else
                            volumeReference = Decibels;
                        
                        trace = new List<Tuple<long, double, double, double>>();
                        traceClock.Reset();
                        traceClock.Start();
                    }
                }

                if (mode == GameMode.Aiming)
                {
                    angle = GetAngle(Frequency);
                    stretch = GetStretch(Decibels);
                    
                    
                    // Update the pitch and volume data
                    if (Decibels > settings.VolumeMinimum)
                    {
                        presenter.Frequency = Frequency;
                        presenter.Decibels = Decibels;

                        
                        // Make sure the anchor and stretching band are visible
                        presenter.IsAnchorVisible = true;

                        var result = ComputeTrajectory(angle, stretch);

                        // Store the trace
                        trace.Add(new Tuple<long, double, double, double>(traceClock.ElapsedMilliseconds, Frequency, Decibels, result.ClosestApproach));

                        if (result.IsHittingTarget)
                            this.presenter.Score += 1;

                        // Post the copying of the trajectory on the main thread.  This should be fast, all it's doing 
                        // is copying a bunch of references to an observable collection.
                        this.mainContext.Post(new SendOrPostCallback(o =>
                        {
                            this.presenter.Trajectory.Clear();
                            foreach (var trajectoryLink in result.Links)
                            {
                                this.presenter.Trajectory.Add(trajectoryLink);
                            }
                        }), null);
                    }
                    else
                    {
                        this.mode = GameMode.Terminal;
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
                    
                    // Serialze the settins data for the trace which is to be written
                    string settingsJson = JsonConvert.SerializeObject(settings, Formatting.Indented);
                    string[] settingsLines = settingsJson.Split('\n');
                    for (int i = 1; i < settingsLines.Count(); ++i)
                        settingsLines[i] = "    " + settingsLines[i];
                    settingsJson = string.Join("\n", settingsLines);

                    // Write out the trace in a JSON document
                    List<string> output = new List<string>();
                    output.Add("{");
                    output.Add("    \"test_id\":\"" + Guid.NewGuid().ToString() + "\",");
                    output.Add("    \"subject\":\"" + presenter.SubjectId + "\",");
                    output.Add("    \"timestamp\":\"" + string.Format("{0:HH:mm:ss}, {0:yyyy-MM-dd}", DateTime.Now) + "\",");
                    output.Add("    \"score\":\"" + this.presenter.Score + "\",");

                    List<string> traceOutput = new List<string>();
                    for (int i = 0; i < trace.Count(); ++i)
                        traceOutput.Add(
                            $"        [{trace[i].Item1}, {trace[i].Item2}, {trace[i].Item3}, {trace[i].Item4}]");
                    output.Add("    \"trace\":[");
                    output.Add(string.Join(",\n", traceOutput));
                    output.Add("    ],");

                    output.Add("    \"settings\":" + settingsJson);
                    output.Add("}");

                    string filename = string.Format("Test {0:yyyy-MM-dd_HH-mm-ss}.json", DateTime.Now);
                    File.WriteAllLines(filename, output);
                    trace = new List<Tuple<long, double, double, double>>();


                    // If there is an object subscribed to the new trial event, we fire it now
                    if (TrialComplete != null)
                        TrialComplete(this, new EventArgs());
                }

                if (!SimulationMode)
                {
                    presenter.Ball.X = ball.X;
                    presenter.Ball.Y = ball.Y;
                }
            }

        }

        private void DetectionEngine()
        {
            PitchDetector detector = new PitchDetector(22050, settings.WindowMilliseconds, settings.PitchPeakThreshold, 32768);
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

        /// <summary>
        /// Creates a GameEngine instance in simulation mode
        /// </summary>
        /// <param name="settings"></param>
        public GameEngine(Settings _settings, double _angle, double _stretch)
        {
            SimulationMode = true;

            target = _settings.Target;
            anchor = _settings.Anchor;
            settings = _settings;

            mode = GameMode.InFlight;
            angle = _angle;
            stretch = _stretch;

            GameLoop();

        }

        public GameEngine(ApplicationPresenter _presenter)
        {
            presenter = _presenter;


            // Place Anchor
            presenter.Anchor = presenter.Settings.Anchor;
            target = presenter.Settings.Target;
            anchor = presenter.Settings.Anchor;
            settings = presenter.Settings;

            // Prepare the simulation starting conditions;
            mode = GameMode.StandBy;
            angle = 45;
            stretch = .5;

            // Start the pitch detection engine
            DetectionEngine();

            this.mainContext = SynchronizationContext.Current;

            // Initialize the engine thread
            Thread gameThread = new Thread(GameLoop);
            gameThread.IsBackground = true;
            gameThread.Name = "GameThread";
            gameThread.Priority = ThreadPriority.AboveNormal;
            gameThread.Start();

        }
    }
}
