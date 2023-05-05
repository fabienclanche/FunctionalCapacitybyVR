using UnityEngine;
using UnityEngine.XR;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FullBodyTracking
{
    public class TrackedObject : MonoBehaviour
    {
        public Quaternion ROffset = Quaternion.identity;
        public float YOffset  { get; internal set; }

        public Transform Reference { get; private set; }

        public Vector3 Velocity { get; private set; }
        public Vector3 AngularVelocity { get; private set; }
        public Vector3 Acceleration { get; private set; }
        public Vector3 AngularAcceleration { get; private set; }

        public Vector3 TrackedPosition => Reference.InverseTransformPoint(WorldPosition);

        public Quaternion TrackedRotation => Quaternion.Inverse(Reference.rotation) * WorldRotation;

        public Quaternion WorldRotation => this.transform.rotation * ROffset;

        public Vector3 WorldPosition => transform.position + Reference.up * YOffset;

        public ulong id { get; private set; }
        public XRNode nodeType { get; private set; }
        public string TypeLabel { get; internal set; }

        private int updates = 0;
        private float lastUpdate = float.NegativeInfinity;
        private bool tracking = true;

        internal void SetFloorOffset()
        {
            YOffset = -Reference.InverseTransformPoint(transform.position).y;
        }

        private MeshRenderer meshRenderer = null;

        public MeshRenderer Renderer
        {
            get
            {
                if (meshRenderer == null) meshRenderer = this.GetComponentInChildren<MeshRenderer>();
                return meshRenderer;
            }
        }

        public bool IgnoreForBodyTracking
        {
            get
            {
                if (updates < 50 || !tracking || lastUpdate < Time.time - 5)
                {
                    Renderer.enabled = false;
                    return true;
                }

                if (gameObject.name == null || gameObject.name.Length == 0) return true;

                if (nodeType == XRNode.CenterEye || nodeType == XRNode.RightEye || nodeType == XRNode.LeftEye || nodeType == XRNode.TrackingReference) return true;

                return false;
            }
        }
        public BodyPart? bodyPart = null;

        public bool IsFoot => bodyPart != null && (bodyPart == BodyPart.Rfoot || bodyPart == BodyPart.Lfoot);

        internal void SetReference(Transform reference)
        {
            Reference = reference;
        }

        public void Start()
        {
            UpdateColor();
        }

        public void UpdateColor()
        {
            string label;
            Color color;

            UserConfig.GetDeviceLabel(id, out label, out color);

            meshRenderer?.material?.SetColor("_Color", color);
        }

        public void SetState(XRNodeState state)
        {
            this.id = state.uniqueID;
            this.nodeType = state.nodeType;

            if (id > 3) this.gameObject.name = InputTracking.GetNodeName(id);

            if (!state.tracked)
            {
                this.gameObject.name += "[Untracked]";
                tracking = false;
                Renderer.enabled = false;
                return;
            }

            updates++;
            this.lastUpdate = Time.time;
            tracking = true;
            Renderer.enabled = true;

            Vector3 position;
            if (state.TryGetPosition(out position)) this.transform.position = Reference.TransformPoint(position);

            Quaternion rotation;
            if (state.TryGetRotation(out rotation)) this.transform.rotation = Reference.rotation * rotation;

            Vector3 value;
            if (state.TryGetAcceleration(out value)) this.Acceleration = Reference.TransformVector(value);
            if (state.TryGetAngularAcceleration(out value)) this.AngularAcceleration = value;
            if (state.TryGetVelocity(out value)) this.Velocity = Reference.TransformVector(value);
            if (state.TryGetAngularVelocity(out value)) this.AngularVelocity = value;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(TrackedObject), true)]
    public class TrackedObjectEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var tobj = this.target as TrackedObject;

            EditorGUILayout.HelpBox(tobj.id + "\n Type: " + tobj.nodeType + "\n", MessageType.Info);
        }
    }
#endif
}