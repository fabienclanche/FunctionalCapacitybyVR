using UnityEngine;
using Utils;
using System;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TestSuite.UI.Form
{
    public abstract class FormElement : MonoBehaviour
    {
        [SerializeField] Text label;
        public Image background;
        public Text errorMessage;

        public Color backgroundDefaultColor = new Color(1, 1, 1, 100 / 255f);

        public string Label
        {
            get { return label.text; }
            set { label.text = Localization.LocalizeDefault(value); }
        }

        public abstract object Value { get; set; }

        public abstract bool IsOK { get; }

        public abstract string ErrorMessage { get; }

        public abstract event Action onValueChanged;

        public virtual void Start()
        {
            onValueChanged += () =>
            {
                if (IsOK) ClearErrorMessage();
                else SetErrorMessage(ErrorMessage);
            };
            ClearErrorMessage();
        }

        public void SetErrorMessage(string message)
        {
            if (this.errorMessage) this.errorMessage.text = Localization.LocalizeDefault(message);
            if (this.background) this.background.color = backgroundDefaultColor * new Color(1, 0.7f, 0.7f, 1);
        }

        public void ClearErrorMessage()
        {
            if (this.errorMessage) this.errorMessage.text = "";
            if (this.background) this.background.color = backgroundDefaultColor;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(FormElement), true)]
    public class FormElementEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var formElement = this.target as FormElement;

            base.OnInspectorGUI();

            EditorGUILayout.HelpBox("Value: " + formElement.Value + " " + (formElement.IsOK ? "[OK]" : ""), MessageType.Info);
        }
    }
#endif
}
