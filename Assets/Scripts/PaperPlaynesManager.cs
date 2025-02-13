using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PaperPlaynesManager : MonoBehaviour
{
    public Spawner[] spawners;
    public Dictionary<BoxLocation, Vector3> spawnerLocations = new();
    private bool shouldRearrange;
    public void Awake()
    {
        foreach (Spawner spawner in spawners)
        {
            spawnerLocations.Add(spawner.boxLocation, spawner.transform.position);
            if (BoxLocationMappings.GetBoxLocationVector().TryGetValue(spawner.boxLocation, out Vector3 box))
                spawner.boxLocationVector = box;
        }
    }

    public void ChangeBoxLocations(Spawner enteredSpawner)
    {
        Vector3 vectorDifference = enteredSpawner.boxLocationVector - new Vector3(1, 1, 0);
        foreach (Spawner spawner in spawners)
        {
            spawner.boxLocationVector -= vectorDifference;
            spawner.boxLocationVector = AdjustVector(spawner.boxLocationVector);
            spawner.boxLocation = BoxLocationMappings.GetBoxLocationVector().GetKeyByValue(spawner.boxLocationVector);
            spawner.gameObject.name = spawner.boxLocation.ToString();
            if (spawnerLocations.TryGetValue(spawner.boxLocation, out Vector3 newPosition))
                spawner.transform.position = newPosition;
            if(shouldRearrange)
                spawner.RearrangeBoxes();
        }
    }
    private Vector3 AdjustVector(Vector3 vector)
    {
        if (vector.x < 0)
        {
            vector.x = 2;
            shouldRearrange = true;
        }
        if (vector.y < 0)
        {
            vector.y = 2;
            shouldRearrange = true;
        }
        if (vector.z < 0)
        {
            vector.z = 1;
            shouldRearrange = true;
        }
        return new(vector.x,vector.y,vector.z);
    }
}
