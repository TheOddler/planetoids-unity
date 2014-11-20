using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SettingsManager : MonoBehaviour {
	
	public Toggle _reverseFlightToggle;
	public const string REVERSE_FLIGHT_ID = "UseReverseFlight";
	public static bool UseReverseFlight = false;
	
	void Start () {
		UseReverseFlight = PlayerPrefs.GetInt(REVERSE_FLIGHT_ID, 0) == 1;
		_reverseFlightToggle.isOn = UseReverseFlight;
	}
	
	public void SetUseReverseFlight(bool value) {
		UseReverseFlight = value;
		PlayerPrefs.SetInt(REVERSE_FLIGHT_ID, value ? 1 : 0);
	}
	
}
