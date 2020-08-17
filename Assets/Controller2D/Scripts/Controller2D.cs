using System;
using UnityEngine;

namespace net.fiveotwo.characterController
{
    [RequireComponent(typeof(Collider2D))]
    public class Controller2D : MonoBehaviour
    {
        public delegate void TriggerEvent(Collider2D collision);
        public delegate void CollisionEvent(RaycastHit2D hit);
        public TriggerEvent onTriggerEnter, onTriggerStay, onTriggerExit;
        public CollisionEvent onCollisionEvent;
        public Vector2 Velocity { get; private set; }
        public Vector2 CurrentNormal { get; private set; }
        private Vector2 Position => transform.position + _colliderOffset;

        [SerializeField]
        [Range(0.01f, 1f)]
        protected float skinWidth = 0.01f;
        [SerializeField]
        [Range(0.001f, 0.5f)]
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
        [SerializeField]
        protected bool debugDraw;

        private Collider2D _collider2D;
        private CapsuleDirection2D _capsuleDirection2D;
        private bool _isCapsuleCollider;
        private Vector3 _colliderOffset;
        private CollisionState _collisionState;
        private Bounds _boundingBox;
        private readonly RaycastHit2D[] _hits = new RaycastHit2D[1];

        private int _raycastHits;

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

        private RaycastHit2D? Cast(Vector2 origin, Vector2 size, Vector2 direction, float distance, LayerMask mask, float angle = 0)
        {
            Vector2 compensatedOrigin = new Vector2(origin.x - size.x * 0.5f, origin.y + size.y * 0.5f);
            DebugDrawRectangle(compensatedOrigin, size, Math.Abs(angle) > Mathf.Epsilon ? Color.yellow : Color.red, debugDraw);
            DebugDrawRectangle(compensatedOrigin + direction * distance, size, Math.Abs(angle) > Mathf.Epsilon ? Color.yellow : Color.red, debugDraw);
            _raycastHits = _isCapsuleCollider ?
                Physics2D.CapsuleCastNonAlloc(origin, size, _capsuleDirection2D, angle, direction, _hits, distance, mask) :
                Physics2D.BoxCastNonAlloc(origin, size, angle, direction, _hits, distance, mask);

            if (_raycastHits > 0)
            {
                var raycastHit = _hits[0];
                Vector2 newOrigin = new Vector2(raycastHit.centroid.x - size.x * 0.5f, raycastHit.centroid.y + size.y * 0.5f);
                DebugDrawRectangle(newOrigin, size, Math.Abs(angle) > Mathf.Epsilon ? Color.green : Color.cyan, debugDraw);
                return _hits[0];
            }
            return null;
        }

        private float CastLength(float value)
        {
            value = Math.Abs(value);

            return value + skinWidth;
        }

        private RaycastHit2D? VerticalCast(float length, Bounds boundingBox)
        {
            float direction = Mathf.Sign(length);
            float castLength = CastLength(length);

            return Cast(Position, boundingBox.size, Vector2.up * direction, castLength, solidMask);
        }

        private RaycastHit2D? DiagonalCast(float length, float angle, Bounds boundingBox)
        {
            float direction = Mathf.Sign(length);
            float castLength = CastLength(length);

            return Cast(Position, boundingBox.size, Vector2.up * direction, castLength, solidMask, angle);
        }

        private void VerticalCollision(ref Vector3 deltaStep, Bounds boundingBox)
        {
            if (Mathf.Abs(deltaStep.y) < minimumMoveDistance)
            {
                deltaStep.y = 0;
                return;
            }

            float direction = Mathf.Sign(deltaStep.y);
            RaycastHit2D? hit = VerticalCast(deltaStep.y, boundingBox);
            if (hit.HasValue)
            {
                float distance =  hit.Value.distance - skinWidth;
                float compensatedDistance = distance * direction;

                deltaStep.y = compensatedDistance;

                if (_collisionState.IsAscendingSlope)
                {
                    deltaStep.x = (deltaStep.y / Mathf.Tan(_collisionState.SlopeAngle * Mathf.Deg2Rad));
                }
                CurrentNormal = hit.Value.normal;

                _collisionState.Above = direction > 0;
                _collisionState.Below = direction < 0;
                onCollisionEvent?.Invoke(hit.Value);
            }
        }

        private void HorizontalCollision(ref Vector3 deltaStep, Bounds boundingBox)
        {
            if (Mathf.Abs(deltaStep.x) < minimumMoveDistance)
            {
                deltaStep.x = 0;
                return;
            }

            float direction = Mathf.Sign(deltaStep.x);
            float castLength = CastLength(deltaStep.x);

            RaycastHit2D? hit = Cast(Position, boundingBox.size, Vector2.right * direction, castLength, solidMask);

            if (hit.HasValue)
            {
                float distance = hit.Value.distance - skinWidth;
                float compensatedDistance = distance * direction;
                float angle = Vector2.Angle(hit.Value.normal, Vector3.up);

                if (manageSlopes && angle > 0 && angle <= maxSlopeAngle)
                {
                    Climb(ref deltaStep, angle, hit);
                } else {
                    deltaStep.x = compensatedDistance;
                }

                CurrentNormal = hit.Value.normal;

                if (!_collisionState.IsAscendingSlope)
                {
                    _collisionState.Right = direction > 0;
                    _collisionState.Left = direction < 0;
                    onCollisionEvent?.Invoke(hit.Value);
                }
            }
        }

        private void Climb(ref Vector3 deltaStep, float slopeAngle, RaycastHit2D? hit)
        {
            float moveDistance = Mathf.Abs(deltaStep.x);
            float direction = Mathf.Sign(deltaStep.x);
            float radAngle = slopeAngle * Mathf.Deg2Rad;
            float climbSpeed = Mathf.Sin(radAngle) * (moveDistance);

            if (deltaStep.y < climbSpeed)
            {
                deltaStep.y = climbSpeed;
                deltaStep.x = Mathf.Cos(radAngle) * moveDistance * direction;
                _collisionState.Below = _collisionState.IsAscendingSlope = true;
                _collisionState.SlopeAngle = slopeAngle;
            } else {
                deltaStep.y = hit.Value.distance - skinWidth;
                deltaStep.x = hit.Value.distance - skinWidth;
            }
        }

        private void Descend(ref Vector3 deltaStep)
        {
            float moveDistance = Mathf.Abs(deltaStep.x);
            float directionX = Mathf.Sign(deltaStep.x);
            RaycastHit2D? hit = VerticalCast(-moveDistance, _boundingBox);

            if (hit.HasValue)
            {
                float slopeAngle = Vector2.Angle(hit.Value.normal, Vector3.up);
                if (slopeAngle > 0 && slopeAngle <= maxSlopeAngle)
                {
                    if (hit.Value.distance - skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * moveDistance)
                    {
                        deltaStep.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * directionX;
                        deltaStep.y -= Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
                        _collisionState.Below = true;
                        _collisionState.SlopeAngle = slopeAngle;
                    }
                }
            }
        }

        public void Move(Vector3 deltaStep)
        {
            _collisionState.Reset();
            CurrentNormal = Vector2.zero;
            Velocity = deltaStep;

            if (manageSlopes && deltaStep.y < 0)
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

                if (_collisionState.IsAscendingSlope)
                {
                    transform.Translate(deltaStep);
                    deltaStep.y = 0;
                }
            }

            if (Math.Abs(deltaStep.y) > Mathf.Epsilon)
            {
                VerticalCollision(ref deltaStep, _boundingBox);
                transform.Translate(Vector2.up * deltaStep);
            }

            if (Time.deltaTime > Mathf.Epsilon)
            {
                Velocity = deltaStep / Time.deltaTime;
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

        public void UpdateCollisionBoundaries()
        {
            _boundingBox = new Bounds(Vector3.zero, _collider2D.bounds.size);
            _colliderOffset = _collider2D.offset;
            _boundingBox.Expand(-skinWidth * 2f);
        }

        private static void DebugDrawRectangle(Vector3 position, Vector2 size, Color color, bool debugDraw = false)
        {
            if (debugDraw)
            {
                Debug.DrawLine(position, new Vector3(position.x + size.x, position.y, position.z), color);
                Debug.DrawLine(position, new Vector3(position.x, position.y - size.y, position.z), color);
                Debug.DrawLine(new Vector3(position.x, position.y - size.y, position.z), new Vector3(position.x + size.x, position.y - size.y, position.z), color);
                Debug.DrawLine(new Vector3(position.x + size.x, position.y - size.y, position.z), new Vector3(position.x + size.x, position.y, position.z), color);
            }
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