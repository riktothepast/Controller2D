using net.fiveotwo.characterController;
using UnityEngine;

[RequireComponent(typeof(Controller2D))]
public class MoveController : MonoBehaviour
{
    [SerializeField]
    private float movementSpeed = 10f;
    [SerializeField]
    private float gravity = 30f;
    [SerializeField]
    private float jumpHeight = 3f;
    [SerializeField]
    private float groundDamping = 10f;
    [SerializeField]
    private float airDamping = 5f;
    private Controller2D _controller2D;
    private Vector2 _velocity;

    private void Awake()
    {
        _controller2D = GetComponent<Controller2D>();
        _controller2D.onTriggerEnter += OnTriggerEnter2D;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Destroy(collision.gameObject);
    }

    private void Update()
    {
        float damping = _controller2D.CollisionState().Below ? groundDamping : airDamping;
        _velocity.x = Mathf.Lerp(_velocity.x, Input.GetAxis("Horizontal") * movementSpeed, Time.deltaTime * damping);
        _velocity.y -= gravity * Time.deltaTime;
        _controller2D.Move(_velocity * Time.deltaTime);
        if (_controller2D.CollisionState().Below || _controller2D.CollisionState().Above)
        {
            _velocity.y = 0;
        }
        if (_controller2D.CollisionState().Left || _controller2D.CollisionState().Right)
        {
            _velocity.x = 0;
        }
        if (_controller2D.CollisionState().Below && Input.GetKeyDown(KeyCode.Space))
        {
            Jump();
        } else if (_controller2D.CollisionState().Below && Input.GetAxis("Vertical") < 0f)
        {
            FallFromPlatform();
        }
    }

    private void Jump()
    {
        _velocity.y = Mathf.Sqrt(2f * jumpHeight * gravity);
    }

    private void FallFromPlatform()
    {
        _controller2D.IgnoreOneWayPlatforms();
        _velocity.y = -1f;
    }
}
