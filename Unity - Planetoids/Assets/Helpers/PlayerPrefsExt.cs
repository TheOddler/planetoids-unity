using System;
using UnityEngine;
using System.Collections;
using System.Globalization;

public static class PlayerPrefsExt {
	
	private const string PASS_PHRASE = "Drie maal kloppen, drummen op je buik en dan knipogen naar de deur.";
	private const string ECRYPTED_PREFIX = "Verstopte waarde: ";
	
	
	static public void SetFloatEncrypted(string key, float value) {
		PlayerPrefsExt.SetStringEncrypted(key, value.ToString(CultureInfo.InvariantCulture));
	}
	static public float GetFloatEncrypted(string key, float defaultValue = 0) {
		try {
			string stringRep = PlayerPrefsExt.GetStringEncrypted(key, "NoValue");
			return Convert.ToSingle(stringRep);
		}
		catch {
			return defaultValue;
		}
	}
	
	static public void SetLong(string key, long value) {
		PlayerPrefs.SetString(key, value.ToString(CultureInfo.InvariantCulture));
	}
	static public long GetLong(string key, long defaultValue = 0) {
		try {
			string stringRep = PlayerPrefs.GetString(key, "NoValue");
			return Convert.ToInt64(stringRep);
		}
		catch {
			return defaultValue;
		}
	}
	
	static public void SetLongEncrypted(string key, long value) {
		PlayerPrefsExt.SetStringEncrypted(key, value.ToString(CultureInfo.InvariantCulture));
	}
	static public long GetLongEncrypted(string key, long defaultValue = 0) {
		try {
			string stringRep = PlayerPrefsExt.GetStringEncrypted(key, "NoValue");
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
	
	static public void SetTimeSpanEncrypted(string key, TimeSpan value) {
		PlayerPrefsExt.SetStringEncrypted(key, value.Ticks.ToString(CultureInfo.InvariantCulture));
	}
	static public TimeSpan GetTimeSpanEncrypted(string key, TimeSpan defaultValue) {
		try {
			string stringRep = PlayerPrefsExt.GetStringEncrypted(key, "NoValue");
			return new TimeSpan( Convert.ToInt64(stringRep) );
		}
		catch {
			return defaultValue;
		}
	}
	
	
	
	static public void SetStringEncrypted(string key, string value = "") {
		PlayerPrefs.SetString(key, Encrypter.Encrypt(ECRYPTED_PREFIX + value, PASS_PHRASE));
	}
	static public string GetStringEncrypted(string key, string defaultValue) {
		try {
			string encryptedString =Encrypter.Decrypt(PlayerPrefs.GetString(key,""), PASS_PHRASE);
			if (encryptedString.StartsWith(ECRYPTED_PREFIX)) {
				return encryptedString.Remove(0, ECRYPTED_PREFIX.Length);
			}
			else {
				return defaultValue;
			}
		}
		catch {
			return defaultValue;
		}
	}
}
