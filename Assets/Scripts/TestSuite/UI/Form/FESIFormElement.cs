using System;
using UnityEngine;
using UnityEngine.UI;

namespace TestSuite.UI.Form
{
    public class FESIFormElement : FormElement
    {
        public int IntValue
        {
            get { return (toggle1 && toggle2 && toggle3 && toggle4) ? (toggle4.isOn ? 4 : toggle3.isOn ? 3 : toggle2.isOn ? 2 : toggle1.isOn ? 1 : 0) : 0; }
            set
            {
                toggle1.isOn = (value == 1);
                toggle2.isOn = (value == 2);
                toggle3.isOn = (value == 3);
                toggle4.isOn = (value == 4);
            }
        }
        public override object Value
        {
            get { return IntValue; }
            set
            {
                if (value is int) IntValue = (int)value;
                else IntValue = 0;
            }
        }

        public Toggle toggle1, toggle2, toggle3, toggle4;

        public override event Action onValueChanged;

        public override bool IsOK => IntValue > 0;

        public override string ErrorMessage => "$form:error:fesirequired";

        public override void Start()
        {
            base.Start();

            toggle1.onValueChanged.AddListener(b => onValueChanged?.Invoke());
            toggle2.onValueChanged.AddListener(b => onValueChanged?.Invoke());
            toggle3.onValueChanged.AddListener(b => onValueChanged?.Invoke());
            toggle4.onValueChanged.AddListener(b => onValueChanged?.Invoke());
        }
    }
}
