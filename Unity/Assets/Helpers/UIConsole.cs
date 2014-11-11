using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIConsole : MonoBehaviour {
	
	public Text _logText;
	
	void OnEnable() {
		Application.RegisterLogCallback(HandleLog);
	}
	void OnDisable() {
		Application.RegisterLogCallback(null);
	}
	
	void HandleLog(string logString, string stackTrace, LogType type) {
		_logText.text += "\n" + logString;
	}
}
