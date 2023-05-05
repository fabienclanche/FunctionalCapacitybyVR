using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization;

namespace TestSuite
{
	[DataContract, Serializable]
	public class IndicatorField
	{
		[DataMember(Order = 0), SerializeField] private string category;
		[DataMember(Order = 1), SerializeField] private string name;
		[DataMember(Order = 2), SerializeField] private object value; 
		[DataMember(Order = 3, EmitDefaultValue = false), SerializeField] public Metadata metadata;
		private string[] categories;

		public string[] Categories => categories ?? (categories = category?.Split('.'));
		public string Name => name;		
		public object Value => value;
		public double? DoubleValue => 
			value is decimal ? 
				decimal.ToDouble((decimal) value) : 
				value as double? ?? value as float? ?? value as long? ?? value as int?;

		public string CategoryID
		{
			get
			{
				if ((Categories?.Length ?? 0) == 0) return "";

				string categoryName = categories[0];
				for (int i = 1; i < categories.Length; i++) categoryName = categories[i] + "." + categoryName;
				return categoryName;
			}
		}

		public string FullName => CategoryID + "." + Name;

		public IndicatorField()
		{

		}

		public IndicatorField(string category, string name)
		{
			this.category = category;
			this.name = name;
			this.value = null;
		}

		public void Set<T>(T value)
		{
			this.value = value;
		}


		public T Get<T>()
		{
			return (T)value;
		}
	}
}