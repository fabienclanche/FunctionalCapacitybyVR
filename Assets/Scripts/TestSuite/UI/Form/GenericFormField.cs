using UnityEngine;
using Utils;
using System;
using System.Runtime.Serialization;
using System.Collections.Generic;

namespace TestSuite.UI.Form
{
    [DataContract]
    public class GenericFormField
    {
        [DataMember(Order = 0, IsRequired = true)] public string name;
        /// <summary>
        /// Type of field to display in the form, possibles values are fesi; textarea; integer; integerslider; password; textfield; checkbox; list;
        /// </summary>
        [DataMember(Order = 1, IsRequired = true)] public string type;
        [DataMember(Order = 2)] public object defaultValue;
        [DataMember(Order = 3, EmitDefaultValue = false)] public bool required = false;
        [DataMember(Order = 4, EmitDefaultValue = false)] public bool hasRange = false;
        [DataMember(Order = 5, EmitDefaultValue = false)] public float min = 0, max = 0;
        [DataMember(Order = 7, EmitDefaultValue = false)] public object[] possibleValues;
        [DataMember(Order = 8, EmitDefaultValue = false)] public string missingValueMessage;

        [DataMember(Order = 100, EmitDefaultValue = false)] public Metadata metadata;

        public object value = null;
        public string stringValue = null;

        public string errorMessage = null;

        private Vector2 listScroll = Vector2.zero;
        private int layoutHeight = 0;

        public GenericFormField Clone()
        {
            var clone = new GenericFormField();

            clone.name = name;
            clone.type = type;
            clone.defaultValue = defaultValue;
            clone.required = required;
            clone.hasRange = hasRange;
            clone.min = min;
            clone.max = max;
            if (possibleValues != null)
            {
                clone.possibleValues = new object[possibleValues.Length];
                for (int i = 0; i < possibleValues.Length; i++) clone.possibleValues[i] = possibleValues[i];
            }
            clone.missingValueMessage = missingValueMessage;
            clone.metadata = metadata != null ? metadata.Clone() : null;

            return clone;
        }

        public bool Validate()
        {
            // skip validation for invalid field specs
            if (!FieldSpecIsValid) return true;

            if (required)
            {
                bool hasValue = value != null && (value as string) != "";

                if (type == "fesi" && ((value as int?) ?? 0) == 0) hasValue = false;

                if (type == "list" && (possibleValues?.Length ?? 0) == 0) return true;

                if (!hasValue)
                {
                    errorMessage = missingValueMessage ?? "$form:error:required";
                    return false;
                }
            }

            if (hasRange)
            {
                float floatValue = (value as float? ?? value as int? ?? 0);
                if (floatValue < min || floatValue > max)
                {
                    errorMessage = "$form:error:outOfBounds " + min + " - " + max;
                    return false;
                }
            }

            errorMessage = null;
            return true;
        }

        public int LayoutHeight(float width)
        {
            if (layoutHeight > 0) return layoutHeight;

            float labelHeight = 10 + GUI.skin.label.CalcHeight(new GUIContent(Localization.LocalizeDefault(this.name)), width / 2 - 10);
            float defaultHeight = 50;

            if (type == "textarea") defaultHeight = 150;

            else if (type == "list") defaultHeight = 150;

            return layoutHeight = (int)Mathf.Max(defaultHeight, labelHeight);
        }

        public bool Error => this.errorMessage != null;

        public bool FieldSpecIsValid =>
            (type == "fesi"
            || type == "checkbox"
            || type == "yesno"
            || (type == "list" && (possibleValues?.Length ?? 0) > 0)
            || type == "password"
            || type == "textfield"
            || type == "textarea"
            || type == "integer"
            || type == "integerslider")
            && (!hasRange || (max > min));

        public void FieldGUI(ref Vector2 position, float maxWidth)
        {
            if (value == null && defaultValue != null) value = defaultValue;

            var oldValue = value;
            var margin = Vector2.one * 5;
            var title = Localization.LocalizeDefault(this.name);
            var height = LayoutHeight(maxWidth);

            GUI.color = (Error ? Color.red : Color.white) / 2f;
            GUI.Box(new Rect(position, new Vector2(maxWidth - position.x * 2, height)), "");

            GUI.color = Color.white;
            GUI.Label(new Rect(position + margin, new Vector2(maxWidth / 2, height) - margin * 2), title);

            Vector2 defaultFieldOrigin = position + new Vector2(maxWidth / 2 + 5, 15);

            GUI.color = Error ? Color.red : Color.white;

            switch (type)
            {
                case "fesi":
                    for (int i = 1; i <= 4; i++)
                    {
                        bool selected = ((value as int?) ?? 0) == i;
                        GUI.color = selected ? Color.cyan * 2 : Color.white;
                        if (GUI.Toggle(new Rect(defaultFieldOrigin + Vector2.right * (i - 1) * 40, new Vector2(35, 20)), selected, " " + i))
                            value = i;
                        else if (selected)
                            value = 0;
                    }
                    break;

                case "yesno":
                    if (value == null || !(value is bool)) value = false;
                    GUI.color = (value as bool? == true) ? Color.cyan * 2 : Color.white;
                    value = GUI.Toggle(new Rect(defaultFieldOrigin, new Vector2(75, 20)), (bool)value, Localization.LocalizeDefault("$yes"));
                    GUI.color = (value as bool? == false) ? Color.cyan * 2 : Color.white;
                    value = !GUI.Toggle(new Rect(defaultFieldOrigin + Vector2.right * 80, new Vector2(75, 20)), !(bool)value, Localization.LocalizeDefault("$no"));
                    break;

                case "checkbox":
                    if (value == null || !(value is bool)) value = false;
                    GUI.color = (value as bool? == true) ? Color.cyan * 2 : Color.white;
                    value = GUI.Toggle(new Rect(defaultFieldOrigin, new Vector2(maxWidth / 2 - 20, 20)), (bool)value, "");
                    break;

                case "list":
                    if ((possibleValues?.Length ?? 0) > 0)
                        MainUI.SelectFromList(ref value, i => possibleValues[i], possibleValues?.Length ?? 0,
                            new Rect(defaultFieldOrigin, new Vector2(maxWidth / 2 - 20, 120)), ref listScroll, v => "" + v);
                    break;

                case "password":
                    value = GUI.PasswordField(new Rect(defaultFieldOrigin, new Vector2(maxWidth / 2 - 20, 20)), value as string ?? "", '*');
                    break;

                case "textfield":
                    value = GUI.TextField(new Rect(defaultFieldOrigin, new Vector2(maxWidth / 2 - 20, 20)), value as string ?? "");
                    break;

                case "textarea":
                    value = GUI.TextArea(new Rect(defaultFieldOrigin, new Vector2(maxWidth / 2 - 20, 120)), value as string ?? "");
                    break;

                case "integer":
                case "integerslider":
                    int intValue;

                    if (hasRange && type == "integerslider")
                    {
                        float sliderWidth = Mathf.Min(200, maxWidth / 2 - 100);

                        intValue = value as int? ?? (int)min;
                        value = (int)GUI.HorizontalSlider(new Rect(defaultFieldOrigin + Vector2.right * 75, new Vector2(sliderWidth, 20)), intValue, min, max);

                        GUI.Label(new Rect(defaultFieldOrigin + new Vector2(65, -15), new Vector2(30, 20)), (int)min + "");
                        GUI.Label(new Rect(defaultFieldOrigin + new Vector2(sliderWidth + 65, -15), new Vector2(30, 20)), (int)max + "");
                    }

                    GUI.SetNextControlName(title + "_integerslider");
                    string newValue = GUI.TextField(new Rect(defaultFieldOrigin, new Vector2(45, 20)), stringValue ?? "").Trim();

                    if (GUI.GetNameOfFocusedControl() != title + "_integerslider")
                    {
                        stringValue = value + "";
                    }
                    else
                    {
                        stringValue = newValue;
                        if (stringValue.Length == 0) value = 0;
                        else if (int.TryParse(stringValue, out intValue)) value = intValue;
                    }

                    break;
            }

            GUI.color = Color.white;

            if (this.Error)
            {
                GUI.color = Color.red;
                GUI.Label(new Rect(position + new Vector2(maxWidth / 2 + 50, height - 18), new Vector2(maxWidth / 2 - 60, 50)), Localization.LocalizeDefault(this.errorMessage));
                GUI.color = Color.white;
            }

            if (value != oldValue)
            {
                this.Validate();
                if (oldValue == null) errorMessage = null;
            }

            position.y += height;
        }
    }
}
