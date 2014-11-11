using UnityEngine;
using System.Collections;

using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Globalization;

using GooglePlayGames;
using UnityEngine.SocialPlatforms;
using GooglePlayGames.BasicApi;

// Workaround for the fact that we can't check the current score yet with the
// unity plugin for google play.
// We can however use a save-state, so we save the records in there, and load
// them at the start of the app.

[System.Serializable]
public class HighscoreInfo {
	public const string TIME_TRIAL_ID = "TimeTrialRecord";
	public long TimeTrialRecord = 0;
	
	public const string AREA_ATTACK_ID = "AreaAttackRecord";
	public long AreaAttackRecord = 0;
}

public class HighscoreManager : MonoBehaviour, OnStateLoadedListener {
	
	public GameManager _gameManager;
	public GameModeTime _gameModeTime;
	public GameModeArea _gameModeArea;
	
	public int _slot = 0;
	HighscoreInfo _highscoresInfo = new HighscoreInfo();
	
	void Start() {
		_gameManager.GameModeEnded.SetSubscription(true, UpdateStateAndSave);
		_gameManager.SetupFinished.SetSubscription(true, Load);
		_gameManager.GooglePlayAuthenticated.SetSubscription(true, Load);
	}
	
	void OnSuccesfullyLoaded () {
		_gameModeTime.UpdateRecord(_highscoresInfo.TimeTrialRecord);
		_gameModeArea.UpdateRecord(_highscoresInfo.AreaAttackRecord);
	}
	
	public void UpdateStateAndSave() {
		_highscoresInfo.TimeTrialRecord = _gameModeTime.CurrentRecord;
		_highscoresInfo.AreaAttackRecord = _gameModeArea.CurrentRecord;
		Save();
	}
	public void Save() {
		if (Social.localUser.authenticated) {		
			try {
				MemoryStream stream = new MemoryStream();
				var serializer = new BinaryFormatter();
				serializer.Serialize(stream, _highscoresInfo);
				
				((PlayGamesPlatform) Social.Active).UpdateState(_slot, stream.ToArray(), this);
			}
			catch (System.Exception exception) {
				Debug.Log("Save Failed: " + exception.Message);
			}
		}
		
		PlayerPrefs.SetString(HighscoreInfo.TIME_TRIAL_ID, _highscoresInfo.TimeTrialRecord.ToString(CultureInfo.InvariantCulture));
		PlayerPrefs.SetString(HighscoreInfo.AREA_ATTACK_ID, _highscoresInfo.AreaAttackRecord.ToString(CultureInfo.InvariantCulture));
	}
	public void OnStateSaved(bool success, int slot) {
		
	}
	
	public void Load() {
		if (Social.localUser.authenticated) {
			((PlayGamesPlatform) Social.Active).LoadState(_slot, this);
		}
		else {
			try {
				_highscoresInfo.TimeTrialRecord = System.Convert.ToInt64(PlayerPrefs.GetString(HighscoreInfo.TIME_TRIAL_ID, "0"));
				_highscoresInfo.AreaAttackRecord = System.Convert.ToInt64(PlayerPrefs.GetString(HighscoreInfo.AREA_ATTACK_ID, "0"));
				
				OnSuccesfullyLoaded();
			}
			catch (System.Exception exception) {
				Debug.Log("Load Failed From Player Pref: " + exception.Message);
			}
		}
	}
	public void OnStateLoaded(bool success, int slot, byte[] data) {
		if (success) {
			try {
				Stream stream = new MemoryStream(data);
				var serializer = new BinaryFormatter();
				_highscoresInfo = serializer.Deserialize(stream) as HighscoreInfo;
				
				OnSuccesfullyLoaded();
			}
			catch (System.Exception exception) {
				Debug.Log("Load Failed: " + exception.Message);
			}
		}
	}
	
	public byte[] OnStateConflict(int slot, byte[] local, byte[] server) {
		var serializer = new BinaryFormatter();
		
		Stream localStream = new MemoryStream(local);
		var localState = serializer.Deserialize(localStream) as HighscoreInfo;
		
		Stream serverStream = new MemoryStream(server);
		var serverState = serializer.Deserialize(serverStream) as HighscoreInfo;
		
		HighscoreInfo combined = new HighscoreInfo();
		combined.TimeTrialRecord = Helpers.Max(localState.TimeTrialRecord, serverState.TimeTrialRecord);
		combined.AreaAttackRecord = Helpers.Max(localState.AreaAttackRecord, serverState.AreaAttackRecord);
		
		MemoryStream combinesStream = new MemoryStream();
		serializer.Serialize(combinesStream, combined);
		
		return combinesStream.ToArray();
	}
}
