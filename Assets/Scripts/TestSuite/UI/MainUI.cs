using UnityEngine;
using System;
using Utils;
using System.Collections.Generic;
using StudyStore;

namespace TestSuite.UI
{
	/// <summary>
	/// The MainUI handles rendering of the different UI screen or views of the application. Views are organised in a stack: 
	/// only the view currently at the top of the stack is rendered. Opening/closing a view pushes/pops that view on/off the stack.
	/// Provides utility functions usable by the views (e.g. popup windows, notifications)
	/// </summary>
	public class MainUI : MonoBehaviour
	{
		public readonly static Color DEV_COLOR = new Color(1, .5f, 0);

		public GUISkin skin;
		public Texture2D backgroundImage;

		public List<View> viewstack = new List<View>();

		public View ActiveView => viewstack.Count > 0 ? viewstack[viewstack.Count - 1] : null;
		public View PreviousView { get; private set; }

		[Header("Top Menu")]
		public Texture2D backIcon;
		public Texture2D exitIcon;
		public Texture2D dropdownMenuArrow;

		[Header("Cameras")]
		public SwitchableCamera[] cameras;
		public int selectedCamera;

		// Dev Console
		public DevConsole Console { get; private set; }
		public bool showConsole;
		public float showConsolePresses = 0f;

		private Action<Vector2> modalWindowRenderer, loginWindowRenderer;
		private Action modalConfirmAction, modalCancelAction;

		private List<Notification> notifications = new List<Notification>();

		public bool HasActiveModalWindow => modalWindowRenderer != null || loginWindowRenderer != null;

		public Rect ModalWindowRect { get; private set; }

		/// <summary>
		/// Contents of the email field of the login pop-up
		/// </summary>
		private string email = "";

		public static readonly Vector2 ButtonSize = new Vector2(125, 30);

		/// <summary>
		/// Displays a button that can be in an enabled or disabled state
		/// </summary>
		/// <param name="rect">A rectangle representing the button position and size</param>
		/// <param name="label">The button's label</param>
		/// <param name="enabled">True iff the button should be clickable</param>
		/// <returns>True if the button has been clicked this frame</returns>
		public static bool Button(Rect rect, string label, bool enabled = false)
		{
			if (enabled) return GUI.Button(rect, label);
			else
			{
				GUI.Box(rect, label);
				return false;
			}
		}

		/// <summary>
		/// Displays a toggle that's colored when its value is true
		/// </summary>
		/// <param name="rect">A rectangle representing the button position and size</param>
		/// <param name="value">The previous value of the toggle</param>
		/// <param name="label">The button's label</param> 
		/// <returns>The current value of the toggle</returns>
		public static bool ColoredToggle(Rect rect, bool value, string label)
		{
			GUI.color = value ? Color.cyan : Color.white;
			value = GUI.Toggle(rect, value, label);
			GUI.color = Color.white;
			return value;
		}

		/// <summary>
		/// Centers a rectangle on the screen
		/// </summary>
		/// <param name="screen">The screen dimensions</param>
		/// <param name="dimensions">The rectangle dimensions</param>
		/// <returns>A rectangle of dimensions <paramref name="dimensions"/> centered on the screen</returns>
		public static Rect CenteredRect(Vector2 screen, Vector2 dimensions)
		{
			return new Rect((screen - dimensions) / 2, dimensions);
		}

		/// <summary>
		/// Centers a rectangle on the screen
		/// </summary>
		/// <param name="screen">The screen dimensions</param>
		/// <param name="width">The rectangle width</param>
		/// <param name="height">The rectangle height</param>
		/// <returns>A rectangle of dimensions <paramref name="dimensions"/> centered on the screen</returns>
		public static Rect CenteredRect(Vector2 screen, int width, int height)
		{
			return CenteredRect(screen, new Vector2(width, height));
		}

		/// <summary>
		/// Return a color, switching between <paramref name="color"/> and white over time
		/// </summary>
		/// <param name="color">The main color</param>
		/// <param name="freq">The blinking frequency</param>
		/// <returns><paramref name="color"/> or the color white, depending on the time and blink frequency</returns>
		public static Color BlinkColor(Color color, float freq = 1)
		{
			if ((((int)(Time.time / freq)) & 1) == 1) return color;
			else return Color.white;
		}

		/// <summary>
		/// Draws a semi-transparent box
		/// </summary>
		/// <param name="rect">A rectangle representing the box position and size</param>
		public static void LightBox(Rect rect)
		{
			GUI.color = GUI.color / 3f;
			GUI.Box(rect, "");
			GUI.color = GUI.color * 3f;
		}

		[Obsolete]
		public static void InputBox(Rect rect)
		{
			GUI.Box(rect, "", "textarea");
		}

		public bool SelectFromDropdown<T>(Rect rect, ref T selected, Func<int, T> valueAt, int length, ref bool open, ref Vector2 scroll, Func<T, string> toString = null, Func<T, string> toolTip = null)
		{
			if (toString == null) toString = t => t.ToString();
			string selectedLabel = Localization.Format(toString(selected));

			bool buttonPressed = GUI.Button(rect, selectedLabel);
			var h = Mathf.Min(rect.height, 32);
			GUI.Label(new Rect(rect.x + rect.width - h, rect.y, h, h), this.dropdownMenuArrow);

			if (buttonPressed)
			{
				open = !open;
				scroll = Vector2.zero;
			}

			if (open)
			{
				Rect dropdownRect = new Rect(rect.x, rect.y + rect.height, rect.width, Mathf.Clamp(length, 1, 10) * 22);
				GUI.Box(dropdownRect, "");
				return SelectFromList(ref selected, valueAt, length, dropdownRect, ref scroll, toString, toolTip);
			}

			return false;
		}

		/// <summary>
		/// Displays a scrollable list from which items can be selected using checkboxes
		/// </summary>
		/// <typeparam name="T">The type of objects from the list</typeparam>
		/// <param name="selected">The selected item</param>
		/// <param name="valueAt">An accessor function for list items, returning the object at each position of the list</param>
		/// <param name="length">The length of the list</param>
		/// <param name="rect"></param>
		/// <param name="scroll"></param>
		/// <param name="toString"></param>
		/// <param name="toolTip"></param>
		/// <returns>True if the selected value changed</returns>
		public static bool SelectFromList<T>(ref T selected, Func<int, T> valueAt, int length, Rect rect, ref Vector2 scroll, Func<T, string> toString = null, Func<T, string> toolTip = null)
		{
			bool changed = false;

			if (toString == null) toString = t => t.ToString();

			Rect viewRect = new Rect(0, 0, rect.width - 20, Mathf.Max(rect.height, length * 22));

			GUI.color = Color.white;

			LightBox(rect);
			scroll = GUI.BeginScrollView(rect, scroll, viewRect);
			{
				GUILayout.BeginArea(viewRect);
				{
					GUILayout.BeginVertical();
					{
						for (int i = 0; i < length; i++)
						{
							T value = valueAt(i);
							bool isSelected = selected == null ? (value == null) : selected.Equals(value);
							GUI.color = (isSelected as bool? == true) ? Color.cyan * 2 : Color.white;

							string label = Localization.LocalizeDefault(toString(value));
							string tooltip = toolTip?.Invoke(value);

							if (GUILayout.Toggle(isSelected, new GUIContent(label, tooltip)))
							{
								changed = !isSelected;
								selected = value;
							}
						}
					}
					GUILayout.EndVertical();
				}
				GUILayout.EndArea();
			}
			GUI.EndScrollView();

			GUI.color = Color.white;

			return changed;
		}

		public void SetConsoleEnabled(bool enabled)
		{
			this.showConsole = enabled;
		}

		public void Start()
		{
			for (int i = 0; i < this.cameras.Length; i++)
			{
				if (i != selectedCamera) this.cameras[i].gameObject.SetActive(this.cameras[i].alwaysOn);
			}

			if (ActiveView)
			{
				ActiveView.MainUI = this;
				ActiveView.OnMadeVisible();
			}

			this.Console = gameObject.AddComponent<DevConsole>();

			this.email = UserConfig.RememberedLogin;
		}

		public void Update()
		{
			if (Input.GetKeyDown(KeyCode.F10))
			{
				showConsole = !showConsole;
				showConsolePresses += 1;
			}
			else
			{
				if (showConsolePresses < 3)
				{
					showConsolePresses -= Time.deltaTime * 3;
					if (showConsolePresses < 0) showConsolePresses = 0;
				}
			}
			if (Input.GetKeyDown(KeyCode.Escape)) showConsole = false;

			if (Input.GetKeyDown(KeyCode.F5)) Localization.InitDefault(force: true);
		}


		/// <summary>
		/// Push a view to the top of the view stack
		/// </summary>
		/// <param name="view">The view to open</param>
		public void PushView(View view)
		{
			if (this.ActiveView)
			{
				this.ActiveView.OnClosed();
				PreviousView = this.ActiveView;
			}

			this.viewstack.Add(view);
			view.MainUI = this;
			view.OnMadeVisible();
		}

		/// <summary>
		/// Replace the view at the top of the view stack.
		/// Does not trigger the OnMadeVisible and OnClosed events of the view below the top of the stack.
		/// If <c>nextView</c> is the view under <c>currentView</c> in the stack, closes <c>currentView</c> instead.
		/// </summary>
		/// <param name="currentView">The current view</param>
		/// <param name="nextView">The view to open</param>
		public void SwapView(View currentView, View nextView)
		{
			if (this.ActiveView == currentView)
			{
				if (viewstack.Count >= 2 && viewstack[viewstack.Count - 2] == nextView)
				{
					CloseView(currentView);
					return;
				}

				viewstack[viewstack.Count - 1] = nextView;
				currentView.OnClosed();
				PreviousView = currentView;
				nextView.MainUI = this;
				nextView.OnMadeVisible();
			}
		}

		/// <summary>
		/// Close a view if it is currently active
		/// </summary>
		/// <param name="view">The view to close</param>
		public void CloseView(View view)
		{
			if (this.ActiveView == view)
			{
				viewstack.RemoveAt(viewstack.Count - 1);
				view.OnClosed();
				PreviousView = view;

				if (ActiveView)
				{
					ActiveView.MainUI = this;
					ActiveView.OnMadeVisible();
				}
			}
		}

		/// <summary>
		/// Closes views from the top of the view stack until reaching the argument view
		/// </summary>
		/// <param name="view">The view to reach</param>
		public void ReturnToView(View view)
		{
			int index = viewstack.LastIndexOf(view);

			if (index >= 0)
			{
				while (viewstack.Count > index + 1) viewstack.RemoveAt(viewstack.Count - 1);
				ActiveView?.OnMadeVisible();
			}
		}

		public void BeginFullscreen()
		{
			GUI.EndGroup();
		}

		public void EndFullScreen()
		{
			Vector2 screen = new Vector2(Screen.width, Screen.height);
			GUI.BeginGroup(new Rect(0, topMenuHeight, screen.x, screen.y));
		}

		const int topMenuHeight = 24;

		/// <summary>
		/// Called every frame by Unity to render the UI
		/// </summary>
		public void OnGUI()
		{
			if (this.skin) GUI.skin = this.skin;

			Vector2 screen = new Vector2(Screen.width, Screen.height);

			Vector2 wSize = new Vector2(400, 250);
			ModalWindowRect = new Rect(screen / 2 - wSize / 2, wSize);


			if (ActiveView?.ShowBackgroundScreen ?? false) GUI.DrawTexture(new Rect(Vector2.zero, screen), backgroundImage);

			OnGUINotifications(screen);

			screen.y -= topMenuHeight;
			GUI.BeginGroup(new Rect(0, topMenuHeight, screen.x, screen.y));

			if (!(ActiveView?.HidesCameraView ?? false)) OnGUICameraSelect(screen);

			if (ActiveView)
			{
				ActiveView.MainUI = this;
				ActiveView.OnViewGUI(screen);
			}

			GUI.EndGroup();
			screen.y += topMenuHeight;

			OnGUITopMenu(new Vector2(screen.x, topMenuHeight));

			OnGUIModalWindow(screen);

			OnGUINotifications(screen);

			if (showConsole && (UserConfig.DevMode || showConsolePresses > 3)) Console.DrawConsole(this, new Vector2(screen.x * .85f, screen.y * .65f));

			if (UserConfig.DevMode)
			{
				GUI.color = DEV_COLOR;
				GUI.Label(new Rect(0, screen.y - 20, 256, 20), Localization.Format("$buildID") + "#Developer");
			}
			else
			{
				GUI.color = new Color(.5f, .5f, .5f, .5f);
				GUI.Label(new Rect(0, screen.y - 20, 256, 20), Localization.Format("$buildID"));
			}
		}

		public void OnGUITopMenu(Vector2 dimensions)
		{
			GUI.color = Color.white;
			if (Screen.fullScreen) GUI.Box(new Rect(-10, 0, dimensions.x + 20, dimensions.y), Localization.Format("<b>$appname</b>"));

			if (this.viewstack.Count >= 2 && this.ActiveView.BackButtonEnabled)
			{
				View prevView = this.viewstack[viewstack.Count - 2];
				string label = Localization.Format(" $ui:backTo:" + prevView.GetType().Name);

				if (GUI.Button(new Rect(0, 0, 180, dimensions.y), new GUIContent(label, backIcon, label)))
				{
					this.CloseView(this.ActiveView);
				}
			}

			GUI.color = Color.red;

			if (this.ActiveView && this.ActiveView.AllowsApplicationExit && Screen.fullScreen)
			{
				bool quit = GUI.Button(new Rect(dimensions.x - dimensions.y * 2 - 2, 0, dimensions.y * 2, dimensions.y), "");
				GUI.color = Color.white;
				GUI.Label(new Rect(dimensions.x - dimensions.y * 2 + 14, 0, dimensions.y * 2, dimensions.y), exitIcon);

				if (quit)
				{
#if UNITY_EDITOR
					UnityEditor.EditorApplication.isPlaying = false;
#endif
					Application.Quit();
				}
			}

			GUI.color = Color.white;
		}

		public void OnGUINotifications(Vector2 screen)
		{
			for (int i = 0; i < notifications.Count; i++)
			{
				var notif = notifications[i];

				if (i + 1 < notifications.Count) notif.Close();

				if (!notif.OnGUI(this, screen, i))
				{
					notifications.RemoveAt(i);
					i--;
				}
			}

			GUI.color = Color.white;
		}

		private void OnGUICameraSelect(Vector2 screen)
		{
			Vector2 toggleSize = new Vector2(125, 25);

			GUI.Box(new Rect(screen.x - 140, 5, 130, 25 * cameras.Length + 10), "");

			for (int i = 0; i < cameras.Length; i++)
			{
				var camera = cameras[i];
				string label = Localization.LocalizeDefault(camera.label);

				if (GUI.Toggle(new Rect(new Vector2(screen.x - 135, i * 25 + 10), toggleSize), this.selectedCamera == i, label))
				{
					SelectCamera(i);
				}
			}
		}

		/// <summary>
		/// Sets a camera outside of the camera list to be active, disabling the currently selected camera (unless it is set as always on)
		/// <param name="extCamera">The camera to set active over the currently selected camera</param>
		/// </summary>
		public void SelectExternalCamera(Camera extCamera)
		{
			if (selectedCamera >= 0 && selectedCamera < this.cameras.Length)
			{
				var oldcam = this.cameras[this.selectedCamera];
				oldcam.gameObject.SetActive(oldcam.alwaysOn);
			}

			extCamera.gameObject.SetActive(true);
		}

		/// <summary>
		/// Disable a camera set using SelectExternalCamera, and re-enables the last used camera
		/// <param name="extCamera">The camera to disable</param>
		/// </summary>
		public void DisableExternalCamera(Camera extCamera)
		{
			extCamera.gameObject.SetActive(false);
			this.SelectCamera(this.selectedCamera);
		}

		public void SelectCamera(int i)
		{
			if (i >= this.cameras.Length) return;

			var oldcam = this.cameras[this.selectedCamera];
			oldcam.gameObject.SetActive(oldcam.alwaysOn);

			var newcam = this.cameras[i];
			newcam.gameObject.SetActive(true);

			this.selectedCamera = i;
		}

		public void OnGUIModalWindow(Vector2 screen)
		{
			if (modalWindowRenderer == null) loginWindowRenderer?.Invoke(screen);
			modalWindowRenderer?.Invoke(screen);
		}

		private void ModalWindow_Confirm()
		{
			if (this.modalWindowRenderer != null)
			{
				this.modalConfirmAction?.Invoke();

				this.modalWindowRenderer = null;
				this.modalConfirmAction = null;
				this.modalCancelAction = null;
			}
		}

		private void ModalWindow_Cancel()
		{
			if (this.modalWindowRenderer != null)
			{
				this.modalCancelAction?.Invoke();

				this.modalWindowRenderer = null;
				this.modalConfirmAction = null;
				this.modalCancelAction = null;
			}
		}

		private void OnLoginExpired()
		{
			LoginPrompt(new APIError() { message = "$ui:loginExpired" }, overrideExistingPrompt: true);
		}

		public void LoginPrompt(APIError? error = null, bool overrideExistingPrompt = false, string defaultEmail = null, string defaultPassword = null, bool forceConfirm = false)
		{
			if (this.loginWindowRenderer != null && !overrideExistingPrompt) return;

			string header = "<b>" + Localization.LocalizeDefault("$ui:loginHeader") + "</b>";

			this.email = defaultEmail ?? this.email ?? "";
			string password = defaultPassword ?? "";

			bool waitingForResponse = false;

			string loginButton = Localization.Format("$ui:loginButton");
			bool rememberLogin = this.email == UserConfig.RememberedLogin;

#if UNITY_EDITOR
			if (error != null) Debug.LogError(error?.FullError);
#endif

			this.loginWindowRenderer = (screen) =>
			{
				GUI.Window(1, ModalWindowRect, id =>
				{
					GUI.color = Color.white;

					if (error == null) GUI.Label(new Rect(50, 35, 300, 75), Localization.Format("$ui:loginText"));
					else GUI.Label(new Rect(50, 35, 300, 75), Localization.Format("<color=red>" + error?.MessageToDisplay + "</color>"));

					GUI.BeginGroup(new Rect(50, 100, 300, 105));
					{
						Rect labelRect = new Rect(0, 0, 100, 22);
						GUI.Label(labelRect, Localization.Format("$ui:loginID"));

						Rect fieldRect = new Rect(100, 0, 200, 22);
						if (!waitingForResponse) this.email = GUI.TextField(fieldRect, this.email);
						else GUI.Label(fieldRect, this.email, "box");

						labelRect.y += 35;
						GUI.Label(labelRect, Localization.Format("$ui:loginPassword"));

						fieldRect.y += 35;
						if (!waitingForResponse) password = GUI.PasswordField(fieldRect, password, '*');
						else GUI.Label(fieldRect, "".PadLeft(password.Length, '*'), "box");

						fieldRect.y += 35;
						GUI.color = rememberLogin && !waitingForResponse ? Color.cyan : waitingForResponse ? Color.gray : Color.white;
						if (!waitingForResponse) rememberLogin = GUI.Toggle(fieldRect, rememberLogin, Localization.Format("$ui:rememberLogin"));
						else GUI.Toggle(fieldRect, rememberLogin, Localization.Format("$ui:rememberLogin"));
						GUI.color = Color.white;
					}
					GUI.EndGroup();

					Rect buttonRect = new Rect(ModalWindowRect.width / 2 - 75, ModalWindowRect.height - 40, 150, ButtonSize.y);

					if (!waitingForResponse)
					{
						if (GUI.Button(buttonRect, loginButton) || forceConfirm)
						{
							waitingForResponse = true;
							API.Instance.Login(this.email, password,
								u =>
								{
									waitingForResponse = false;
									API.Instance.onLoginExpired -= OnLoginExpired;
									API.Instance.onLoginExpired += OnLoginExpired;
									Notify(Localization.Format("$ui:loginWelcome::2", u.name, u.email));
									this.loginWindowRenderer = null;

									if (rememberLogin)
									{
										UserConfig.RememberedLogin = this.email;
										UserConfig.SaveUserConfig();
									}
								},
								err =>
								{
									waitingForResponse = false;
									this.loginWindowRenderer = null;
									LoginPrompt(err);
								});
						}
					}
					else
					{
						GUI.Box(buttonRect, loginButton);
					}

				}, header);
			};
		}

		/// <summary>
		/// Displays a notification
		/// </summary>
		/// <param name="message">The message content of the notification</param>
		/// <param name="severity">The severity of the notification (0: Info, 1: Warning, 2: Error)</param>
		public void Notify(string message, int severity = 0)
		{
			this.notifications.Add(new Notification(message, severity));
		}

		/// <summary>
		/// Return the color associated with a given notification severity level
		/// </summary>
		/// <param name="severity">The severity of the notification (0: Info, 1: Warning, 2: Error)</param>
		/// <returns>the color associated with the given notification severity level</returns>
		public Color SeverityColor(int severity)
		{
			if (severity == 1) return Color.yellow;
			if (severity > 1) return Color.red;
			return Color.cyan;
		}

		public void ConfirmAction(string header, string body, Action confirmAction, Action cancelAction = null, int severity = 0, string confirmText = "$ui:confirm", string cancelText = "$ui:cancel")
		{
			ModalWindow_Cancel();

			header = "<b>" + Localization.LocalizeDefault(header) + "</b>";
			body = Localization.LocalizeDefault(body);
			confirmText = Localization.LocalizeDefault(confirmText);
			cancelText = Localization.LocalizeDefault(cancelText);

			this.modalConfirmAction = confirmAction;
			this.modalCancelAction = cancelAction;

			this.modalWindowRenderer = (screen) =>
			{
				GUI.color = SeverityColor(severity);

				GUI.ModalWindow(0, ModalWindowRect, id =>
				{
					GUI.color = severity > 0 ? Color.yellow : new Color(.5f, .5f, 1f, 1f);

					GUI.Label(new Rect(50, 50, 50, 50), severity > 0 ? "<b>!</b>" : "<b>?</b>");

					GUI.color = Color.white;

					GUI.Label(new Rect(100, 50, 250, 140), body);

					if (GUI.Button(new Rect(ModalWindowRect.width - 150, ModalWindowRect.height - 40, ButtonSize.x, ButtonSize.y), cancelText))
					{
						ModalWindow_Cancel();
					}

					if (GUI.Button(new Rect(ModalWindowRect.width - 300, ModalWindowRect.height - 40, ButtonSize.x, ButtonSize.y), confirmText))
					{
						ModalWindow_Confirm();
					}

				}, header); ;

				GUI.color = Color.white;
			};
		}

		/// <summary>
		/// Represents a notification pop-up
		/// </summary>
		private class Notification
		{
			string text;
			int severity = 0;
			float time = 0;
			float alpha = 0.01f;
			bool closed = false;

			public Notification(string message, int severity)
			{
				this.text = Localization.Format(message);
				this.severity = severity;
			}

			public void Close()
			{
				closed = true;
				alpha = 0.01f;
			}

			/// <summary>
			/// Render the notification and returns true if the notification should continue to be visible, false otherwise
			/// </summary>
			/// <param name="mainUI">The MainUI instance</param>
			/// <param name="screen">The screens dimensions</param>
			/// <param name="position">The position of the notification, when displaying several notifications</param>
			/// <returns>true if the notification should continue to be visible, false otherwise</returns>
			public bool OnGUI(MainUI mainUI, Vector2 screen, int position = 0)
			{
				if (!closed && alpha < 1) alpha = Mathf.Clamp01(alpha + Time.deltaTime * 2);
				if (time > 7.5 && severity < 1) closed = true;

				var color = mainUI.SeverityColor(severity);
				color.a = alpha;
				GUI.color = color;

				Rect rect = new Rect(screen.x / 2 - 200 + (15 * position), screen.y - 150 + (15 * position), 400, 75);

				GUI.Box(rect, "", "window");
				GUI.BeginGroup(rect);
				{
					GUI.color = new Color(1, 1, 1, alpha);
					GUI.Box(new Rect(10, 15, rect.width - 65, rect.height - 20), text, "label");

					if (!closed)
					{
						closed = GUI.Button(new Rect(rect.width - 60, 35, 50, 30), Localization.Format("$ui:ok"));
					}
				}
				GUI.EndGroup();

				time += Time.deltaTime;
				if (closed) alpha = Mathf.Clamp01(alpha - Time.deltaTime * 2);

				return !closed || alpha > 0;
			}
		}

	}
}