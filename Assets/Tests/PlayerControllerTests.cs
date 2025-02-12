//using NUnit.Framework;
//using UnityEngine;

//public class PlayerControllerTests
//{
//    private PlayerController playerController;
//    private Rigidbody rb;
//    private InputManager inputManager;

//    [SetUp]
//    public void SetUp()
//    {
//        var gameObject = new GameObject();
//        playerController = gameObject.AddComponent<PlayerController>();
//        rb = gameObject.AddComponent<Rigidbody>();
//        inputManager = gameObject.AddComponent<InputManager>();
//        playerController.inputManager = inputManager;
//        playerController.rb = rb;
//    }

//    [Test]
//    public void OnEnable_SubscribesToInputManagerEvents()
//    {
//        playerController.OnEnable();
//        Assert.IsNotNull(inputManager.OnStartTouch);
//        Assert.IsNotNull(inputManager.OnEndTouch);
//    }

//    [Test]
//    public void OnDisable_UnsubscribesFromInputManagerEvents()
//    {
//        playerController.OnEnable();
//        playerController.OnDisable();
//        Assert.IsNull(inputManager.OnStartTouch);
//        Assert.IsNull(inputManager.OnEndTouch);
//    }

//    [Test]
//    public void FixedUpdate_MovesPlayer()
//    {
//        playerController.allowMove = true;
//        playerController.speed = 10;
//        playerController.rotationSpeed = 10;
//        playerController.maxRotationAngle = 45;

//        inputManager.CurrentFingerPosition = new Vector2(Screen.width / 2 + 1, 0);

//        playerController.FixedUpdate();

//        Assert.AreNotEqual(Vector3.zero, rb.position);
//    }

//    [Test]
//    public void OnCollisionEnter_ChangesBoxLocations()
//    {
//        var spawner = new GameObject().AddComponent<Spawner>();
//        spawner.boxLocation = BoxLocation.Forward;
//        playerController.playnesManager = new GameObject().AddComponent<PaperPlaynesManager>();
//        playerController.playnesManager.spawners = new Spawner[] { spawner };

//        var collision = new Collision();
//        collision.gameObject = spawner.gameObject;

//        playerController.OnCollisionEnter(collision);

//        Assert.AreEqual(BoxLocation.ForwardLeft, spawner.boxLocation);
//    }
//}
