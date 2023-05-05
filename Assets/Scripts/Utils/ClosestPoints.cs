using UnityEngine;
using System.Collections;

public class ClosestPoints : MonoBehaviour
{
	public LineRenderer line;
	public Collider collider1, collider2;
	public int iterations = 10;

	void Update()
	{
		line.positionCount = iterations * 2;
		line.startColor = Color.blue;
		line.endColor = Color.red;
		line.startWidth = 0.01f;
		line.endWidth = 0.05f;

		Vector3 p1 = collider1.transform.position;
		Vector3 p2 = collider2.transform.position;

		bool converged = false;

		for (int i = 0; i < iterations; i++)
		{
			if (!converged)
			{
				var old1 = p1;
				var old2 = p2;

				var tmp = collider1.ClosestPoint(p2);
				p2 = collider2.ClosestPoint(p1);
				p1 = tmp;


				float conv = (p1 - old1).sqrMagnitude + (p2 - old2).sqrMagnitude; 
				converged = (conv < 0.001f * 0.001f);
			}

			if (i % 2 == 0)
			{
				line.SetPosition(i * 2, p1);
				line.SetPosition(i * 2 + 1, p2);
			}
			else
			{
				line.SetPosition(i * 2, p2);
				line.SetPosition(i * 2 + 1, p1);
			}

		}

		Debug.Log(Vector3.Distance(p1, p2));

		Debug.Log(Mathf.Abs(p1.y - p2.y));
	}
}
