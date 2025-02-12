using NUnit.Framework;
using UnityEngine;

public class SpawnerTests
{
    private Spawner spawner;

    [SetUp]
    public void SetUp()
    {
        var gameObject = new GameObject();
        spawner = gameObject.AddComponent<Spawner>();
        spawner.spawnBox = new GameObject();
        spawner.boxCount = 3;
    }

    [Test]
    public void Respawn_SpawnsBoxes()
    {
        spawner.Respawn();
        Assert.AreEqual(3, spawner.transform.childCount);
    }

    [Test]
    public void Despawn_RemovesAllChildObjects()
    {
        spawner.Respawn();
        spawner.Despawn();
        Assert.AreEqual(0, spawner.transform.childCount);
    }

    [Test]
    public void GetRandomPositionWithinBounds_ReturnsPositionWithinBounds()
    {
        var renderer = spawner.gameObject.AddComponent<MeshRenderer>();
        renderer.bounds = new Bounds(Vector3.zero, Vector3.one);

        var position = spawner.GetRandomPositionWithinBounds(spawner.gameObject);

        Assert.IsTrue(renderer.bounds.Contains(position));
    }
}
