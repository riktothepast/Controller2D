using UnityEngine;

namespace net.fiveotwo.characterController
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class Controller2D : MonoBehaviour
    {
        [SerializeField]
        [Range(0.01f, 0.05f)]
        protected float skinWidth = 0.01f;
        [SerializeField]
        [Range(0.001f, 0.1f)]
        protected float minimumMoveDistance = 0.001f;
        [SerializeField]
        protected LayerMask solidMask;
        [SerializeField]
        protected bool manageSlopes = false;
        [SerializeField]
        [Range(10f, 90f)]
        protected float maxSlopeAngle = 45f;
        [SerializeField]
        protected bool logCollisions = false;
        protected BoxCollider2D boxCollider2D;
        private CollisionState collisionState, lastCollisionState;
        private Bounds boundingBox;
        public delegate void TriggerEvent(Collider2D collision);
        public TriggerEvent onTriggerEnter, onTriggerStay, onTriggerExit;

        void Awake()
        {
            boxCollider2D = GetComponent<BoxCollider2D>();
            collisionState = lastCollisionState = new CollisionState();
            collisionState.Reset();
            UpdateCollisionBoundaries();
        }

        protected RaycastHit2D CastBox(Vector2 origin, Vector2 size, Vector2 direction, float distance, LayerMask mask, float angle = 0)
        {
            Vector2 compensatedOrigin = new Vector2(origin.x - size.x * 0.5f, origin.y + size.y * 0.5f);
            DebugDrawRectangle(compensatedOrigin, size, angle != 0 ? Color.yellow : Color.red);
            DebugDrawRectangle(compensatedOrigin + direction * distance, size, angle != 0 ? Color.yellow : Color.red);
            RaycastHit2D hit = Physics2D.BoxCast(origin, size, angle, direction, distance, mask);
            if (hit)
            {
                Vector2 newOrigin = new Vector2(hit.centroid.x - size.x * 0.5f, hit.centroid.y + size.y * 0.5f);
                DebugDrawRectangle(newOrigin, size, angle != 0 ? Color.green : Color.cyan);
            }
            return hit;
        }

        private float CastLenght(float value)
        {
            if (Mathf.Abs(value) < skinWidth)
            {
                return 2 * skinWidth;
            }
            return value;
        }

        private RaycastHit2D VerticalCast(float lenght, Bounds boundingBox) {
            float direction = Mathf.Sign(lenght);
            float castLength = CastLenght(Mathf.Abs(lenght));
            float extends = boundingBox.extents.y;
            float halfExtends = boundingBox.extents.y * 0.5f;
            float initialDistance = halfExtends * direction;
            Vector2 size = new Vector2(boundingBox.size.x, extends + skinWidth);

            return CastBox(transform.position + new Vector3(0, initialDistance), size, Vector2.up * direction, castLength, solidMask);
        }

        private void VerticalCollision(ref Vector3 deltaStep, Bounds boundingBox)
        {
            float direction = Mathf.Sign(deltaStep.y);
            RaycastHit2D hit = VerticalCast(deltaStep.y, boundingBox);
            if (hit)
            {
                float distance = hit.distance * direction;
                if (Mathf.Abs(distance) < minimumMoveDistance)
                {
                    deltaStep.y = 0;
                }
                float compensatedDistance = distance + skinWidth * direction;

                deltaStep.y = Mathf.Abs(compensatedDistance) < Mathf.Abs(distance) ? compensatedDistance : distance;

                if (collisionState.onSlopeAsc)
                {
                    deltaStep.x = deltaStep.y / Mathf.Tan(collisionState.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(deltaStep.x);
                }

                collisionState.above = direction > 0 ? true : false;
                collisionState.below = direction < 0 ? true : false;
            }
        }

        private void HorizontalCollision(ref Vector3 deltaStep, Bounds boundingBox)
        {
            float direction = Mathf.Sign(deltaStep.x);
            float castLength = CastLenght(Mathf.Abs(deltaStep.x));
            float extends = boundingBox.extents.x;
            float extendsY = boundingBox.extents.y;
            float halfExtends = extends * 0.5f;
            float initialDistance = halfExtends * direction;
            float edgeY = transform.position.y - extendsY;
            Vector2 size = new Vector2(extends + skinWidth, boundingBox.size.y);
            RaycastHit2D hit = CastBox(transform.position + new Vector3(initialDistance, 0), size, Vector2.right * direction, castLength, solidMask);

            if (hit)
            {
                if (manageSlopes)
                {
                    float angle = Vector2.Angle(hit.normal, Vector3.up);
                    if (angle <= maxSlopeAngle)
                    {
                        Climb(ref deltaStep, angle);
                    }
                }
                float distance = (hit.distance * direction);

                if (Mathf.Abs(distance) < minimumMoveDistance)
                {
                    deltaStep.x = 0;
                }
                float compensatedDistance = distance + skinWidth * direction;

                deltaStep.x = Mathf.Abs(compensatedDistance) < Mathf.Abs(distance) ? compensatedDistance : distance;
                if (!collisionState.onSlopeAsc)
                {
                    collisionState.right = direction > 0 ? true : false;
                    collisionState.left = direction < 0 ? true : false;
                }
            }
        }
        
        private void Climb(ref Vector3 deltaStep, float slopeAngle)
        {
            float moveDistance = Mathf.Abs(deltaStep.x);
            float direction = Mathf.Sign(deltaStep.x);
            float climbVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;

            if (deltaStep.y <= climbVelocityY)
            {
                deltaStep.y = climbVelocityY;
                deltaStep.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * direction;
                collisionState.below = collisionState.onSlopeAsc = true;
                collisionState.slopeAngle = slopeAngle;
            }
        }

        private void Decend(ref Vector3 deltaStep)
        {
            float moveDistance = Mathf.Abs(deltaStep.x);
            float directionX = Mathf.Sign(deltaStep.x);
            if (manageSlopes)
            {
                RaycastHit2D hit = VerticalCast(-Mathf.Infinity, boundingBox);

                if (hit)
                {
                    float slopeAngle = Vector2.Angle(hit.normal, Vector3.up);
                    float descendVelocity = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance;
                    if (slopeAngle != 0)
                    {
                        if (hit.distance - skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * moveDistance)
                        {
                            deltaStep.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * directionX;
                            deltaStep.y -= Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
                            collisionState.onSlopeDesc = collisionState.below = true;
                            collisionState.slopeAngle = slopeAngle;
                        }
                    }
                }
            }
        }

        public void Move(Vector3 deltaStep)
        {
            collisionState.Reset();

            if (deltaStep.y < 0) {
                Decend(ref deltaStep);
            }

            if (deltaStep.x != 0)
            {
                HorizontalCollision(ref deltaStep, boundingBox);
                transform.Translate(Vector2.right * deltaStep);
            }

            if (deltaStep.y != 0)
            {
                VerticalCollision(ref deltaStep, boundingBox);
                transform.Translate(Vector2.up * deltaStep);
            }
            
            lastCollisionState = collisionState;
            if (logCollisions)
            {
                collisionState.Log();
            }
        }

        public CollisionState CollisionState()
        {
            return collisionState;
        }

        public void UpdateCollisionBoundaries()
        {
            boundingBox = new Bounds(Vector3.zero, boxCollider2D.size);
            boundingBox.Expand(-2f * skinWidth);
        }

        private void DebugDrawRectangle(Vector3 position, Vector2 size, Color color)
        {
            Debug.DrawLine(position, new Vector3(position.x + size.x, position.y, position.z), color);
            Debug.DrawLine(position, new Vector3(position.x, position.y - size.y, position.z), color);
            Debug.DrawLine(new Vector3(position.x, position.y - size.y, position.z), new Vector3(position.x + size.x, position.y - size.y, position.z), color);
            Debug.DrawLine(new Vector3(position.x + size.x, position.y - size.y, position.z), new Vector3(position.x + size.x, position.y, position.z), color);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            onTriggerEnter?.Invoke(collision);
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            onTriggerStay?.Invoke(collision);
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            onTriggerExit?.Invoke(collision);
        }
    }
}