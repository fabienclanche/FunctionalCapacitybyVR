using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.Events;

namespace TestSuite.UI
{
    public interface InputHelper
    {
        void Update();
    }


    public class ButtonInput : InputHelper
    {
        private Button button;
        private Func<bool> active;
        private UnityAction action;
        private Func<string> label;

        private Text buttonText;

        public ButtonInput(Button button, Func<string> label, Func<bool> active, UnityAction action)
        {
            this.button = button;
            this.active = active;
            this.action = action;
            this.label = label;

            buttonText = button.GetComponentInChildren<Text>();

            button.onClick.AddListener(action);

            Update();
        }

        public void Update()
        {
            buttonText.text = label();
            button.interactable = active();
        }
    }

    public class HoldInput : InputHelper
    {
        private Func<bool> inputOk;
        private float requiredHoldTime, heldTime = 0;
        private Action action;

        public HoldInput(Func<bool> inputOk, float holdTime, Action action)
        {
            this.inputOk = inputOk;
            this.requiredHoldTime = holdTime;
            this.action = action;
        }

        public void Update()
        {
            if (inputOk()) heldTime += Time.deltaTime;
            else heldTime = 0;

            if (heldTime > requiredHoldTime)
            {
                action();
                heldTime = 0;
            }
        }
    }

}
