using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
	public class Pool
	{
		private GameObject @object;
		private List<GameObject> poolList;
		private int index = 0; 

		public Pool(GameObject @object, int size, bool preInstantiate = false)
		{
			if (size <= 0) throw new ArgumentException("Invalid pool size: " + size);

			this.@object = @object ?? throw new ArgumentException("Cannot build a pool of null objects");

			poolList = new List<GameObject>(size);

			if (preInstantiate)
			{
				while (poolList.Count < size) poolList.Add(Instantiate());
			}

			while (poolList.Count < size) poolList.Add(null);
		}

        public void ClearAllInstances()
        {
            foreach (var o in poolList) if (o) o.SetActive(false);
        }

		private GameObject Instantiate()
		{
			var o = GameObject.Instantiate(@object);

			o.SetActive(false);

			return o;
		}

		public void Create(Vector3 position, Quaternion rotation, Transform parent = null)
		{
			if (index >= poolList.Count) index = 0;

			if(!poolList[index])
			{
				poolList[index] = Instantiate();
			}

			GameObject @object = poolList[index];
			@object.SetActive(true);

			@object.transform.parent = parent;
			@object.transform.position = position;
			@object.transform.rotation = rotation;
			
			index++;
		}
	}
}