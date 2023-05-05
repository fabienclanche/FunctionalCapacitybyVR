using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;

/*
Copyright 2020 Julie#8169 STREAM_DOGS#4199

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation 
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, 
modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the 
Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS 
BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF
OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace Liquids
{
    public class LiquidSim : MonoBehaviour
    {

        private Rigidbody MakeSpillParticle()
        {
            poolIndex++;
            if (poolIndex >= maxPoolSize) poolIndex = 0;

            if (dropletPool == null) dropletPool = new List<GameObject>(maxPoolSize);

            while (poolIndex >= dropletPool.Count)
            {
                var newDroplet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                newDroplet.AddComponent<Rigidbody>();
                newDroplet.AddComponent<Droplet>();
                var renderer = newDroplet.GetComponent<MeshRenderer>();
 
                renderer.sharedMaterial = SpillMaterial;

                dropletPool.Add(newDroplet);
            }

            var droplet = dropletPool[poolIndex];
            droplet.SetActive(true);
            return droplet.GetComponent<Rigidbody>();
        }

        public Material SpillMaterial
        {
            get
            {
                if (spillMaterial == null)
                {
                    spillMaterial = new Material(this.sphereRenderer.sharedMaterial);

                    spillMaterial.SetFloat("_LiquidLevel", 1f);
                    spillMaterial.SetFloat("_Radius", 0.5f);
                    spillMaterial.SetFloat("_WavesIntensity", 0);
                }
                return spillMaterial;
            }
        }

        public MeshRenderer sphereRenderer;
        public ParticleSystem particleEmitter;
        public float containerHeight = 1.5f;

        [Header("Physics")]
        public float volume = 0;
        public float sphereRadius = 1f;
        [Tooltip("Loss of velocity, as a proportion of velocity per second"), Range(0, 1)]
        public float friction = 0.01f;

        [Header("Pool")]
        public int maxPoolSize = 30;
        private int poolIndex = -1;
        internal List<GameObject> dropletPool;


        [Header("Current physics state")]
        public Vector3 pendulum;
        public Vector3 pendulumSpeed;
        public Quaternion lastOrientation;
        public Vector3 lastPosition, lastVelocity;
        public float rippleMagnitude = 0;
        public float lastVolume = 0;
        public GameObject lastSpill;
        public Material spillMaterial;

        public float SurfaceRadius
        {
            get
            {
                float h = NormalizedLiquidHeight;
                return Mathf.Sqrt(1 - h * h) * sphereRadius;
            }
        }

        public float VolumePct => volume / (Mathf.PI * 4f / 3f * sphereRadius * sphereRadius * sphereRadius);

        public float NormalizedLiquidHeight
        {
            get
            {
                float v = VolumePct;

                if (v <= 0) return -1;
                if (v >= 1) return 1;

                float h = -Mathf.Log((1 - v) / (v)) / Mathf.Log(2.72f) / 3.25f; // inverse logistic function to estimate height given volume

                return Mathf.Min(1, h);
            }
        }

        public float MaxVolume => HeightToVolume(this.containerHeight - this.sphereRadius);

        public float HeightToVolume(float h)
        {
            h = h + sphereRadius;
            return (Mathf.PI * h * h / 3) * (3 * sphereRadius - h);
        }

        private void InitPendulum()
        {
            this.pendulum = -this.transform.up;
            this.pendulumSpeed = Vector3.zero;
            this.lastOrientation = this.transform.rotation;
            this.lastPosition = this.transform.position;
            this.lastVolume = this.volume;
        }

        private void UpdatePendulum()
        {
            const float g = 9.81f;

            Vector3 oldSpeed = this.pendulumSpeed;
            float friction = Mathf.Pow(1 - this.friction, Time.deltaTime);

            // apply object rotation to pendulum
            this.pendulum = this.transform.rotation * Quaternion.Inverse(this.lastOrientation) * this.pendulum;

            // apply linear movement forces
            Vector3 velocity = (this.transform.position - this.lastPosition) / Time.deltaTime;
            this.pendulumSpeed -= (velocity - this.lastVelocity);
            this.lastVelocity = velocity;
            this.lastPosition = this.transform.position;

            // apply friction
            this.pendulumSpeed *= friction;

            // apply pendulum rotation forces
            Vector3 pAcceleration = Vector3.down * g;

            this.pendulumSpeed += pAcceleration * Time.deltaTime;
            this.pendulumSpeed -= pendulum * Mathf.Max(0, Vector3.Dot(pendulum, pendulumSpeed));

            // apply speed to pendulum
            Vector3 pendulumOld = pendulum;
            this.pendulum += this.pendulumSpeed * Time.deltaTime;
            this.pendulum = pendulum.normalized;

            // apply pendulum rotation to object
            Vector3 axis = Vector3.Cross(pendulumOld, pendulum);
            float angle = Vector3.SignedAngle(pendulumOld, pendulum, axis);
            this.transform.rotation = Quaternion.AngleAxis(angle, axis) * this.transform.rotation;

            this.lastOrientation = this.transform.rotation;

            var acc = (this.pendulumSpeed - oldSpeed);
            this.rippleMagnitude += ((acc.y * acc.y) / (1 + acc.magnitude) + Mathf.Abs(this.volume - this.lastVolume)) / 10f;
            if (this.rippleMagnitude > 1) this.rippleMagnitude = 1;
            this.rippleMagnitude *= friction;
            if (this.rippleMagnitude < 0) this.rippleMagnitude = 0;

            this.lastVolume = this.volume;

            // debug
            Debug.DrawLine(this.transform.position, this.transform.position + pendulum, Color.green);
            Debug.DrawLine(this.transform.position + pendulum, this.transform.position + pendulum + pendulumSpeed, Color.red);
        }

        private void UpdateSpill()
        {
            float maxContainerCapacity = MaxVolume;
            if (this.volume > maxContainerCapacity)
            {
                this.volume = maxContainerCapacity;
            }

            //

            Quaternion invRot = Quaternion.Inverse(this.transform.rotation);
            Vector3 gravityLocal = this.transform.InverseTransformDirection(Vector3.down);
            Vector3 containerUpLocal = invRot * this.transform.parent.up;
            Vector3 containerOpeningCenter = containerUpLocal * (this.containerHeight - this.sphereRadius);

            // ray from the center of the container opening, towards the bottom of the liquid, in the plane of the container opening
            // points towards the edge of the container where the liquid is likely to spill
            Vector3 ray = Vector3.down - Vector3.Dot(Vector3.down, containerUpLocal) * containerUpLocal;

            ray = ray.normalized;

            // project sphere center onto ray 
            Vector3 projCenter = containerOpeningCenter + Vector3.Dot(-containerOpeningCenter, ray) * ray;
            // radius of a circle centered on projCenter coplanar with ray
            float radiusAroundProj = Mathf.Sqrt(Mathf.Max(0, this.sphereRadius * this.sphereRadius - projCenter.sqrMagnitude));

            // get the maximum volume capacity of the container at that angle with the liquid
            maxContainerCapacity = this.HeightToVolume(projCenter.y + ray.y * radiusAroundProj);

            if (this.volume > maxContainerCapacity)
            {
                float flowRate = 2 + Vector3.Dot(containerUpLocal, gravityLocal);
                flowRate = Mathf.Pow(1 - this.friction / flowRate, Time.deltaTime * flowRate * 20);

                float volumeLoss = (this.volume - maxContainerCapacity) * (1 - flowRate);

                this.volume -= volumeLoss;

                this.Spill(volumeLoss, projCenter + ray * radiusAroundProj);
            }



            Debug.DrawLine(this.transform.TransformPoint(containerOpeningCenter), this.transform.TransformPoint(containerOpeningCenter + ray.normalized * radiusAroundProj), Color.blue);
        }

        private void Spill(float volume, Vector3 direction)
        {
            var rigidbody = MakeSpillParticle();
            rigidbody.mass = volume;
            rigidbody.velocity = direction.normalized * this.pendulumSpeed.magnitude;
            rigidbody.drag = 2;

            float radius = SphereVolumeToRadius(volume);
            rigidbody.transform.localScale = Vector3.Scale(this.transform.localScale, Vector3.one * 2 * radius);
            rigidbody.transform.position = this.transform.TransformPoint(direction);
        }

        public static float SphereVolumeToRadius(float v)
        {
            return Mathf.Pow(v * 3 / 4 / Mathf.PI, 1f / 3f);
        }

        public static float SphereRadiusToVolume(float r)
        {
            return r * r * r * Mathf.PI * 4 * 3;
        }

        private void UpdateRenderer()
        {
            sphereRenderer.enabled = volume > 0;

            float height = this.NormalizedLiquidHeight;

            sphereRenderer.material.SetFloat("_LiquidLevel", height);
            sphereRenderer.material.SetFloat("_Radius", sphereRadius);
            sphereRenderer.material.SetFloat("_WavesIntensity", rippleMagnitude);

            if (particleEmitter)
            {
                particleEmitter.transform.localPosition = Vector3.Scale(new Vector3(1, 0, 1), particleEmitter.transform.localPosition) + height * this.sphereRadius * Vector3.up;

                EmissionModule emission = particleEmitter.emission;
                emission.rateOverTimeMultiplier = SurfaceRadius / sphereRadius;
            }
        }

        public void Start()
        {
            InitPendulum();
        }

        public void Update()
        {
            UpdatePendulum();

            UpdateSpill();

            UpdateRenderer();
        }
    }

    public class Droplet : MonoBehaviour
    {
        public void OnCollisionStay(Collision collision)
        {
            if (collision.collider.gameObject.isStatic)
            {
                this.transform.localScale *= Mathf.Exp(-Time.deltaTime / 5);
                if (this.transform.localScale.x <= 0.0001f) this.gameObject.SetActive(false);
            }
        }
    }
}