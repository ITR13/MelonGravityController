using System;
using UnityEngine;

namespace GravityController.Config {
    [Serializable]
    class GravityConfig {
        public SerializedVector3 gravity = new SerializedVector3(0,0,0);
        public KeyCode hold = KeyCode.LeftControl, trigger = KeyCode.P;
        public bool holdToActivate;
        [NonSerialized] 
        internal bool Enabled;
    }

    [Serializable]
    public class SerializedVector3 {
        public float x, y, z;

        public SerializedVector3() { }

        public SerializedVector3(float x,float y,float z) {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static implicit operator Vector3(SerializedVector3 vector3) {
            if (vector3 == null) return Vector3.zero;
            return new Vector3(vector3.x,vector3.y,vector3.z);
        }
    }
}
