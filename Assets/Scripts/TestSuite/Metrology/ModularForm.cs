using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TestSuite.UI.Form;
using Utils;

namespace TestSuite.Metrology
{
	public class ModularForm : TestObjective
	{
		public string formName;
		public bool useStudyConfig = false;

		public Vector2 formPosition = new Vector2(.35f, .05f);
		public Vector2 formDimensions = new Vector2(.60f, .90f);

		private GenericForm[] formPages = null;
        
		public int currentFormIndex = 0;

		public GenericForm CurrentForm => formPages == null ? null : currentFormIndex >= 0 && currentFormIndex < formPages.Length ? formPages[currentFormIndex] : null;

		public override bool ConditionVerified => formPages != null && currentFormIndex == formPages.Length;

		public override string Name => formName;

		protected override void Begin()
		{
			if (useStudyConfig) formPages = this.Test.Suite.StudyConfig.MakeForms();

			currentFormIndex = 0;
		}

		protected override void End()
		{
			foreach (var form in formPages)
			{
				this.formName = form.name;

				if (form.Validated)
				{
					for (int i = 0; i < form.fields.Count; i++)
					{
						var metadata = form.fields[i].metadata?.Clone() ?? new Metadata();
						metadata.importance = 1;						

						SetUsingGetter(form.fields[i].name, metadata, () => form.fields[i].value);
					}
				}
			}
		}

		public void OnGUI()
		{
			if (CurrentForm != null)
			{
				CurrentForm.formRect = new Rect(formPosition, formDimensions);
				CurrentForm.FormGUI(otherButton1: currentFormIndex > 0 ? "$form:button:previous" : null, otherButtonAction1: PreviousForm);
			}
		}

		private void PreviousForm()
		{
			if (currentFormIndex > 0)
			{
				currentFormIndex--;
				CurrentForm.SetUnvalidated();
			}
		}

		private void NextForm()
		{
			currentFormIndex++;
		}

		protected override void RecordFrame()
		{
			if (CurrentForm != null && CurrentForm.Validated) NextForm();
		}
	}
}