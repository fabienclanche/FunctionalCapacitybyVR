using UnityEngine;
using UnityEditor;
using System;
using System.Runtime.Serialization;

namespace FullBodyTracking.Mocap
{
    [Serializable, DataContract]
    public class SinglePointTrackingData
    {
        [DataMember(Order = 0)] public Vector3 p;

        // Legacy mocap files used euler angles to represent rotation. Those will be stored in Vector3 r. Newer files use quaternions, stored in Vector4 q. Newer files will not have r present
        [DataMember(Order = 1, EmitDefaultValue = false)] public Vector3? r;
        [DataMember(Order = 2)] public Vector4 q;

        [DataMember(Order = 3, EmitDefaultValue = false)] public Vector3 v;
        [DataMember(Order = 4, EmitDefaultValue = false)] public Vector3 av;
        [DataMember(Order = 5, EmitDefaultValue = false)] public Vector3 a;
        [DataMember(Order = 6, EmitDefaultValue = false)] public Vector3 aa;

        public Vector3 Position
        {
            get { return p; }
            set { p = value; }
        }
        public Quaternion Rotation
        {
            get { if (r != null) return Quaternion.Euler((Vector3)r); else return new Quaternion(q.x, q.y, q.z, q.w); }
            set { q = new Vector4(value.x, value.y, value.z, value.w); }
        }
    }
}