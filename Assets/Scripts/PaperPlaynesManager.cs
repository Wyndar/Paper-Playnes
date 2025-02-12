using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PaperPlaynesManager : MonoBehaviour
{
    public Spawner[] spawners;
    public Dictionary<BoxLocation, Vector3> spawnerLocations = new();

    public void Start()
    {
        foreach (Spawner spawner in spawners)
            spawnerLocations.Add(spawner.boxLocation, spawner.transform.position);
    }

    public void ChangeBoxLocations(Spawner spawner)
    {
        var mappings = GetLocationMappings(spawner.boxLocation);

        if (mappings == null)
            return;

        HandleRespawn(spawner.boxLocation, new[] { BoxLocation.Forward, BoxLocation.ForwardLeft,
            BoxLocation.ForwardRight }, new[] { BoxLocation.Left, BoxLocation.Right, BoxLocation.Centre });
        HandleRespawn(spawner.boxLocation, new[] { BoxLocation.Left, BoxLocation.ForwardLeft },
            new[] { BoxLocation.Right, BoxLocation.ForwardRight });
        HandleRespawn(spawner.boxLocation, new[] { BoxLocation.Right, BoxLocation.ForwardRight },
            new[] { BoxLocation.Left, BoxLocation.ForwardLeft });

        spawners.Where(s => mappings.TryGetValue(s.boxLocation, out _))
            .ToList().ForEach(s =>
            {
                if (mappings.TryGetValue(s.boxLocation, out BoxLocation newLocation))
                {
                    s.boxLocation = newLocation;
                    if (spawnerLocations.TryGetValue(newLocation, out Vector3 newPosition))
                        s.transform.position = newPosition;
                }
            });
    }

    public void HandleRespawn(BoxLocation spawnerLocation, BoxLocation[] triggerLocations, BoxLocation[] targetLocations)
    {
        if (triggerLocations.Contains(spawnerLocation))
            foreach (Spawner s in spawners.Where(s => targetLocations.Contains(s.boxLocation)))
                s.Respawn();
    }

    private Dictionary<BoxLocation, BoxLocation> GetLocationMappings(BoxLocation boxLocation) => boxLocation switch
    {
        BoxLocation.Forward => new Dictionary<BoxLocation, BoxLocation>
            {
                { BoxLocation.Left, BoxLocation.ForwardLeft }, { BoxLocation.Right, BoxLocation.ForwardRight },
                { BoxLocation.Centre, BoxLocation.Forward }, { BoxLocation.ForwardLeft, BoxLocation.Left },
                { BoxLocation.ForwardRight, BoxLocation.Right }, { BoxLocation.Forward, BoxLocation.Centre }
            },
        BoxLocation.Left => new Dictionary<BoxLocation, BoxLocation>
            {
                { BoxLocation.Forward, BoxLocation.ForwardRight }, { BoxLocation.Centre, BoxLocation.Right },
                { BoxLocation.ForwardRight, BoxLocation.ForwardLeft }, { BoxLocation.Right, BoxLocation.Left },
                { BoxLocation.ForwardLeft, BoxLocation.Forward }, { BoxLocation.Left, BoxLocation.Centre }
            },
        BoxLocation.ForwardRight => new Dictionary<BoxLocation, BoxLocation>
            {
                { BoxLocation.Forward, BoxLocation.Left }, { BoxLocation.Centre, BoxLocation.ForwardLeft },
                { BoxLocation.ForwardLeft, BoxLocation.Right }, { BoxLocation.Left, BoxLocation.ForwardRight },
                { BoxLocation.Right, BoxLocation.Centre }, { BoxLocation.ForwardRight, BoxLocation.Forward }
            },
        BoxLocation.ForwardLeft => new Dictionary<BoxLocation, BoxLocation>
            {
                { BoxLocation.Forward, BoxLocation.Right }, { BoxLocation.Centre, BoxLocation.ForwardRight },
                { BoxLocation.ForwardRight, BoxLocation.Left }, { BoxLocation.Right, BoxLocation.ForwardLeft },
                { BoxLocation.Left, BoxLocation.Centre }, { BoxLocation.ForwardLeft, BoxLocation.Forward }
            },
        BoxLocation.Right => new Dictionary<BoxLocation, BoxLocation>
            {
                { BoxLocation.Forward, BoxLocation.ForwardLeft }, { BoxLocation.Centre, BoxLocation.Left },
                { BoxLocation.ForwardLeft, BoxLocation.ForwardRight }, { BoxLocation.Left, BoxLocation.Right },
                { BoxLocation.ForwardRight, BoxLocation.Forward }, { BoxLocation.Right, BoxLocation.Centre }
            },
        _ => null
    };
}
