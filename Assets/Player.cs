using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 100f;

    private void Update()
    {
        Vector2 moveVector = InputManager.Instance.CurrentMoveVector;
        transform.Translate(moveSpeed * Time.deltaTime * moveVector.y * Vector3.forward);
        transform.Rotate(Vector3.up * moveVector.x);
    }
}
