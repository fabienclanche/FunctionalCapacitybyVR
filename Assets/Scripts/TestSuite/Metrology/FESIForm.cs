using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TestSuite.UI.Form;
using Utils;

namespace TestSuite.Metrology
{
	public class FESIForm : TestObjective
	{
		public string formName;
		public string[] questionLabels = { "$fesi:Q1dress", "$fesi:Q2bath", "$fesi:Q3chair", "$fesi:Q4stairs", "$fesi:Q5reach", "$fesi:Q6slope", "$fesi:Q7goingout" };
		public string headerTextContent = "$fesi:intro";

		private GenericForm fesiForm;

		public override bool ConditionVerified => fesiForm != null && fesiForm.Validated;

		public override string Name => formName;

		protected override void Begin()
		{
			fesiForm = new GenericForm(new Rect(0.35f, 0.05f, 0.6f, .9f));
			fesiForm.text = headerTextContent;
			fesiForm.name = formName;

			for (int i = 0; i < questionLabels.Length; i++)
			{
				var field = new GenericFormField();
				field.name = questionLabels[i];
				field.type = "fesi";
				field.value = 0;
				field.defaultValue = 0;
				field.required = true;
				field.missingValueMessage = "$form:error:fesirequired";
				fesiForm.fields.Add(field);
			}
		}

		public void OnGUI()
		{
			if (fesiForm != null) fesiForm.FormGUI();
		}

		protected override void End()
		{
			if (fesiForm.Validated)
			{
				for (int i = 0; i < fesiForm.fields.Count; i++)
				{
					SetUsingGetter(fesiForm.fields[i].name, fesiForm.fields[i].metadata, () => fesiForm.fields[i].value);
				}
			}

			fesiForm = null;
		}

		protected override void RecordFrame()
		{

		}
	}
}