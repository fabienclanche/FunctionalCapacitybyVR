using UnityEngine;
using System.Collections;

namespace TestSuite
{ 
    public abstract class TestObjective : TestIndicator
    { 
        public abstract bool ConditionVerified
        {
            get;
        }
    }
}