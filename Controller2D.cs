using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Controller2D : MonoBehaviour
{
    [SerializeField]
    [Range(0.01f, 0.5f)]
    protected float skinWidth = 0.01f;
    [SerializeField]
    [Range(0.001f, 0.1f)]
    protected float minimumMoveDistance = 0.001f;
    [SerializeField]
    protected LayerMask solidMask;
    [SerializeField]
    protected bool logCollisions = false;
    protected BoxCollider2D boxCollider2D;
    private CollisionState collisionState, lastCollisionState;
    private Bounds boundingBox;
    private Bounds expandedBoundingBox;

    void Awake()
    {
        boxCollider2D = GetComponent<BoxCollider2D>();
        boundingBox = expandedBoundingBox = new Bounds(Vector3.zero, boxCollider2D.size);
        collisionState = lastCollisionState = new CollisionState();
        collisionState.Reset();
        boundingBox.Expand(-2f * skinWidth);
    }

    protected RaycastHit2D CastBox(Vector2 origin, Vector2 size, Vector2 direction, float distance, LayerMask mask, float angle = 0f)
    {
        Vector2 compensatedOrigin = new Vector2(origin.x - size.x * 0.5f, origin.y + size.y * 0.5f);
        DebugDrawRectangle(compensatedOrigin, size, Color.red);
        DebugDrawRectangle(compensatedOrigin + direction * distance, size, Color.red);
        RaycastHit2D hit = Physics2D.BoxCast(origin, size, angle, direction, distance, mask);
        if (hit)
        {
            Vector2 newOrigin = new Vector2(hit.centroid.x - size.x * 0.5f, hit.centroid.y + size.y * 0.5f);
            DebugDrawRectangle(newOrigin, size, Color.cyan);
        }
        return hit;
    }

    protected RaycastHit2D CastRay(Vector2 origin, Vector2 ending, LayerMask mask)
    {
        Debug.DrawLine(origin, ending, Color.green);
        return Physics2D.Linecast(origin, ending, mask);
    }

    private float CastLenght(float value)
    {
        if (Mathf.Abs(value) < skinWidth)
        {
            return 2 * skinWidth;
        }
        return value;
    }

    private float VerticalCollision(Vector2 deltaStep, Bounds boundingBox)
    {
        float direction = Mathf.Sign(deltaStep.y);
        float castLength = CastLenght(Mathf.Abs(deltaStep.y));
        float extends = boundingBox.extents.y;
        float halfExtends = boundingBox.extents.y * 0.5f;
        float initialDistance = halfExtends * direction;
        Vector2 size = new Vector2(boundingBox.size.x, extends + skinWidth);
        RaycastHit2D hit = CastBox(transform.position + new Vector3(0, initialDistance), size, Vector2.up * direction, castLength, solidMask);

        if (!hit)
        {
            return deltaStep.y;
        }

        collisionState.above = direction > 0 ? true : false;
        collisionState.below = direction < 0 ? true : false;

        float distance = hit.distance * direction;
        if (Mathf.Abs(distance) < minimumMoveDistance)
        {
            return 0;
        }
        float compensatedDistance = distance + skinWidth * direction;

        return Mathf.Abs(compensatedDistance) < Mathf.Abs(distance) ? compensatedDistance : distance;
    }

    private float HorizontalCollision(Vector2 deltaStep, Bounds boundingBox)
    {
        float direction = Mathf.Sign(deltaStep.x);
        float castLength = CastLenght(Mathf.Abs(deltaStep.x));
        float extends = boundingBox.extents.x;
        float halfExtends = extends * 0.5f;
        float initialDistance = halfExtends * direction;
        Vector2 size = new Vector2(extends + skinWidth, boundingBox.size.y);
        RaycastHit2D hit = CastBox(transform.position + new Vector3(initialDistance, 0), size, Vector2.right * direction, castLength, solidMask);

        if (!hit)
        {
            return deltaStep.x;
        }

        collisionState.right = direction > 0 ? true : false;
        collisionState.left = direction < 0 ? true : false;

        float distance = (hit.distance * direction);

        if (Mathf.Abs(distance) < minimumMoveDistance)
        {
            return 0;
        }
        float compensatedDistance = distance + skinWidth * direction;

        return Mathf.Abs(compensatedDistance) < Mathf.Abs(distance) ? compensatedDistance : distance;
    }

    public void Move(Vector3 deltaStep)
    {
        bool noCollisionLastTime = lastCollisionState.NoCollision();
        collisionState.Reset();

        if (deltaStep.y != 0)
        {
            deltaStep.y = VerticalCollision(deltaStep, boundingBox);
            transform.Translate(new Vector3(0, deltaStep.y));
        }

        if (deltaStep.x != 0)
        {
            deltaStep.x = HorizontalCollision(deltaStep, boundingBox);
            transform.Translate(new Vector3(deltaStep.x, 0));
        }

        lastCollisionState = collisionState;
        if (logCollisions)
        {
            collisionState.Log();
        }
    }

    private void DebugDrawRectangle(Vector3 position, Vector2 size, Color color)
    {
        Debug.DrawLine(position, new Vector3(position.x + size.x, position.y, position.z), color);
        Debug.DrawLine(position, new Vector3(position.x, position.y - size.y, position.z), color);
        Debug.DrawLine(new Vector3(position.x, position.y - size.y, position.z), new Vector3(position.x + size.x, position.y - size.y, position.z), color);
        Debug.DrawLine(new Vector3(position.x + size.x, position.y - size.y, position.z), new Vector3(position.x + size.x, position.y, position.z), color);
    }

    public CollisionState CollisionState()
    {
        return collisionState;
    }
}

public struct CollisionState
{
    public bool above;
    public bool below;
    public bool left;
    public bool right;

    public void Reset()
    {
        above = below = left = right = false;
    }

    public bool NoCollision()
    {
        return !above && !below && !left && !right;
    }

    string GetColor(bool value)
    {
        return value ? "green" : "red";
    }

    public void Log()
    {
        Debug.Log("Above: <color=" + GetColor(above) + ">" + above + "</color>"
            + ", Below: <color=" + GetColor(below) + ">" + below + "</color>"
            + ", Left: <color=" + GetColor(left) + ">" + left + "</color>"
            + ", Right: <color=" + GetColor(right) + ">" + right + "</color>");
    }
}