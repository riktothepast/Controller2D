using UnityEngine;

namespace raia.characterController
{
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
}