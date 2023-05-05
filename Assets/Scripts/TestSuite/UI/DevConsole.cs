using StudyStore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TestSuite.UI
{
	public class DevConsole : MonoBehaviour
	{
		private struct ConsoleLog
		{
			public string time;
			public LogType type;
			public string content, stackTrace;

			public string ToString(bool trace)
			{
				if (trace) return "> " + content + "\n" + stackTrace;
				else return "> " + content;
			}
		}

		private List<ConsoleLog> messages = new List<ConsoleLog>();
		private Vector2 scroll;
		private bool changed = false, scrollToEnd = false;
		private float totalHeight = 0, currentWidth = 0;
		private int logs = 0, warnings = 0, errors = 0;
		private bool filterLog = true, filterWarn = true, filterError = true;
		private int expandedMessage = -1;

		private void UpdateHeight()
		{
			totalHeight = 0;

			for (int i = 0; i < messages.Count; i++)
			{
				var msg = messages[i];
				if (Filter(msg))
				{
					string text = msg.ToString(expandedMessage == i);
					totalHeight += GUI.skin.box.CalcHeight(new GUIContent(text), currentWidth - 120);
				}
			}

			if (scrollToEnd && scroll.y < totalHeight) scroll.y = totalHeight;

			changed = false;
			scrollToEnd = false;
		}

		private bool Filter(ConsoleLog message)
		{
			if (message.type == LogType.Log) return filterLog;
			if (message.type == LogType.Warning) return filterWarn;
			return filterError;
		}

		public void DrawConsole(MainUI mainUI, Vector2 dimensions)
		{
			if (changed || currentWidth != dimensions.x)
			{
				currentWidth = dimensions.x;
				UpdateHeight();
			}

			GUI.color = Color.white;
			GUI.Window(222, new Rect(Vector3.zero, dimensions), (id) =>
			{
				Rect rect = new Rect(0, 0, dimensions.x, dimensions.y);
				GUI.Box(rect, "");
				GUI.Box(rect, "");

				GUI.changed = false;
				GUI.color = Color.cyan;
				filterLog = GUI.Toggle(new Rect(0, 0, 50, 24), filterLog, " " + logs);
				GUI.color = Color.yellow;
				filterWarn = GUI.Toggle(new Rect(50, 0, 50, 24), filterWarn, " " + warnings);
				GUI.color = Color.red;
				filterError = GUI.Toggle(new Rect(100, 0, 50, 24), filterError, " " + errors);
				if (GUI.changed) changed = scrollToEnd = true;

				GUI.color = Color.white;
				if (GUI.Button(new Rect(150, 2, 85, 20), "Clear")) Clear();

				// exit console button
				GUI.color = Color.red;

				bool quit = GUI.Button(new Rect(dimensions.x - 48 - 2, 0, 48, 24), "");
				GUI.color = Color.white;
				GUI.Label(new Rect(dimensions.x - 48 + 14, 0, 48, 24), mainUI.exitIcon);

				if (quit) mainUI.SetConsoleEnabled(false);

				GUI.color = Color.white;

				scroll = GUI.BeginScrollView(new Rect(0, 24, dimensions.x, dimensions.y - 24), scroll, new Rect(0, 0, dimensions.x - 20, totalHeight));
				{
					float y = 0;

					for (int i = 0; i < messages.Count; i++)
					{
						var msg = messages[i];
						if (Filter(msg))
						{
							GUI.color = msg.type == LogType.Log ? Color.white : msg.type == LogType.Warning ? Color.yellow : Color.red;

							string text = msg.ToString(expandedMessage == i);
							float h = GUI.skin.box.CalcHeight(new GUIContent(text), dimensions.x - 120);

							GUI.changed = false;
							GUI.Toggle(new Rect(2, y, 92, 16), expandedMessage == i, msg.time);
							if (GUI.changed)
							{
								expandedMessage = expandedMessage == i ? -1 : i;
								changed = true;
							}

							GUI.Label(new Rect(94, y, dimensions.x - 120, h), text);

							y += h;
						}
					}
				}
				GUI.EndScrollView();
			}, "", "box");
		}

		public void Clear()
		{
			messages.Clear();

			changed = true;
			scrollToEnd = true;

			expandedMessage = -1;
			logs = 0;
			warnings = 0;
			errors = 0;

			Debug.Log("Console cleared");
		}

		private void Start()
		{
			Application.logMessageReceived += Log;
			Debug.Log("Console start");
		}

		/// <summary>
		/// Returns a stream containing the error log, if any error has been logged since the last time the console has been cleared, returns null otherwise. 
		/// Always return full log while in DevMode.
		/// </summary>
		/// <returns>a stream containing the error log, if any error has been logged since the last time the console has been cleared, null otherwise</returns>
		public Stream GetErrorLog()
		{
			if (errors > 0 || UserConfig.DevMode)
			{
				var stream = new MemoryStream();
				var writer = new StreamWriter(stream);

				foreach (var msg in messages)
				{
					writer.Write(msg.type + " @ " + msg.time + "\t: " + msg.content + "\n");
					if (msg.type != LogType.Log) writer.Write(msg.stackTrace + "\n");
				}

				writer.Flush();
				stream.Position = 0;
				return stream;
			}
			else
			{
				return null;
			}
		}

		private void Log(string content, string stackTrace, LogType type)
		{
			var log = new ConsoleLog()
			{
				time = DateTime.Now.ToLongTimeString(),
				type = type,
				content = content,
				stackTrace = stackTrace
			};


			if (log.type == LogType.Log) logs++;
			else if (log.type == LogType.Warning) warnings++;
			else errors++;

			messages.Add(log);
			changed = true;
			scrollToEnd = true;
		}
	}
}