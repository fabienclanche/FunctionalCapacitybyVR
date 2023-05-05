using UnityEngine;
using Utils;
using System;
using System.Runtime.Serialization;
using System.Collections.Generic;

namespace TestSuite.UI.Form
{
	[DataContract]
	public class GenericForm
	{
		[DataMember(Order = 0)] public string name;
		[DataMember(Order = 1)] public string text;
		[DataMember(Order = 2)] public List<GenericFormField> fields = new List<GenericFormField>();

		public Rect formRect;
		private Vector2 scrollPosition;
		public int elementsPadding = 5;

		public bool Validated { get; private set; }
		public string confirmForm = "$form:button:validate";

		public GenericForm()
		{

		}

		public GenericForm(Rect formRect)
		{
			this.formRect = formRect;
		}

		public GenericForm Clone()
		{
			var clone = new GenericForm();
			clone.name = name;
			clone.text = text;
			clone.fields = new List<GenericFormField>();
			foreach (var field in fields) clone.fields.Add(field.Clone());

			return clone;
		}

		public void ValidateForm()
		{
			bool ok = true;

			foreach (var field in this.fields) if (field.FieldSpecIsValid) ok = field.Validate() && ok;

			this.Validated = ok;
		} 

		public void SetUnvalidated()
		{
			this.Validated = false;
		}

		public bool FormButton(int index, string label)
		{
			Rect scaledFormRect = new Rect(formRect.x * Screen.width, formRect.y * Screen.height,
				formRect.width * Screen.width, formRect.height * Screen.height);

			return GUI.Button(new Rect(scaledFormRect.width - 150 * (index + 1), scaledFormRect.height - 40, 125, 30), Localization.LocalizeDefault(label));
		}

		public void FormGUI(string otherButton1 = null, Action otherButtonAction1 = null)
		{
			int totalHeight = 10 + elementsPadding;

			Rect scaledFormRect = new Rect(formRect.x * Screen.width, formRect.y * Screen.height,
				formRect.width * Screen.width, formRect.height * Screen.height);

			foreach (var field in this.fields) if (field.FieldSpecIsValid) totalHeight += field.LayoutHeight(scaledFormRect.width - 20) + elementsPadding;
			
			GUI.Window(1001, scaledFormRect, (id) =>
			{
				GUI.Box(new Rect(Vector2.zero, scaledFormRect.size), "");
				GUI.Label(new Rect(new Vector2(5, 25), new Vector2(scaledFormRect.width - 10, 120)), Localization.LocalizeDefault(this.text));

				// form scroll view
				Rect viewportRect = new Rect(new Vector2(0, 150), scaledFormRect.size - new Vector2(0, 200));
				
				scrollPosition = GUI.BeginScrollView(viewportRect, scrollPosition, new Rect(0, 0, scaledFormRect.width - 20, Mathf.Max(viewportRect.height, totalHeight))); ;
				{
					Vector2 position = new Vector2(elementsPadding, 10 + elementsPadding);

					foreach (var field in this.fields) if (field.FieldSpecIsValid)
						{
							field.FieldGUI(ref position, scaledFormRect.width - 20);
							position.y += elementsPadding;
						}
				}
				GUI.EndScrollView();

				// confirm button
				if (GUI.Button(new Rect(scaledFormRect.width - 150, scaledFormRect.height - 40, 125, 30), Localization.LocalizeDefault(confirmForm)))
				{
					ValidateForm();
				}

				// other button 1
				if (otherButton1 != null &&
					GUI.Button(new Rect(scaledFormRect.width - 300, scaledFormRect.height - 40, 125, 30), Localization.LocalizeDefault(otherButton1)))
				{
					otherButtonAction1?.Invoke();
				}

			}, Localization.LocalizeDefault("<b>" + this.name + "</b>"));
		}
	}
}
