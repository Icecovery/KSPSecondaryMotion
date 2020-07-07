using System.Collections.Generic;
using UnityEngine;

namespace KSPSecondaryMotion
{
    public static class InterpolationSegmentData
    {
        public static Dictionary<string, List<Segment>> segmentsDataBase = new Dictionary<string, List<Segment>>();
        
        public struct Segment
        {
            public string name;
            public int index;

            public Segment(string name, int index)
            {
                this.name = name;
                this.index = index;
            }
        }
    }
}
