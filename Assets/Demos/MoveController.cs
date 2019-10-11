using raia.characterController;
using UnityEngine;

public class MoveController : MonoBehaviour
{
    [SerializeField]
    float movementSpeed = 10f;
    [SerializeField]
    float gravity = 30f;
    [SerializeField]
    float jumpHeight = 3f;
    Controller2D controller2D;
    Vector2 velocity;

    private void Awake()
    {
        controller2D = GetComponent<Controller2D>();
        controller2D.onTriggerEnter += OnTriggerEnter2D;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Destroy(collision.gameObject);
    }

    void Update()
    {
        velocity.x = Input.GetAxis("Horizontal") * movementSpeed;
        velocity.y -= gravity * Time.deltaTime;
        controller2D.Move(velocity * Time.deltaTime);
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
        velocity.y = Mathf.Sqrt(2f * jumpHeight * gravity);
    }
}
