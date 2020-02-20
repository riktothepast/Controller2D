using System;
using UnityEngine;

namespace net.fiveotwo.characterController
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class Controller2D : MonoBehaviour
    {
        public delegate void TriggerEvent(Collider2D collision);
        public TriggerEvent onTriggerEnter, onTriggerStay, onTriggerExit;
        
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

        private BoxCollider2D _boxCollider2D;
        private CollisionState _collisionState, _lastCollisionState;
        private Bounds _boundingBox;

        protected void Awake()
        {
            _boxCollider2D = GetComponent<BoxCollider2D>();
            _collisionState = _lastCollisionState = new CollisionState();
            _collisionState.Reset();
            UpdateCollisionBoundaries();
        }

        private RaycastHit2D CastBox(Vector2 origin, Vector2 size, Vector2 direction, float distance, LayerMask mask, float angle = 0)
        {
            Vector2 compensatedOrigin = new Vector2(origin.x - size.x * 0.5f, origin.y + size.y * 0.5f);
            DebugDrawRectangle(compensatedOrigin, size, Math.Abs(angle) > Mathf.Epsilon ? Color.yellow : Color.red);
            DebugDrawRectangle(compensatedOrigin + direction * distance, size, angle != 0 ? Color.yellow : Color.red);
            RaycastHit2D hit = Physics2D.BoxCast(origin, size, angle, direction, distance, mask);
            if (hit)
            {
                Vector2 newOrigin = new Vector2(hit.centroid.x - size.x * 0.5f, hit.centroid.y + size.y * 0.5f);
                DebugDrawRectangle(newOrigin, size, Math.Abs(angle) > Mathf.Epsilon ? Color.green : Color.cyan);
            }
            return hit;
        }

        private float CastLength(float value)
        {
            if (Mathf.Abs(value) < skinWidth)
            {
                return 2 * skinWidth;
            }
            return value;
        }

        private RaycastHit2D VerticalCast(float length, Bounds boundingBox) {
            float direction = Mathf.Sign(length);
            float castLength = CastLength(Mathf.Abs(length));
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

                if (_collisionState.IsAscendingSlope)
                {
                    deltaStep.x = deltaStep.y / Mathf.Tan(_collisionState.SlopeAngle * Mathf.Deg2Rad) * Mathf.Sign(deltaStep.x);
                }

                _collisionState.Above = direction > 0;
                _collisionState.Below = direction < 0;
            }
        }

        private void HorizontalCollision(ref Vector3 deltaStep, Bounds boundingBox)
        {
            float direction = Mathf.Sign(deltaStep.x);
            float castLength = CastLength(Mathf.Abs(deltaStep.x));
            float extends = boundingBox.extents.x;
            float halfExtends = extends * 0.5f;
            float initialDistance = halfExtends * direction;
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
                if (!_collisionState.IsAscendingSlope)
                {
                    _collisionState.Right = direction > 0;
                    _collisionState.Left = direction < 0;
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
                _collisionState.Below = _collisionState.IsAscendingSlope = true;
                _collisionState.SlopeAngle = slopeAngle;
            }
        }

        private void Descend(ref Vector3 deltaStep)
        {
            float moveDistance = Mathf.Abs(deltaStep.x);
            float directionX = Mathf.Sign(deltaStep.x);
            if (manageSlopes)
            {
                RaycastHit2D hit = VerticalCast(-Mathf.Infinity, _boundingBox);

                if (hit)
                {
                    float slopeAngle = Vector2.Angle(hit.normal, Vector3.up);
                    if (Math.Abs(slopeAngle) > Mathf.Epsilon)
                    {
                        if (hit.distance - skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * moveDistance)
                        {
                            deltaStep.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * directionX;
                            deltaStep.y -= Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
                            _collisionState.Below = true;
                            _collisionState.SlopeAngle = slopeAngle;
                        }
                    }
                }
            }
        }

        public void Move(Vector3 deltaStep)
        {
            _collisionState.Reset();

            if (deltaStep.y < 0) {
                Descend(ref deltaStep);
            }

            if (Math.Abs(deltaStep.x) > Mathf.Epsilon)
            {
                HorizontalCollision(ref deltaStep, _boundingBox);
                transform.Translate(Vector2.right * deltaStep);
            }

            if (Math.Abs(deltaStep.y) > Mathf.Epsilon)
            {
                VerticalCollision(ref deltaStep, _boundingBox);
                transform.Translate(Vector2.up * deltaStep);
            }
            
            _lastCollisionState = _collisionState;
            if (logCollisions)
            {
                _collisionState.Log();
            }
        }

        public CollisionState CollisionState()
        {
            return _collisionState;
        }

        public void UpdateCollisionBoundaries()
        {
            _boundingBox = new Bounds(Vector3.zero, _boxCollider2D.size);
            _boundingBox.Expand(-2f * skinWidth);
        }

        private static void DebugDrawRectangle(Vector3 position, Vector2 size, Color color)
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