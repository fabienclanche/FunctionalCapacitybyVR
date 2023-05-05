using UnityEngine;
using Utils;
using System;

namespace TestSuite.UI.Form
{
	public class PasswordFormElement : FormElement
	{
		string value;

		public override object Value { get { return value; } set { this.value = "" + value; } }

		public override bool IsOK => value.Length > 0;

		public override string ErrorMessage => "$form:error:pwdrequired";

		public override event Action onValueChanged;

		public void OnGUI()
		{
			var tr = this.GetComponent<RectTransform>();
			var corners = new Vector3[4];
			tr.GetWorldCorners(corners);

			var pos = new Vector2(corners[0].x, Screen.height - corners[0].y);
			var dimensions = corners[2].xy() - corners[0].xy();			
			pos.y -= dimensions.y;

			value = GUI.TextField(new Rect(pos, dimensions), value ?? "" );

			GUI.Toggle(new Rect(10, 10, 100, 30), true, "cocuou"); 
		}
	}
}
