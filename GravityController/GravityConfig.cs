using System;
using UnityEngine;

namespace GravityController
{
    [System.Serializable]
    class GravityConfig
    {
        public SerializedVector3 gravity;
        public KeyCode hold = KeyCode.LeftControl, trigger = KeyCode.P;
        [NonSerialized] public bool Enabled;
    }

    [System.Serializable]
    public class SerializedVector3
    {
        public float x, y, z;

        public SerializedVector3() { }

        public SerializedVector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static implicit operator Vector3(SerializedVector3 vector3)
        {
            return new Vector3(vector3.x, vector3.y, vector3.z);
        }
    }
}
