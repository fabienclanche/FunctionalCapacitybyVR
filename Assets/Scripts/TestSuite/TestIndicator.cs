using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TestSuite
{
	[AttributeUsage(AttributeTargets.Field)]
	public class InitAttribute : Attribute
	{
		internal object @default;

		public InitAttribute(object value)
		{
			this.@default = value;
		}
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class IndicatorValueAttribute : Attribute
	{

	}

	[DataContract, AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class Metadata : Attribute
	{
		[DataMember(EmitDefaultValue = false)] public string unit;
		[DataMember(EmitDefaultValue = false)] public string label;
		[DataMember(EmitDefaultValue = false)] public string description;
		[DataMember(EmitDefaultValue = false)] public string aggregation;
		[DataMember(EmitDefaultValue = false)] public int importance;
		[DataMember(EmitDefaultValue = false)] public int decimalPlaces;
		[DataMember(EmitDefaultValue = false)] public string aggregationWeightAttribute;
		public bool debug_only;

		public Metadata Clone()
		{
			var clone = new Metadata();
			clone.unit = unit;
			clone.label = label;
			clone.description = description;
			clone.debug_only = debug_only;
			clone.aggregation = aggregation;
			clone.importance = importance;
			clone.decimalPlaces = decimalPlaces;
			clone.aggregationWeightAttribute = aggregationWeightAttribute;
			return clone;
		}
	}

	[RequireComponent(typeof(Test))]
	public abstract class TestIndicator : MonoBehaviour
	{
		public abstract string Name { get; }

		public virtual Metadata Metadata => new Metadata();

		public bool Recording { get { return Test != null; } }

		public Test Test { get; private set; }

		public virtual string DebugMessage => null;

		public void StartRecording(Test test)
		{
			if (!Recording)
			{
				this.Test = test;
				InitFields();
				Begin();
			}
		}

		public void StopRecording()
		{
			if (Recording)
			{
				End();

				ForEachIndicator(SetUsingGetter);

				this.Test = null;
			}
		}

		protected void InitFields()
		{
			foreach (var field in GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(prop => prop.IsDefined(typeof(InitAttribute), false)))
			{
				field.SetValue(this, field.GetCustomAttribute<InitAttribute>().@default);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="action">A function that take as parameters the name of a field and a function that computes and return its value</param>
		internal void ForEachIndicator(Action<string, Metadata, Func<object>> action)
		{
			string prefix = "$field:" + this.GetType().Name + ":";

			foreach (var field in GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(prop => prop.IsDefined(typeof(IndicatorValueAttribute), false)))
				action(field.Name, field.GetCustomAttribute<Metadata>(), () => field.GetValue(this));

			foreach (var property in GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(prop => prop.IsDefined(typeof(IndicatorValueAttribute), false)))
				action(property.Name, property.GetCustomAttribute<Metadata>(), () => property.GetValue(this));
		}

		protected abstract void Begin();

		protected abstract void End();

		protected abstract void RecordFrame();


		// Update is called once per frame
		void Update()
		{
			if (Recording) RecordFrame();
		}

		public void SetUsingGetter(string fieldname, Metadata metadata, Func<object> getter)
		{
			SetUsingGetter(fieldname, "$field:" + this.GetType().Name + ":", metadata, getter);
		}

		public void SetUsingGetter(string fieldname, string prefix, Metadata metadata, Func<object> getter)
		{
			try
			{
				if (metadata != null && metadata.debug_only) return;

				IndicatorField field;

				field = new IndicatorField(this.Name, prefix + fieldname);
				Test.AddIndicatorField(field);

				field.Set(getter());
				field.metadata = metadata?.Clone();
				if (field.metadata?.aggregationWeightAttribute != null) field.metadata.aggregationWeightAttribute = prefix + field.metadata.aggregationWeightAttribute;

			}
			catch (Exception e)
			{
				Debug.LogError(fieldname + "\n" + e.GetBaseException());
			}
		}
	}


#if UNITY_EDITOR
	[CustomEditor(typeof(TestIndicator), true)]
	public class TestIndicatorEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			var indicatorComponent = this.target as TestIndicator;

			base.OnInspectorGUI();

			string indicatorsDisplay = "";
			int errors = 0;
			int total = 0;

			indicatorComponent.ForEachIndicator((name, attr, getter) =>
			{
				string val = "";

				try
				{
					val = "" + getter();
				}
				catch (Exception e)
				{
					val = "Error! " + e.GetBaseException().Message;
					errors++;
				}

				indicatorsDisplay += name + ": " + val + "\n";
				total++;
			});

			if (total > 0)
			{
				if (errors > 0)
					EditorGUILayout.HelpBox(total + " INDICATORS, " + errors + " UNREADABLE\n\n" + indicatorsDisplay, MessageType.Warning);
				else
					EditorGUILayout.HelpBox(total + " INDICATORS\n\n" + indicatorsDisplay, MessageType.Info);
			}
			else
			{
				EditorGUILayout.HelpBox("NO INDICATORS TO RECORD", MessageType.Info);
			}

			var objective = indicatorComponent as TestObjective;

			if (objective)
			{
				EditorGUILayout.HelpBox((objective.ConditionVerified ? "Objective condition OK" : "Objective condition not verified").ToUpper(), MessageType.Info);
			}

			EditorGUILayout.HelpBox(indicatorComponent.DebugMessage, MessageType.None);
		}
	}
#endif
}