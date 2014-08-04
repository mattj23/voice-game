using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using Audio_Processor;

namespace Voice_Game
{
    public class GameEngine
    {
        private ApplicationPresenter presenter;

        // Game Mode enumeration
        enum GameMode {Aiming, InFlight, PostFlight};

        // Boolean read/write operations are atomic in C#, so this does not need to be 
        // wrapped to be thread-safe
        private bool isRunning = true;

        private bool triggerLaunch = false;
        
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

        // Simulation Mode
        GameMode mode;

        public void TriggerLaunch()
        {
            if (mode == GameMode.Aiming)
                triggerLaunch = true;
        }

        private void GameLoop()
        {
            double dt = 1000 / 60.0;
            System.Diagnostics.Stopwatch clock = new System.Diagnostics.Stopwatch();
            clock.Start();
            
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

                if (mode == GameMode.Aiming)
                {
                    // Position ball on slingshot
                    Vector drawn = -50 * stretch * new Vector(1, 0, 0);
                    Vector temp = drawn.RotateAboutZ(angle * Math.PI / 180.0);
                    ball = anchor + temp;
                    
                    // Angle rotate for demonstration interface
                    angle += 0.2;
                    if (angle > 90)
                        angle = 0;

                    // Update the pitch and volume data
                    presenter.Frequency = Frequency;
                    presenter.Decibels = Decibels;

                    presenter.IsAnchorVisible = true;

                    // Check to see if a launch has been triggered
                    if (triggerLaunch)
                    {
                        triggerLaunch = false;
                        mode = GameMode.InFlight;

                        // Prepare the initial velocity
                        velocity = stretch * new Vector(1, 0, 0);
                        velocity = velocity.RotateAboutZ(angle * Math.PI / 180.0);
                    }
                }

                if (mode == GameMode.InFlight)
                {
                    presenter.IsAnchorVisible = false;
                    velocity.Y += (-0.01);
                    ball = ball + (dt * velocity);

                    if (ball.X > 700 || ball.X < 0 || ball.Y > 5000 || ball.Y < 0)
                    {
                        mode = GameMode.Aiming;
                    }
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
            mode = GameMode.Aiming;
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
