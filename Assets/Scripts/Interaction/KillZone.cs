using UnityEngine; 

namespace Interaction
{
    public class KillZone : MonoBehaviour
    {
		public bool killsInteractiveObjects = true;

		public void OnTriggerEnter(Collider col)
		{
			if (killsInteractiveObjects)
			{
				var iobj = col.gameObject.GetComponent<InteractiveObject>();
				if (iobj) Destroy(iobj.gameObject);
			}
		}
	}
}