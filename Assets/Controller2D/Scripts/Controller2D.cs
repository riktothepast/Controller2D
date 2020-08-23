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
        private Vector2 Position => transform.localPosition + _colliderOffset;

        [SerializeField]
        [Range(0.01f, 0.5f)]
        protected float skinWidth = 0.01f;
        [SerializeField]
        [Range(0.001f, 0.5f)]
        protected float minimumMoveDistance = 0.001f;
        [SerializeField]
        protected LayerMask solidMask;
        [SerializeField]
        protected LayerMask triggerMask;
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
        private Vector3 _colliderOffset;
        private CollisionState _collisionState;
        private Bounds _boundingBox;
        private RaycastHit2D? _horizontalHit;
        private RaycastHit2D? _verticalHit;
        private readonly RaycastHit2D[] _hits = new RaycastHit2D[1];
        private Vector3 _displacementVector;
        private int _raycastHits;

        private void IgnoreLayers()
        {
            for (int layerIndex = 0; layerIndex < 32; layerIndex++)
            {
                if ((triggerMask.value & 1 << layerIndex) == 0)
                {
                    Physics2D.IgnoreLayerCollision(gameObject.layer, layerIndex);
                }
            }
        }

        protected void Awake()
        {
            _collider2D = GetComponent<Collider2D>();
            _collisionState = new CollisionState();
            _collisionState.Reset();
            UpdateCollisionBoundaries();
            IgnoreLayers();
        }

        private RaycastHit2D? Cast(Vector2 origin, Vector2 size, Vector2 direction, float distance, LayerMask mask, float angle = 0)
        {
            if (debugDraw)
            {
                Vector2 compensatedOrigin = new Vector2(origin.x - size.x * 0.5f, origin.y + size.y * 0.5f);
                DebugDrawRectangle(compensatedOrigin, size, Color.blue, debugDraw);
                DebugDrawRectangle(compensatedOrigin + direction * distance, size, Color.blue, debugDraw);
            }
            _raycastHits = Physics2D.BoxCastNonAlloc(origin, size, angle, direction, _hits, distance, mask);

            if (_raycastHits > 0)
            {
                if (debugDraw)
                {
                    Vector2 newOrigin = new Vector2(_hits[0].centroid.x - size.x * 0.5f, _hits[0].centroid.y + size.y * 0.5f);
                    DebugDrawRectangle(newOrigin, size, Color.red, debugDraw);
                }
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

        private void VerticalCollision(ref Vector3 deltaStep, Bounds boundingBox)
        {
            if (Mathf.Abs(deltaStep.y) < minimumMoveDistance)
            {
                deltaStep.y = 0;
                return;
            }

            float direction = Mathf.Sign(deltaStep.y);
            _verticalHit = VerticalCast(deltaStep.y, boundingBox);
            if (_verticalHit.HasValue)
            {
                float distance = _verticalHit.Value.distance - skinWidth;
                float compensatedDistance = distance * direction;

                deltaStep.y = compensatedDistance;

                if (_collisionState.IsAscendingSlope)
                {
                    deltaStep.x = (deltaStep.y / Mathf.Tan(_collisionState.SlopeAngle * Mathf.Deg2Rad));
                }
                CurrentNormal = _verticalHit.Value.normal;

                _collisionState.Above = direction > 0;
                _collisionState.Below = direction < 0;
                onCollisionEvent?.Invoke(_verticalHit.Value);
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

            _horizontalHit = Cast(Position, boundingBox.size, Vector2.right * direction, castLength, solidMask);

            if (_horizontalHit.HasValue)
            {
                float distance = _horizontalHit.Value.distance - skinWidth;
                float compensatedDistance = distance * direction;
                float angle = Vector2.Angle(_horizontalHit.Value.normal, Vector3.up);

                if (manageSlopes && angle > 0 && angle <= maxSlopeAngle)
                {
                    Climb(ref deltaStep, angle, _horizontalHit);
                } else
                {
                    deltaStep.x = compensatedDistance;
                }

                CurrentNormal = _horizontalHit.Value.normal;

                if (!_collisionState.IsAscendingSlope)
                {
                    _collisionState.Right = direction > 0;
                    _collisionState.Left = direction < 0;
                    onCollisionEvent?.Invoke(_horizontalHit.Value);
                }
            }
        }

        private void Climb(ref Vector3 deltaStep, float slopeAngle, RaycastHit2D? hit)
        {
            float moveDistance = Mathf.Abs(deltaStep.x);
            float direction = Mathf.Sign(deltaStep.x);
            float radAngle = slopeAngle * Mathf.Deg2Rad;
            float climbSpeed = Mathf.Sin(radAngle) * (moveDistance);

            if (deltaStep.y <= climbSpeed)
            {
                deltaStep.y = climbSpeed;
                deltaStep.x = Mathf.Cos(radAngle) * moveDistance * direction;
                _collisionState.Below = _collisionState.IsAscendingSlope = true;
                _collisionState.SlopeAngle = slopeAngle;
            } else
            {
                deltaStep.x = (hit.Value.distance - (skinWidth * 2f)) * direction;
            }
        }

        private void Descend(ref Vector3 deltaStep)
        {
            float moveDistance = Mathf.Abs(deltaStep.x);
            float directionX = Mathf.Sign(deltaStep.x);
            _verticalHit = VerticalCast(-Mathf.Infinity, _boundingBox);

            if (_verticalHit.HasValue)
            {
                float slopeAngle = Vector2.Angle(_verticalHit.Value.normal, Vector3.up);
                if (slopeAngle > 0 && slopeAngle <= maxSlopeAngle)
                {
                    if (_verticalHit.Value.distance - skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * moveDistance)
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
            _displacementVector = transform.localPosition;

            if (manageSlopes && deltaStep.y < 0)
            {
                Descend(ref deltaStep);
            }

            if (Math.Abs(deltaStep.x) > Mathf.Epsilon)
            {
                HorizontalCollision(ref deltaStep, _boundingBox);

                if (!_collisionState.IsAscendingSlope)
                {
                    _displacementVector.x += deltaStep.x;
                    transform.localPosition = _displacementVector;
                }

                if (_collisionState.IsAscendingSlope)
                {
                    _displacementVector += deltaStep;
                    transform.localPosition = _displacementVector;
                    deltaStep.y = 0;
                }
            }

            if (Math.Abs(deltaStep.y) > Mathf.Epsilon)
            {
                VerticalCollision(ref deltaStep, _boundingBox);
                _displacementVector.y += deltaStep.y;
                transform.localPosition = _displacementVector;
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