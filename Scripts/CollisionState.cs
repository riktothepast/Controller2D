using UnityEngine;

namespace net.fiveotwo.characterController
{
    public struct CollisionState
    {
        public bool Above;
        public bool Below;
        public bool Left;
        public bool Right;
        public bool IsAscendingSlope;
        public float SlopeAngle;

        public void Reset()
        {
            Above = Below = Left = Right = IsAscendingSlope = false;
            SlopeAngle = 0;
        }

        public bool NoCollision()
        {
            return !Above && !Below && !Left && !Right;
        }

        string GetColor(bool value)
        {
            return value ? "green" : "red";
        }

        public void Log()
        {
            Debug.Log("Above: <color=" + GetColor(Above) + ">" + Above + "</color>"
                + ", Below: <color=" + GetColor(Below) + ">" + Below + "</color>"
                + ", Left: <color=" + GetColor(Left) + ">" + Left + "</color>"
                + ", Right: <color=" + GetColor(Right) + ">" + Right + "</color>"
                + ", IsAscendingSlope: <color=" + GetColor(IsAscendingSlope) + ">" + IsAscendingSlope + "</color>");
        }
    }
}