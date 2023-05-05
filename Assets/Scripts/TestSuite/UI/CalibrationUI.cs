using UnityEngine;
using Utils;
using System.Linq;
using FullBodyTracking;
using System;
using System.Collections.Generic;
using FullBodyTracking.Cosmetics;
using System.Collections;

namespace TestSuite.UI
{
    public class CalibrationUI : View
    {
        public TestSuiteUI testSuiteUI;
        public TPoseCalibrator calibrator;
        public TPoseCalibrator replayCalibrator;
        public Camera calibrationCamera;
        public Texture devicePointer, dot;
        public Color[] colors;

        // data for currently edited tracked object
        private TrackedObject editable;
        private Color editableColor = Color.white;
        private string editableName = "";

        // dev mode shortcuts
        private List<InputHelper> inputHelpers = new List<InputHelper>();

        private int calibrationStepsLeft = 0;

        private int? openedDevicesDropdown = null;
        private Vector2 deviceDropdownScroll;

        // accessibility options
        private BodyPart dontUseControllerOn = (BodyPart)(-1);
        /// <summary>
        /// The tracked object to use as a replacement for a controller in accessibility modes
        /// </summary>
        private TrackedObject replacementTrackedObject = null;

        public override bool HidesCameraView => true;
        public override bool ShowBackgroundScreen => false;
        public override bool AllowsApplicationExit => false;
        public override bool BackButtonEnabled => calibrationStepsLeft <= 0;

        private TestSuite Suite => testSuiteUI.suite;

        public bool ReadyToCalibrate { get; private set; }

        public void Start()
        {
            this.enabled = false;

#if UNITY_EDITOR
            if (UserConfig.DevMode)
            {
                inputHelpers.Add(new HoldInput(
                    () => (Suite.RunningTest == null && (Input.GetAxisRaw("RTrigger") > 0.3 && Input.GetAxisRaw("RTrigger") <= 0.9) || (Input.GetAxisRaw("LTrigger") > 0.3 && Input.GetAxisRaw("LTrigger") <= 0.9)),
                    1f,
                    RunCalibration
                ));

                inputHelpers.Add(new HoldInput(
                    () => Input.GetKey(KeyCode.T) && Input.GetKeyDown(KeyCode.C),
                    .0f,
                    () => StartCoroutine(DebugCalibrationCoroutine())
                ));

                inputHelpers.Add(new HoldInput(
                    () => Input.GetKey(KeyCode.T) && Input.GetKeyDown(KeyCode.Alpha3),
                    .0f,
                    () => calibrator.EnableDummyTrackers()
                ));
            }
#endif
        }

        private IEnumerator DebugCalibrationCoroutine()
        {
            calibrator.StartFullBodyTracking(debug: true);
            yield return null;
            yield return null;
            calibrator.CalibrateScale(); 
        }

        public void Update()
        {
            foreach (var helper in this.inputHelpers) helper.Update();

            if (this.calibrationStepsLeft > 0)
            {
                try
                {
					if(this.calibrationStepsLeft == 3) calibrator.StartFullBodyTracking();
					else if(this.calibrationStepsLeft == 1) calibrator.CalibrateScale(); 
                }
                catch (Exception e) { Debug.LogError(e); }

                this.calibrationStepsLeft--;
                return;
            }
        }

        public override void OnMadeVisible()
        {
            base.OnMadeVisible();
            this.enabled = true;
            this.calibrationStepsLeft = 0;
            this.MainUI.SelectExternalCamera(calibrationCamera);

            if (Suite.IKRig == null)
                dontUseControllerOn = (BodyPart)(-1);

            if (UserConfig.AccessibilityDeviceID != 0)
            {
                foreach (var tobj in calibrator.TrackedObjects)
                {
                    if (tobj.id == UserConfig.AccessibilityDeviceID)
                    {
                        this.replacementTrackedObject = tobj;
                    }
                }
            }

            ApplyCalibrationSettings();
        }

        public override void OnClosed()
        {
            this.enabled = false;
            this.MainUI.DisableExternalCamera(calibrationCamera);

            UserConfig.AccessibilityDeviceID = this.replacementTrackedObject?.id ?? UserConfig.AccessibilityDeviceID;
            UserConfig.SaveUserConfig();
        }

        public override void OnViewGUI(Vector2 screen)
        {
            GUI.color = Color.white;

            MainUI.BeginFullscreen();

            if (UserConfig.ShowDevicesLabelsDuringCalibration)
            {
                if (editable)
                {
                    TrackedObjectGUI(editable);
                }
                else
                {
                    if (calibrator.HMD) TrackedObjectGUI(calibrator.HMD);

                    foreach (TrackedObject tobj in calibrator.TrackedObjects) TrackedObjectGUI(tobj);
                }
            }

            MainUI.EndFullScreen();

            AvatarSelectionGUI(screen, Vector2.zero, this.calibrator, this.replayCalibrator);
            AvatarResizeGUI(screen, this.calibrator);
            CalibrationGUI(screen);
            CalibrationStatusGUI(screen);
            CalibrationModeGUI(screen);
        }

        private void ApplyCalibrationSettings()
        {
            TPoseAnchor rightHand = calibrator.GetAnchor(BodyPart.Rhand);
            TPoseAnchor leftHand = calibrator.GetAnchor(BodyPart.Lhand);

            this.ReadyToCalibrate = true;

            ApplyCalibrationMode(rightHand);
            ApplyCalibrationMode(leftHand);
        }

        private void ApplyCalibrationMode(TPoseAnchor anchor)
        {
            if (anchor.bodypart == this.dontUseControllerOn)
            {
                anchor.degradedMode = true;
                anchor.forcePairWith = null;

                if (replacementTrackedObject && anchor.AcceptsPairing(this.replacementTrackedObject))
                {
                    anchor.forcePairWith = this.replacementTrackedObject;
                }
                else
                {
                    this.replacementTrackedObject = null;
                }
            }
            else
            {
                anchor.degradedMode = false;
                anchor.forcePairWith = null;
            }
        }

        private void CalibrationStatusGUI(Vector2 screen)
        {
            GUI.BeginGroup(new Rect(5, 338, 208, 170));
            GUI.Box(new Rect(0, 0, 202, 155), "<b>" + Localization.Format("$ui:calibrationStatus") + "</b>", "window");

            int i = 0;

            bool calibrated = Suite.IKRig;

            GUI.Label(new Rect(5, 5 + (20 * ++i), 198, 20), Localization.Format(calibrated ? "$ui:calibrationOK" : "$ui:calibrationNotOK"));

            CalibrationStatusGUI(new Rect(5, 5 + (20 * ++i), 198, 20), BodyPart.Rhand);
            CalibrationStatusGUI(new Rect(5, 5 + (20 * ++i), 198, 20), BodyPart.Lhand);
            CalibrationStatusGUI(new Rect(5, 5 + (20 * ++i), 198, 20), BodyPart.Back);
            CalibrationStatusGUI(new Rect(5, 5 + (20 * ++i), 198, 20), BodyPart.Rfoot);
            CalibrationStatusGUI(new Rect(5, 5 + (20 * ++i), 198, 20), BodyPart.Lfoot);

            GUI.EndGroup();
        }

        private void CalibrationStatusGUI(Rect rect, BodyPart part)
        {
            var anchor = calibrator.GetAnchor(part);
            string label = Localization.Format("$" + part + " : " + ((anchor.TrackedObject != null) ? "$ui:checkmark " + DeviceToString(anchor.TrackedObject) : "$ui:crossmark $undetected"));

            GUI.Label(rect, label);
        }

        private void CalibrationModeGUI(Vector2 screen)
        {
            GUI.BeginGroup(new Rect(5, 173, 208, 1000));

            GUI.Box(new Rect(0, 0, 202, 155), "<b>" + Localization.Format("$ui:accessibilityOptions") + "</b>", "window");

            GUI.changed = false;

            if (MainUI.ColoredToggle(new Rect(5, 25, 192, 24), dontUseControllerOn == (BodyPart)(-1), Localization.Format("$ui:calibration2Controllers")))
            {
                dontUseControllerOn = (BodyPart)(-1);
            }

            if (MainUI.ColoredToggle(new Rect(5, 50, 192, 24), dontUseControllerOn == BodyPart.Lhand, Localization.Format("$ui:calibrationTrackerLeftHand")))
            {
                dontUseControllerOn = BodyPart.Lhand;
            }

            if (MainUI.ColoredToggle(new Rect(5, 75, 192, 24), dontUseControllerOn == BodyPart.Rhand, Localization.Format("$ui:calibrationTrackerRightHand")))
            {
                dontUseControllerOn = BodyPart.Rhand;
            }

            Rect dropdown = new Rect(5, 122, 192, 22);

            if (dontUseControllerOn != (BodyPart)(-1))
            {
                GUI.Label(new Rect(5, 100, 192, 20), Localization.Format("<b>$ui:linkedTracker :</b>"));

                TPoseAnchor anchor = calibrator.GetAnchor(dontUseControllerOn);
                var compatibleDevices = GetCompatiblesDevices(anchor);

                DevicesDropdown(dropdown, compatibleDevices, -1, ref this.replacementTrackedObject);
            }
            else
            {
                if (this.openedDevicesDropdown != null && ((int)this.openedDevicesDropdown) == -1) ForceCloseDropdown();
                GUI.Box(dropdown, "");
            }

            if (GUI.changed) ApplyCalibrationSettings();

            GUI.EndGroup();
        }

        private void ForceCloseDropdown()
        {
            this.openedDevicesDropdown = null;
            this.deviceDropdownScroll = Vector2.zero;
        }

        private void DevicesDropdown(Rect rect, List<TrackedObject> deviceList, int dropdownId, ref TrackedObject selectedDevice)
        {
            bool open = this.openedDevicesDropdown != null && ((int)this.openedDevicesDropdown) == dropdownId;
            bool changed = MainUI.SelectFromDropdown(rect, ref selectedDevice, i => deviceList[i], deviceList.Count,
                ref open, ref this.deviceDropdownScroll, DeviceToString);

            if (open) this.openedDevicesDropdown = dropdownId;
            else if (this.openedDevicesDropdown == dropdownId) this.openedDevicesDropdown = null;

            if (changed) GUI.changed = true;
        }

        private static string DeviceToString(TrackedObject tobj)
        {
            if (tobj == null)
            {
                return "<color='white>$ui:calibrationAutodetect</color>";
            }

            string deviceLabel;
            Color deviceColor;
            UserConfig.GetDeviceLabel(tobj.id, out deviceLabel, out deviceColor);

            return "<color='#" + ColorUtility.ToHtmlStringRGB(deviceColor) + "'>$ui:dot</color> <color='white>" + deviceLabel + "</color>";
        }

        private List<TrackedObject> GetCompatiblesDevices(TPoseAnchor anchor)
        {
            List<TrackedObject> devices = new List<TrackedObject>();

            devices.Add(null);

            foreach (var device in calibrator.TrackedObjects)
            {
                var forcedPair = anchor.forcePairWith;
                anchor.forcePairWith = null;

                if (anchor.AcceptsPairing(device))
                {
                    devices.Add(device);
                }

                anchor.forcePairWith = forcedPair;
            }

            return devices;
        }

        public void AvatarResizeGUI(Vector2 screen, TPoseCalibrator calibrator)
        {
            var ik = Suite.IKRig;

            float xOffset = 202 + (UserConfig.DevMode ? 66 : 0) + 5;

            if (ik)
            {
                GUI.BeginGroup(new Rect(xOffset, 75, 165, 163));

                GUI.Box(new Rect(5, 5, 155, 81), "");

                GUI.changed = false;
				 
                GUI.Label(new Rect(10, 12, 100, 20), Localization.Format("$ui:avatarSize"));
                if (GUI.Button(new Rect(110, 13, 18, 18), "-")) ik.ModelScale = Mathf.Clamp(ik.ModelScale - 0.025f, 0.5f, 2);
                if (GUI.Button(new Rect(130, 13, 18, 18), "+")) ik.ModelScale = Mathf.Clamp(ik.ModelScale + 0.025f, 0.5f, 2);

                GUI.Label(new Rect(10, 36, 100, 20), Localization.Format("$ui:avatarArmsLength"));
                if (GUI.Button(new Rect(110, 37, 18, 18), "-")) ik.ArmScale = Mathf.Clamp(ik.ArmScale - 0.025f, 0.5f, 2);
                if (GUI.Button(new Rect(130, 37, 18, 18), "+")) ik.ArmScale = Mathf.Clamp(ik.ArmScale + 0.025f, 0.5f, 2);

                GUI.Label(new Rect(10, 60, 100, 20), Localization.Format("$ui:avatarLegsLength"));
                if (GUI.Button(new Rect(110, 61, 18, 18), "-")) ik.LegScale = Mathf.Clamp(ik.LegScale - 0.025f, 0.5f, 2);
                if (GUI.Button(new Rect(130, 61, 18, 18), "+")) ik.LegScale = Mathf.Clamp(ik.LegScale + 0.025f, 0.5f, 2);

                if (GUI.changed)
                {
                    foreach (var avatar in calibrator.AllAvatars())
                    {
                        var cIk = avatar.GetComponent<CalibratedIK>();
                        if (cIk)
                        {
                            cIk.ModelScale = ik.ModelScale;
                            cIk.ArmScale = ik.ArmScale;
                            cIk.LegScale = ik.LegScale;
                        }
                    }
                }

                GUI.EndGroup();
            }
        }

        public static void AvatarSelectionGUI(Vector2 screen, Vector2 position, TPoseCalibrator calibrator, params TPoseCalibrator[] otherCalibrators)
        {
            GUI.color = Color.white;

            float width = 202 + (UserConfig.DevMode ? 66 : 0);

            GUI.BeginGroup(new Rect(position.x, position.y, width + 6, 163));

            GUI.Box(new Rect(5, 5, width, 157), "<b>" + Localization.Format("$ui:avatarSelection") + "</b>", "window");

            int y = 0;

            CharacterSkinSelector selectedAvatar = null;
            int selectedSkin = -1;
            bool changed = false;

            foreach (var bipedIK in calibrator.AllAvatars())
            {
                var avatar = bipedIK.GetComponent<CharacterSkinSelector>();

                if (avatar)
                {
                    for (int i = 0; i < avatar.SkinCount; i++)
                    {
                        var icon = avatar.GetSkinIcon(i);
                        GUI.changed = false;

                        bool currentIsSelected = (avatar.SelectedSkin == i);

                        if (currentIsSelected && selectedAvatar != null) // corrects the state where several avatars are selected
                        {
                            currentIsSelected = false;
                            changed = true;
                        }

                        bool selectNow;
                        selectNow = GUI.Button(new Rect(9 + i * 66, 29 + y * 66, 64, 64), icon);
                        selectNow |= MainUI.ColoredToggle(new Rect(57 + i * 66, 77 + y * 66, 16, 16), currentIsSelected, "") && !currentIsSelected;

                        if (currentIsSelected || selectNow)
                        {
                            selectedAvatar = avatar;
                            selectedSkin = i;
                            changed = selectNow;
                        }
                    }
                }

                y++;
            }

            GUI.EndGroup();

            if (changed)
            {
                foreach (var bipedIK in calibrator.AllAvatars())
                {
                    var avatar = bipedIK.GetComponent<CharacterSkinSelector>();

                    avatar.SelectedSkin = (avatar == selectedAvatar) ? selectedSkin : -1;
                }

                foreach (var cal in otherCalibrators)
                    foreach (var bipedIK in cal.AllAvatars())
                    {
                        var avatar = bipedIK.GetComponent<CharacterSkinSelector>();

                        avatar.SelectedSkin = (avatar.gameObject.name == selectedAvatar.gameObject.name) ? selectedSkin : -1;
                    }
            }
        }

        public void CalibrationGUI(Vector2 screen)
        {
            if (calibrationStepsLeft > 0) return;

            Vector2 buttonSize = new Vector2(180, 65);
            string calibrateLabel = Localization.LocalizeDefault("<b>$ui:calibrateButton</b>");

            GUI.changed = false;
            {
                UserConfig.ShowDevicesLabelsDuringCalibration
                    = MainUI.ColoredToggle(new Rect(screen.x / 2 - 90, screen.y - 150, 240, 24), UserConfig.ShowDevicesLabelsDuringCalibration, Localization.Format("$ui:showDevicesLabelsDuringCalibration"));

                UserConfig.LockRoomOffset
                   = MainUI.ColoredToggle(new Rect(screen.x / 2 - 90, screen.y - 125, 240, 24), UserConfig.LockRoomOffset, Localization.Format("$ui:lockRoomOffset"));
            }
            if (GUI.changed) UserConfig.SaveUserConfig();
             
            if (!Suite.IKRig)
            {
                GUI.color = Color.green;

                if (GUI.Button(new Rect(screen.x / 2 - buttonSize.x / 2, screen.y - (buttonSize.y + 30), buttonSize.x, buttonSize.y), calibrateLabel))
                {
                    RunCalibration();
                }
            }
            else
            {
                GUI.color = Color.white;

                if (GUI.Button(new Rect(screen.x / 2 - buttonSize.x - 5, screen.y - (buttonSize.y + 30), buttonSize.x, buttonSize.y), calibrateLabel))
                {
                    RunCalibration();
                }

                GUI.color = Color.green;

                if (GUI.Button(new Rect(screen.x / 2 + 5, screen.y - (buttonSize.y + 30), buttonSize.x, buttonSize.y), Localization.Format("$ui:goToTestSuite")))
                {
                    MainUI.SwapView(this, testSuiteUI);
                }
            }

            GUI.color = Color.white;
        }

        private void RunCalibration()
        {
            this.calibrationStepsLeft = 3;
        }


        private void TrackedObjectGUI(TrackedObject tobj)
        {
            if (tobj.IgnoreForBodyTracking && !editable) return;

            string deviceLabel;
            Color deviceColor;

            UserConfig.GetDeviceLabel(tobj.id, out deviceLabel, out deviceColor);

            Vector3 screenPoint = calibrationCamera.WorldToScreenPoint(tobj.WorldPosition);
            screenPoint.y = Screen.height - screenPoint.y;

            Rect rect = new Rect(screenPoint.xy() - new Vector2(16, 16), new Vector2(32, 32));

            GUI.Label(rect, devicePointer);

            rect.x += 32;
            rect.width = 128;
            GUI.Label(rect, "<i>" + Localization.Format(tobj.TypeLabel) + "</i>");

            rect.y += 16;
            rect.x += 16;
            rect.width = 256;
            rect.height = 20;
            GUI.color = Color.white;
            if (editable != tobj) GUI.Label(rect, "<b>" + deviceLabel + "</b>");
            else editableName = GUI.TextField(rect, editableName, 32);

            rect.x -= 16;
            rect.width = 16;
            rect.height = 16;
            GUI.color = (editable == tobj) ? editableColor : deviceColor;
            GUI.Label(rect, dot);

            GUI.color = Color.white;
            rect.y += 20;

            if (editable == tobj)
            {
                rect.width = 18 * (colors.Length) + 4;
                rect.height = 20;
                GUI.Box(rect, "");

                for (int i = 0; i < colors.Length; i++)
                {
                    GUI.color = colors[i];
                    if (GUI.Button(new Rect(rect.position + new Vector2(2 + i * 18, 2), Vector2.one * 16), dot))
                    {
                        editableColor = colors[i];
                    }
                }

                rect.y += 20;
            }

            GUI.color = Color.white;
            rect.width = rect.height = 24;

            if (editable != tobj && GUI.Button(rect, "+"))
            {
                editable = tobj;
                editableColor = deviceColor;
                editableName = deviceLabel;
            }

            if (editable == tobj)
            {
                if (GUI.Button(rect, Localization.Format("$ui:checkmark")))
                {
                    UserConfig.SetDeviceLabel(tobj.id, editableName, editableColor);
                    UserConfig.SaveUserConfig();
                    editable = null;
                    tobj.UpdateColor();
                }

                rect.x += 26;
                if (GUI.Button(rect, Localization.Format("$ui:crossmark")))
                {
                    editable = null;
                }
            }
        }
    }
}