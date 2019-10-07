using UnityEngine;

public class MoveController : MonoBehaviour
{
    [SerializeField]
    float movementSpeed;
    [SerializeField]
    float gravity;
    Controller2D controller2D;
    Vector2 velocity;

    private void Awake()
    {
        controller2D = GetComponent<Controller2D>();
    }

    void Update()
    {
        velocity.x = Input.GetAxis("Horizontal");
        velocity.y -= gravity * Time.deltaTime;
        controller2D.Move(velocity * movementSpeed * Time.deltaTime);
        if (controller2D.CollisionState().below)
        {
            velocity.y = 0;
        }
        if (controller2D.CollisionState().below && Input.GetKeyDown(KeyCode.Space))
        {
            Jump();
        }
    }

    void Jump()
    {
        velocity.y = Mathf.Sqrt(gravity);
    }
}
