using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voice_Game;
using Newtonsoft.Json;
using System.IO;

namespace ComputeSolution
{
    class Program
    {
        static void Main(string[] args)
        {
            // Perform something with the information
            // Load the settings 
            string settingsFile = "settings.json";
            if (!File.Exists(settingsFile))
            {
                throw new Exception("No settings file!");
            }
            Settings settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(settingsFile));
            if (settings.Anchor == null)
                settings.Anchor = new Voice_Game.Vector(60, 75, 0);

            while (true)
            {
                string s = Console.ReadLine();
                if (s.Contains(','))
                {
                    // Attempt to parse into angle and stretch variables 
                    string[] parts = s.Split(',');
                    double angle = 0;
                    double stretch = 0;
                    if (!double.TryParse(parts[0], out angle))
                        continue;
                    if (!double.TryParse(parts[1], out stretch))
                        continue;

                    

                    // Perform the simulation
                    GameEngine engine = new GameEngine(settings, angle, stretch);
                    Console.WriteLine(engine.cpaOutput.ToString());

                }
                else if (s == "end")
                    break;
            }
        }
    }
}
