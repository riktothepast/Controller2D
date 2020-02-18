using raia.characterController;
using UnityEngine;

[RequireComponent(typeof(Controller2D))]
public class MoveController : MonoBehaviour
{
    [SerializeField]
    float movementSpeed = 10f;
    [SerializeField]
    float gravity = 30f;
    [SerializeField]
    float jumpHeight = 3f;
    [SerializeField]
    float groundDamping = 10f;
    [SerializeField]
    float airDamping = 5f;
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
        float damping = controller2D.CollisionState().below ? groundDamping : airDamping;
        velocity.x = Mathf.Lerp(velocity.x, Input.GetAxis("Horizontal") * movementSpeed, Time.deltaTime * damping);
        velocity.y -= gravity * Time.deltaTime;
        controller2D.Move(velocity * Time.deltaTime);
        if (controller2D.CollisionState().below)
        {
            velocity.y = 0;
        }
        if (controller2D.CollisionState().left || controller2D.CollisionState().right)
        {
            velocity.x = 0;
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
