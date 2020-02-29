using System;
using UnityEngine;

namespace net.fiveotwo.characterController
{
    [RequireComponent(typeof(Collider2D))]
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
        protected bool manageSlopes;
        [SerializeField]
        [Range(10f, 90f)]
        protected float maxSlopeAngle = 45f;
        [SerializeField]
        protected bool logCollisions;

        private Collider2D _collider2D;
        private CapsuleDirection2D _capsuleDirection2D;
        private bool _isCapsuleCollider;
        private Vector3 _colliderOffset;
        private CollisionState _collisionState;
        private Bounds _boundingBox;

        private Vector2 Position => transform.position + _colliderOffset;

        protected void Awake()
        {
            _collider2D = GetComponent<Collider2D>();
            _isCapsuleCollider = (_collider2D.GetType() == typeof(CapsuleCollider2D));
            if (_isCapsuleCollider)
            {
                _capsuleDirection2D = ((CapsuleCollider2D)_collider2D).direction;
            }
            _collisionState = new CollisionState();
            _collisionState.Reset();
            UpdateCollisionBoundaries();
        }

        private RaycastHit2D Cast(Vector2 origin, Vector2 size, Vector2 direction, float distance, LayerMask mask, float angle = 0)
        {
            Vector2 compensatedOrigin = new Vector2(origin.x - size.x * 0.5f, origin.y + size.y * 0.5f);
            DebugDrawRectangle(compensatedOrigin, size, Math.Abs(angle) > Mathf.Epsilon ? Color.yellow : Color.red);
            DebugDrawRectangle(compensatedOrigin + direction * distance, size, Math.Abs(angle) > Mathf.Epsilon ? Color.yellow : Color.red);
            RaycastHit2D hit = _isCapsuleCollider ?
                Physics2D.CapsuleCast(origin, size, _capsuleDirection2D, angle, direction, distance, mask) :
                Physics2D.BoxCast(origin, size, angle, direction, distance, mask);

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
            return value + skinWidth;
        }

        private RaycastHit2D VerticalCast(float length, Bounds boundingBox)
        {
            float direction = Mathf.Sign(length);
            float castLength = CastLength(Mathf.Abs(length));

            return Cast(Position + new Vector2(0, skinWidth * direction), boundingBox.size, Vector2.up * direction, castLength, solidMask);
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
                } else
                {
                    float compensatedDistance = distance + skinWidth * direction;
                    deltaStep.y = Mathf.Abs(compensatedDistance) < Mathf.Abs(distance) ? compensatedDistance : distance;
                }
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
            RaycastHit2D hit = Cast(Position + new Vector2(direction * skinWidth, 0), boundingBox.size, Vector2.right * direction, castLength, solidMask);

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
                if (!_collisionState.IsAscendingSlope)
                {
                    float distance = (hit.distance * direction);
                    if (Mathf.Abs(distance) < minimumMoveDistance)
                    {
                        deltaStep.x = 0;
                    } else
                    {
                        float compensatedDistance = distance + skinWidth * direction;
                        deltaStep.x = Mathf.Abs(compensatedDistance) < Mathf.Abs(distance) ? compensatedDistance : distance;
                    }
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
                    if (slopeAngle <= maxSlopeAngle)
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

            if (deltaStep.y < 0)
            {
                Descend(ref deltaStep);
            }

            if (Math.Abs(deltaStep.x) > Mathf.Epsilon)
            {
                HorizontalCollision(ref deltaStep, _boundingBox);
                if (!_collisionState.IsAscendingSlope)
                {
                    transform.Translate(Vector2.right * deltaStep);
                }
            }

            if (Math.Abs(deltaStep.y) > Mathf.Epsilon)
            {
                float previousVerticalSpeed = deltaStep.y;
                VerticalCollision(ref deltaStep, _boundingBox);
                if (_collisionState.IsAscendingSlope)
                {
                    transform.Translate(Vector2.right * deltaStep);
                }
                bool slopeConditions = _collisionState.IsAscendingSlope && Mathf.Abs(previousVerticalSpeed) > Mathf.Abs(deltaStep.y);
                deltaStep.y = slopeConditions ? previousVerticalSpeed : deltaStep.y;
                transform.Translate(Vector2.up * deltaStep);
            }

            if (logCollisions)
            {
                _collisionState.Log();
            }
        }

        public CollisionState CollisionState()
        {
            return _collisionState;
        }

        private void UpdateCollisionBoundaries()
        {
            _boundingBox = new Bounds(Vector3.zero, _collider2D.bounds.size);
            _boundingBox.Expand(-2f * skinWidth);
            _colliderOffset = _collider2D.offset;
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