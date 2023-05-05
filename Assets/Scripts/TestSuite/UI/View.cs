using UnityEngine;
using UnityEngine.UI;
using System;
using FullBodyTracking;
using TestSuite.Metrology;
using Utils;
using System.Linq;
using System.Collections.Generic;

namespace TestSuite.UI
{
	/// <summary>
	/// An UI View. A view is meant to represent one screen of the application. The MainUI component of the application handles the rendering of views.
	/// </summary>
	public abstract class View : MonoBehaviour
	{
		/// <summary>
		/// If true, the MainUI won't display camera controls
		/// </summary>
		public virtual bool HidesCameraView => false;

        /// <summary>
        /// If true, the MainUI will display an exit button in the top right corner
        /// </summary>
        public virtual bool AllowsApplicationExit => false;

		/// <summary>
		/// If true, the MainUI will display a back button to return to the previous view in the viewstack
		/// </summary>
		public virtual bool BackButtonEnabled => true;

		/// <summary>
		/// If true, will show the MainUI's background image behind this UI while it is active
		/// </summary>
		public virtual bool ShowBackgroundScreen => false;

		/// <summary>
		/// Called every frame when the UI is being rendered
		/// </summary>
		/// <param name="screenDimensions">A vector whose dimensions represent the width and height of available screen space to render this UI</param>
		public abstract void OnViewGUI(Vector2 screenDimensions);

		/// <summary>
		/// Reference to the MainUI component
		/// </summary>
		public MainUI MainUI { get; internal set; }

		/// <summary>
		/// Called after this view is added to the viewstack, or becomes the top view of the stack after a view is closed
		/// </summary>
		public virtual void OnMadeVisible() { }

		/// <summary>
		/// Called after this view is removed from the viewstack
		/// </summary>
		public virtual void OnClosed() { }
	}
}