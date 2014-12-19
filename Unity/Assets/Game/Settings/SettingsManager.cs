using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SettingsManager : MonoBehaviour {
	
	const int TRUE = 1;
	const int FALSE = 0;
	
	public Toggle _soundToggle;
	public const string SOUND_ENABLED_ID = "Sound";
	
	void Start () {
		_soundToggle.isOn = PlayerPrefs.GetInt(SOUND_ENABLED_ID, TRUE) == TRUE;
		SetSoundEnabled(_soundToggle.isOn);
	}
	
	public void SetSoundEnabled(bool enabled) {
		PlayerPrefs.SetInt(SOUND_ENABLED_ID, enabled ? TRUE : FALSE);
		AudioListener.volume = enabled ? 1 : 0;
		
		Debug.Log(enabled);
	}
	
}
