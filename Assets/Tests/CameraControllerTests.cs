using NUnit.Framework;
using UnityEngine;

public class CameraControllerTests
{
    private CameraController cameraController;
    private PlayerController playerController;

    [SetUp]
    public void SetUp()
    {
        var gameObject = new GameObject();
        cameraController = gameObject.AddComponent<CameraController>();
        playerController = new GameObject().AddComponent<PlayerController>();
        cameraController.playerController = playerController;
    }

    [Test]
    public void Start_SetsOffset()
    {
        cameraController.Start();
        Assert.AreNotEqual(Vector3.zero, cameraController.offset);
    }

    [Test]
    public void Update_MovesCameraTowardsPlayer()
    {
        cameraController.Start();
        var initialPosition = cameraController.transform.position;

        cameraController.Update();

        Assert.AreNotEqual(initialPosition, cameraController.transform.position);
    }
}
