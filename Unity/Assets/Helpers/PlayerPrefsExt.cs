using System;
using UnityEngine;
using System.Collections;
using System.Globalization;

public static class PlayerPrefsExt {
	
	static public void SetLong(string key, long value) {
		PlayerPrefs.SetString(key, value.ToString(CultureInfo.InvariantCulture));
	}
	static public long GetString(string key, long defaultValue = 0) {
		try {
			string stringRep = PlayerPrefs.GetString(key, "NoValue");
			return Convert.ToInt64(stringRep);
		}
		catch {
			return defaultValue;
		}
	}
	
	static public void SetTimeSpan(string key, TimeSpan value) {
		PlayerPrefs.SetString(key, value.Ticks.ToString(CultureInfo.InvariantCulture));
	}
	static public TimeSpan GetTimeSpan(string key, TimeSpan defaultValue) {
		try {
			string stringRep = PlayerPrefs.GetString(key, "NoValue");
			return new TimeSpan( Convert.ToInt64(stringRep) );
		}
		catch {
			return defaultValue;
		}
	}
	
}
