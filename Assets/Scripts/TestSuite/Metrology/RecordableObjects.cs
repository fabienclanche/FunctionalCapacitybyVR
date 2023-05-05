using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using FullBodyTracking.Mocap;

namespace TestSuite.Metrology
{
    public class RecordableObjects : TestIndicator
    {
		public List<RecordableObject> recordableObjects;
		  
        public override string Name => "";

        protected override void Begin()
        {
            foreach(var robj in recordableObjects)
			{
				Test.Mocap.AddRecordableObject(robj);
			}
        }

        protected override void End()
        {
            
        }

        protected override void RecordFrame()
        {
            
        }
    }
}