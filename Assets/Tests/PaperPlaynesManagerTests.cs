using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;


public class PaperPlaynesManagerTests
{
    private PaperPlaynesManager manager;
    private Spawner spawner;

    [SetUp]
    public void SetUp()
    {
        var gameObject = new GameObject();
        manager = gameObject.AddComponent<PaperPlaynesManager>();
        spawner = gameObject.AddComponent<Spawner>();
        manager.spawners = new Spawner[] { spawner };
        manager.spawnerLocations = new Dictionary<BoxLocation, Vector3>();
    }

    [Test]
    public void Start_AddsSpawnerLocations()
    {
        manager.Start();
        Assert.IsTrue(manager.spawnerLocations.ContainsKey(spawner.boxLocation));
    }

    [Test]
    public void ChangeBoxLocations_ChangesBoxLocationAndPosition()
    {
        spawner.boxLocation = BoxLocation.Forward;
        manager.spawnerLocations[BoxLocation.Left] = new Vector3(1, 1, 1);
        manager.spawnerLocations[BoxLocation.ForwardLeft] = new Vector3(2, 2, 2);

        manager.ChangeBoxLocations(spawner);

        Assert.AreEqual(BoxLocation.ForwardLeft, spawner.boxLocation);
        Assert.AreEqual(new Vector3(2, 2, 2), spawner.transform.position);
    }

    [Test]
    public void HandleRespawn_RespawnsTargetSpawners()
    {
        var targetSpawner = new GameObject().AddComponent<Spawner>();
        targetSpawner.boxLocation = BoxLocation.Left;
        manager.spawners = new Spawner[] { spawner, targetSpawner };

        manager.HandleRespawn(BoxLocation.Forward, new[] { BoxLocation.Forward }, new[] { BoxLocation.Left });

        // Assuming Respawn method sets boxCount to 0
        Assert.AreEqual(0, targetSpawner.boxCount);
    }
}
