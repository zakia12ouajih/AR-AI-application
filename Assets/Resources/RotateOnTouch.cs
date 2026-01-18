using UnityEngine;

public class RotateOnTouch : MonoBehaviour
{
    public float rotationSpeed = 0.2f;

    private Vector2 lastTouchPosition;

    void Update()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                lastTouchPosition = touch.position;
            }
            else if (touch.phase == TouchPhase.Moved)
            {
                Vector2 delta = touch.position - lastTouchPosition;

                // Rotate around Y (left/right swipe)
                transform.Rotate(Vector3.up, -delta.x * rotationSpeed, Space.World);

                // Rotate around X (up/down swipe)
                transform.Rotate(Vector3.right, delta.y * rotationSpeed, Space.World);

                lastTouchPosition = touch.position;
            }
        }
    }
}
