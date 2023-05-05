using UnityEngine;
using Utils;
using System.Runtime.Serialization;
using System;
using System.Collections.Generic;

namespace FullBodyTracking.Mocap
{
    [Serializable, DataContract]
    public class MocapMetadata
    {
        [DataMember(Order = 0)] public string scene;
        [DataMember(Order = 1)] public Vector3 position;
        [DataMember(Order = 2)] public Vector3 rotation;
        [DataMember(Order = 3)] public Vector3 scale;

        [DataMember(Order = 6)] public float modelScale, armScale, legScale;
        [DataMember(Order = 7)] public float modelScaleF, armScaleF, legScaleF;

        [DataMember(Order = 4)] public List<string> accessibilityModeDevices;

        public void ApplyTo(Transform @ref, CalibratedIK avatarM, CalibratedIK avatarF)
        {
            @ref.position = this.position;
            @ref.rotation = Quaternion.Euler(this.rotation);
            @ref.localScale = new Vector3(this.scale.x / @ref.lossyScale.x, this.scale.y / @ref.lossyScale.y, this.scale.z / @ref.lossyScale.z);

            avatarM.ModelScale = this.modelScale;
            avatarM.ArmScale = this.armScale;
            avatarM.LegScale = this.legScale;

            // if female avatar scale is 0, defaults to male scale
            avatarF.ModelScale = this.modelScaleF > 0 ? this.modelScaleF : this.modelScale;
            avatarF.ArmScale = this.armScaleF > 0 ? this.armScaleF : this.armScale;
            avatarF.LegScale = this.legScaleF > 0 ? this.legScaleF : this.legScale;

            BodyPart degradedModePart = (BodyPart)(-1);

            if (accessibilityModeDevices != null)
                foreach (var dev in this.accessibilityModeDevices)
                    if (Enum.TryParse(dev, out degradedModePart))
                    {
                        avatarM.SetEffectorDegradedMode(degradedModePart, true);
                        avatarF.SetEffectorDegradedMode(degradedModePart, true);
                    }
        }

        public static implicit operator MocapMetadata(string jsonData)
        {
            return JSONSerializer.FromJSON<MocapMetadata>(jsonData);
        }

        public static implicit operator string(MocapMetadata data)
        {
            return JSONSerializer.ToJSON(data);
        }
    }
}