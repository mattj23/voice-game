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
        public enum GameMode {StandBy, Aiming, InFlight, Terminal, PostFlight};

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
        double volumeReference = 0;

        // Voice trace
        List<Tuple<long, double, double>> trace = new List<Tuple<long, double, double>>();

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

        public void StartAiming()
        {
            if (mode == GameMode.StandBy)
                startAiming = true;
        }

        private double GetAngle(double frequency)
        {
            double fraction = 0;

            // Are we using semitones? If so we need to calculate them
            if (presenter.Settings.UseSemitones)
            {
                // ST = 12*log2(x/reference) ... email from Jarrad August 27, 2014
                presenter.Semitone = 12 * Math.Log(frequency / pitchReference, 2);
                fraction = presenter.Semitone / presenter.Settings.SemitoneSpan;
            }
            else
            {
                // The given frequency is turned into a fraction which will range from -1 to +1 based 
                // on the PitchSpan setting parameter.
                fraction = (frequency - pitchReference) / presenter.Settings.PitchSpan;
            }
            
            // Set limits on the fraction
            if (fraction < -1) fraction = -1;
            if (fraction > 1) fraction = 1;

            // Now the fraction is turned into an angle based on even interplation between the setting
            // parameters for AngleMaximum and AngleMinimum
            return fraction * (presenter.Settings.AngleMaximum - presenter.Settings.AngleMinimum) + (presenter.Settings.AngleMaximum + presenter.Settings.AngleMinimum) / 2.0;
        }

        private double GetStretch(double volume)
        {
            // The given volume is turned into a fraction which will range from 0 to 1 based on the 
            // VolumeSpan setting parameter
            double fraction = (volume - volumeReference) / presenter.Settings.VolumeSpan;
            if (fraction < 0) fraction = 0;
            if (fraction > 1) fraction = 1;

            // Now the fraction is turned into a stretch value based on an even interpolation between
            // StretchMinimum and StretchMaximum
            return fraction * (presenter.Settings.StretchMaximum - presenter.Settings.StretchMinimum) + presenter.Settings.StretchMinimum;
        }

        private void GameLoop()
        {
            double dt = (1000 / 60.0) - 1;
            System.Diagnostics.Stopwatch clock = new System.Diagnostics.Stopwatch();
            System.Diagnostics.Stopwatch traceClock = new System.Diagnostics.Stopwatch();
            clock.Start();

            // Prepare the target and ball last-position vectors
            Vector target = presenter.Settings.Target;
            Vector lastBall = new Vector();

            // Prepare the tracking variables
            double closestApproach = 0;
            string outcome = "";
            double releasePitch = 0;
            double releaseVolume = 0;
            long releaseTime = 0;

            while (isRunning)
            {
                if (clock.Elapsed.Milliseconds < dt)
                {
                    Thread.Sleep(1);
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
                        volumeReference = Decibels;
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
                        // If the release method is 1 and the volume has dropped below the threshold 
                        // (i.e. we're in this block of code) then we can trigger the launch.
                        if (presenter.Settings.ReleaseMethod == 1)
                        {
                            presenter.Frequency = 0;
                            triggerLaunch = true;
                        }
                    }
                    presenter.Decibels = Decibels;

                    // If the release method is 0 and the elapsed time has exceeded the number of milliseconds
                    // stored in AutoReleaseTime, we trigger the launch
                    if (presenter.Settings.ReleaseMethod == 0 && traceClock.ElapsedMilliseconds > presenter.Settings.AutoReleaseTime)
                    {
                        presenter.Frequency = 0;
                        triggerLaunch = true;
                    }

                    // Store the trace
                    trace.Add(new Tuple<long,double,double>(traceClock.ElapsedMilliseconds, Frequency, Decibels));

                    // Make sure the anchor and stretching band are visible
                    presenter.IsAnchorVisible = true;

                    // When the given condition is achieved to trigger the launch of the projectile,
                    // the triggerLaunch flag is set. To ensure that this state lasts for only part of
                    // a single frame, the flag is immediately unset and the game mode changes.
                    if (triggerLaunch)
                    {
                        triggerLaunch = false;
                        mode = GameMode.InFlight;
                        
                        // If the release method is 1, we must grab the data from ten frames ago,
                        // otherwise we use the current frame
                        int targetFrame = trace.Count() - 1;
                        if (presenter.Settings.ReleaseMethod == 1)
                            targetFrame = trace.Count() - 10;

                        if (targetFrame < 0)
                            targetFrame = 0;
                        releasePitch = trace[targetFrame].Item2;
                        releaseVolume = trace[targetFrame].Item3;
                        releaseTime = trace[targetFrame].Item1;

                        angle = GetAngle(releasePitch);
                        stretch = GetStretch(releaseVolume);

                        // Prepare the initial velocity
                        velocity = stretch * new Vector(1, 0, 0);
                        velocity = velocity.RotateAboutZ(angle * Math.PI / 180.0);

                        // Intialize the closest point of approach
                        double distance = (target - ball).Length;
                        if (ball.Y < target.Y)
                            distance *= -1;
                        closestApproach = distance;

                        // Set the last position for the ball to be equal to the current position
                        lastBall = ball.Clone();
                    }
                }

                if (mode == GameMode.InFlight)
                {
                    presenter.IsAnchorVisible = false;
                    velocity.Y += (presenter.Settings.Gravity * (dt / 20.0));
                    ball = ball + (dt * velocity);
                    
                    // Check if we've collided with the obstacle
                    if (ball.X > presenter.Settings.Obstacle.Position &&
                        ball.X < presenter.Settings.Obstacle.Position + 30 &&
                        ball.Y > presenter.Settings.Obstacle.Bottom &&
                        ball.Y < presenter.Settings.Obstacle.Top)
                    {
                        mode = GameMode.Terminal;
                        outcome = "obstacle";
                    }

                    // Check if we've gone off screen (miss)
                    if (ball.X > 700 || ball.X < 0 || ball.Y > 5000 || ball.Y < 20)
                    {
                        mode = GameMode.Terminal;
                        outcome = "miss";
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
                        double distance = (target - ball).Length;
                        if (ball.Y < target.Y)
                            distance *= -1;
                        if (Math.Abs(distance) < Math.Abs(closestApproach))
                            closestApproach = distance;


                        if ((ball - target).Length < presenter.Settings.TargetValidDiameter / 2.0)
                        {
                            mode = GameMode.Terminal;
                            outcome = "hit";
                        }
                    }

                    // If the scalar projection is between zero and one then it means the flight
                    // vector includes the closest distance to the target. We can then check and
                    // see if the closest point of approach was within the target valid diameter.
                    else
                    {
                        Vector closestPoint = scalarProjection * flight.Unit();

                        // Compute the new closest approach
                        double distance = (target - closestPoint).Length;
                        if (ball.Y < target.Y)
                            distance *= -1;
                        if (Math.Abs(distance) < Math.Abs(closestApproach))
                            closestApproach = distance;

                        if ((targetRelative - closestPoint).Length < presenter.Settings.TargetValidDiameter / 2.0)
                        {
                            mode = GameMode.Terminal;
                            ball = closestPoint + lastBall;
                            outcome = "hit";
                        }
                    }

                    
                    // Set the last position for the ball to now be equal to the current
                    // ball position.
                    lastBall = ball.Clone();
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
                    output.Add(string.Format("trial, {0:hh:mm:ss tt}, {0:yyyy-MM-dd}", DateTime.Now));
                    output.Add(string.Format("release time, {0}, ms", releaseTime));
                    output.Add(string.Format("release pitch, {0}, Hz", releasePitch));
                    output.Add(string.Format("release volume, {0}, dB", releaseVolume));
                    output.Add(string.Format("closest approach, {0}", closestApproach));
                    output.Add(string.Format("outcome, {0}", outcome));
                    output.Add("");
                    output.Add("time (ms), pitch (Hz), volume (dB)");
                    long lastWrittenTime = 0;
                    double lastWrittenPitch = 0;
                    double lastWrittenVolume = 0;
                    for (int i = 0; i < trace.Count(); ++i)
                    {
                        bool writeThisLine = true;

                        // If we're doing a clean trace, we check to see if the last written pitch and 
                        // volume is equal to the current volume. If it is we set the write flag to ignore
                        // writing this line.
                        if (presenter.Settings.CleanTrace)
                        {
                            if (lastWrittenPitch == trace[i].Item2 && lastWrittenVolume == trace[i].Item3)
                                writeThisLine = false;
                        }

                        // If it's been equal to or more than the window frame from the last time we wrote
                        // a line to the file, we need to write this one regardless of what the clean trace
                        // option says.
                        if (trace[i].Item1 - lastWrittenTime >= presenter.Settings.WindowMilliseconds)
                            writeThisLine = true;

                        if (writeThisLine)
                        {
                            lastWrittenTime = trace[i].Item1;
                            lastWrittenPitch = trace[i].Item2;
                            lastWrittenVolume = trace[i].Item3;
                            output.Add(string.Format("{0}, {1}, {2}", trace[i].Item1, trace[i].Item2, trace[i].Item3));
                        }
                    }
                    
                    string filename = string.Format("Trace {0:yyyy-MM-dd_HH-mm-ss}.csv", DateTime.Now);
                    File.WriteAllLines(filename, output);



                    trace = new List<Tuple<long, double, double>>();
                }

                presenter.Ball.X = ball.X;
                presenter.Ball.Y = ball.Y;
            }

        }

        private void DetectionEngine()
        {
            PitchDetector detector = new PitchDetector(22050, presenter.Settings.WindowMilliseconds, 32768);
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
