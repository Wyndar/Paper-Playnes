using System.Collections.Generic;
using UnityEngine;

public static class BoxLocationMappings
{
    public static Dictionary<BoxLocation, Vector3> GetBoxLocationVector() => new()
    {
        { BoxLocation.UpperLeft, new(0,2,0) }, { BoxLocation.UpperRight, new(2,2,0) },
        { BoxLocation.UpperCentre, new(1,2,0) }, { BoxLocation.UpperForwardLeft, new(0,2,1) },
        { BoxLocation.UpperForwardRight, new(2,2,1) }, { BoxLocation.UpperForward, new(1,2,1) },
        { BoxLocation.MiddleLeft, new(0,1,0) }, { BoxLocation.MiddleRight, new(2,1,0) },
        { BoxLocation.MiddleCentre, new(1,1,0) }, { BoxLocation.MiddleForwardLeft, new(0,1,1) },
        { BoxLocation.MiddleForwardRight, new(2,1,1) }, { BoxLocation.MiddleForward, Vector3.one },
        { BoxLocation.LowerLeft, Vector3.zero }, { BoxLocation.LowerRight, new(2,0,0) },
        { BoxLocation.LowerCentre, new(1,0,0) }, { BoxLocation.LowerForwardLeft, new(0,0,1) },
        { BoxLocation.LowerForwardRight, new(2,0,1) }, { BoxLocation.LowerForward, new(1,0,1) }
    };
}
