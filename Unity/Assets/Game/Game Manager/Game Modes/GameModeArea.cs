using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class GameModeArea : AGameMode {
	
	public const string AREA_ATTACK_LEADERBOARD_ID = "CgkIgr_5uO8aEAIQAQ";
	public const string AREA_ATTACK_RECORD_PLAYERPREF_ID = "AreaAttackRecord";
	public const string AREA_FORMAT = @"0.000 m²";
	
	public GameManager _manager;
	public float _minWantedArea = 10;
	
	float _totalAddedArea;
	float _totalDestroyedArea;
	float _lastInGameArea;
	
	float _curRecord;
	public float CurrentRecord { get { return _curRecord; } }
	
	public List<string> _finishMessages;
	public List<string> _recordMessages;
	public Text _recordText; 
	
	void Awake () {
		gameObject.SetActive(false);
		
		_manager.SetupFinished.SetSubscription(true, LoadKnownRecord);
		
		_manager.GooglePlayAuthenticated.SetSubscription(true, SaveRecord);
		_manager.GooglePlayAuthenticated.SetSubscription(true, LoadGooglePlayRecord);
	}
	
	override public bool Running { get { return gameObject.activeSelf; } }
	
	override public void StartGame() {
		gameObject.SetActive(true);
		
		_manager.Spaceship.Reset();
		
		_totalAddedArea = 0;
		while (_totalAddedArea < _minWantedArea) {
			_totalAddedArea += _manager.PlanetoidsManager.CreatePlanetoid().GetArea();
		}
		_totalDestroyedArea = 0;
		_lastInGameArea = _totalAddedArea;
		
		UpdateProgressInfo();
		
		SetSubscribtionToEvents(true);
	}
	override public void EndGame() {
		gameObject.SetActive(false);
		
		SetSubscribtionToEvents(false);
	}
	override public void CleanUpGame() {
		_manager.PlanetoidsManager.CashAllPlanetoids();
		_totalDestroyedArea = 0;
		_totalAddedArea = 0;
	}
	
	void SetSubscribtionToEvents(bool sub) {
		_manager.PlanetoidsManager.PlanetoidLeftPlay.SetSubscription(sub, OnPlanetoidsLeftPlay);
		_manager.PlanetoidsManager.PlanetoidEnteredPlay.SetSubscription(sub, OnPlanetoidsEnteredPlay);
		_manager.Spaceship.Died.SetSubscription(sub, OnGameFinished); //play until you die
	}
	
	
	
	void OnGameFinished() {
		if (ReportArea(_totalDestroyedArea)) {
			//New record!
			_manager.WinLooseMessage.text =  _recordMessages.GetRandom() + "\n" + _totalDestroyedArea.ToString(AREA_FORMAT);
			_manager.ProgressText.text = "A new record! Try to beat it again?";
		}
		else {
			_manager.WinLooseMessage.text =  _finishMessages.GetRandom() + "\n" + _totalDestroyedArea.ToString(AREA_FORMAT);
			_manager.ProgressText.text = "But no new record, try again?";
		}
		
		_manager.StopGameMode();
	}
	
	// returns true if it's a record
	public bool ReportArea(float area) {
		if (area > _curRecord) {
			_curRecord = area;
			UpdateRecordText();
			SaveRecord();
			return true;
		}
		else {
			return false;
		}
	}
	void UpdateRecordText() {
		_recordText.text = "Record:\n" + _curRecord.ToString(AREA_FORMAT);
	}
	void SaveRecord() {
		// In playerprefs
		PlayerPrefs.SetFloat(AREA_ATTACK_RECORD_PLAYERPREF_ID, _curRecord);
		// In google play services if active
		if (Social.localUser.authenticated) {
			long threeDecimals = FloatToLongThreeDecimal(_curRecord);
			Social.ReportScore(threeDecimals, AREA_ATTACK_LEADERBOARD_ID, (bool success) => {});
		}
	}
	void LoadKnownRecord() {
		ReportArea(PlayerPrefs.GetFloat(AREA_ATTACK_RECORD_PLAYERPREF_ID, 0.0f));
	}
	void LoadGooglePlayRecord() {
		// TODO Add GooglePlayServices stuff
	}
	static public long FloatToLongThreeDecimal(float area) {
		return (long)(area * 1000.0f + 0.5f);
	}
	static public string ThreeDecimalAreaToString(long area) {
		return (area / 1000).ToString() + "." + (area % 1000).ToString();
	}
	
	
	
	
	void Update () {
		_manager.ProgressText.text = _totalDestroyedArea.ToString("0.000");
	}
	
	void OnPlanetoidsLeftPlay() {
		UpdateProgressInfo();
		float currentInGameArea = CalculateAreaInPlay();
		_totalDestroyedArea += _lastInGameArea - currentInGameArea;
		
		if (currentInGameArea < _minWantedArea) {
			_totalAddedArea += _manager.PlanetoidsManager.CreatePlanetoid().GetArea();
		}
		
		_lastInGameArea = CalculateAreaInPlay();
	}
	void OnPlanetoidsEnteredPlay() {
		UpdateProgressInfo();
	}
	
	void UpdateProgressInfo() {
		float percentage = Mathf.Clamp01(_totalDestroyedArea / _totalAddedArea);
		_manager.ProgressBar.SetProgress(percentage);
	}
	
	float CalculateAreaInPlay() {
		float totalArea = 0;
		foreach (var planetoid in _manager.PlanetoidsManager.PlanetoidsInPlay) {
			totalArea += planetoid.GetArea();
		}
		return totalArea;
	}
	
}
