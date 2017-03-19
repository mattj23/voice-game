using System.Collections.Generic;

namespace Voice_Game
{
    public class TrajectoryResult
    {
        public IEnumerable<TrajectoryLink> Links { get; set; }
        public bool IsHittingTarget { get; set; }
    }
}