using Utils;
using System.Runtime.Serialization;
using UnityEngine;
using System;
using TestSuite;
using System.Collections.Generic;

[DataContract]
public class UserConfig
{
	[DataContract]
	private struct DeviceLabel
	{
		[DataMember(Order = 0)] internal string label;
		[DataMember(Order = 1)] float r;
		[DataMember(Order = 2)] float g;
		[DataMember(Order = 3)] float b;

		public Color Color
		{
			get => new Color(r, g, b);
			set
			{
				r = value.r;
				g = value.g;
				b = value.b;
			}
		}
	}

	public static Vector3 RoomOffsetTranslation { get => config.roomOffsetTranslation; set => config.roomOffsetTranslation = value; }
	public static Quaternion RoomOffsetRotation { get => config.roomOffsetRotation; set => config.roomOffsetRotation = value; }
	public static string RememberedLogin { get => config.rememberedLogin; set => config.rememberedLogin = value; }
	public static bool ShowDevicesLabelsDuringCalibration { get => config.showDevicesLabelsDuringCalibration; set => config.showDevicesLabelsDuringCalibration = value; }
	public static ulong AccessibilityDeviceID { get => config.accessibilityDeviceID; set => config.accessibilityDeviceID = value; }
	public static bool LockRoomOffset { get => config.lockRoomOffset; set => config.lockRoomOffset = value; }

	public static bool AllowConnectionToTestServer { get => config.allowConnectionToTestServer; }
	public static string TestServer { get => config.testServer; }
	public static string TestServerCert { get => config.testServerCert; }

	public static bool DevMode => config.devMode;
	public static bool ForceOfflineMode => config.forceOfflineMode;

	public static void GetDeviceLabel(ulong deviceId, out string label, out Color color)
	{
		DeviceLabel value;

		if (config.devices == null) config.devices = new Dictionary<string, DeviceLabel>();

		if (config.devices.TryGetValue(deviceId.ToString(), out value))
		{
			label = value.label;
			color = value.Color;
		}
		else
		{
			label = deviceId.ToString();
			if (label.Length > 3) label = label.Substring(label.Length - 3);
			color = Color.white;
		}
	}

	public static void SetDeviceLabel(ulong deviceId, string label, Color color)
	{
		if (config.devices == null) config.devices = new Dictionary<string, DeviceLabel>();
		DeviceLabel value = new DeviceLabel();
		value.label = label;
		value.Color = color;
		config.devices[deviceId.ToString()] = value;
	}

	public static void SaveUserConfig()
	{
		try
		{
			JSONSerializer.MkDirParent(Config.OutputDirectory + "/userconfig.json");
			JSONSerializer.ToJSONFile(Config.OutputDirectory + "/userconfig.json", config);
			Debug.Log("Wrote User Config");
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
	}

	// static members  
	private static UserConfig config;

	[DataMember(Order = 1)] private Vector3 roomOffsetTranslation;
	[DataMember(Order = 2)] private Quaternion roomOffsetRotation;
	[DataMember(Order = 0)] private string rememberedLogin = null;
	[DataMember(Order = 3)] private bool showDevicesLabelsDuringCalibration;
	[DataMember(Order = 4)] private bool lockRoomOffset;
	[DataMember(Order = 20)] private Dictionary<string, DeviceLabel> devices;
	[DataMember(Order = 10)] private bool devMode;
	[DataMember(Order = 11)] private ulong accessibilityDeviceID;
	[DataMember(Order = 12)] private bool forceOfflineMode;
	[DataMember(Order = 30)] private bool allowConnectionToTestServer;
	[DataMember(Order = 31)] private string testServer;
	[DataMember(Order = 32)] private string testServerCert;

	static UserConfig()
	{
		try
		{
			config = JSONSerializer.FromJSONFile<UserConfig>(Config.OutputDirectory + "/userconfig.json");
			Debug.Log("Read User Config");
		}
		catch (Exception e)
		{
			config = new UserConfig();

			Debug.LogException(e);
		}
	}

	private UserConfig()
	{
		this.rememberedLogin = null;
		this.roomOffsetRotation = Quaternion.identity;
		this.roomOffsetTranslation = Vector3.zero;
		this.devices = new Dictionary<string, DeviceLabel>();
		this.devMode = false;
		this.showDevicesLabelsDuringCalibration = true;
		this.accessibilityDeviceID = 0;
		this.lockRoomOffset = false;
		this.forceOfflineMode = false;
	}
}