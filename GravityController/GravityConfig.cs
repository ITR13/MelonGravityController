using System;
using UnityEngine;

namespace GravityController
{
    [System.Serializable]
    class GravityConfig
    {
        public Vector3 gravity;
        public KeyCode hold = KeyCode.LeftControl, trigger = KeyCode.P;
        [NonSerialized] public bool Enabled;
    }
}
