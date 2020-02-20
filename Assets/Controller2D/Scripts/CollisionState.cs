using UnityEngine;

namespace net.fiveotwo.characterController
{
    public struct CollisionState
    {
        public bool above;
        public bool below;
        public bool left;
        public bool right;
        public bool onSlopeAsc;
        public bool onSlopeDesc;
        public float slopeAngle;

        public void Reset()
        {
            above = below = left = right = onSlopeAsc = onSlopeDesc = false;
            slopeAngle = 0;
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
}