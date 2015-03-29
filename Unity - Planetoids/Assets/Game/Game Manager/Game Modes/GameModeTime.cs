using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

public class GameModeTime : AGameMode {
	
	public const string TIME_TRIAL_LEADERBOARD_ID = "CgkIgr_5uO8aEAIQDw";
	public const string TIME_TRIAL_RECORD_PLAYERPREF_ID = "Record_TimeTrial";
	
	public const string TIME_FORMAT = @"mm\:ss.ff";
	
	public GameManager _manager;
	public int _planetoidCount = 10;
	
	float _areaAtStart;
	Stopwatch _stopwatch = new Stopwatch();
	
	TimeSpan _curRecord = TimeSpan.MaxValue;
	public TimeSpan CurrentRecord { get { return _curRecord; } }
	
	public List<string> _winMessages;
	public List<string> _recordMessages;
	public List<string> _looseMessages;
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
		_manager.PlanetoidsManager.CreatePlanetoids(_planetoidCount);
		
		_areaAtStart = CalculateAreaInPlay();
		_stopwatch.Start();
		
		UpdateProgressInfo();
		
		SetSubscribtionToEvents(true);
	}
	override public void EndGame() {
		gameObject.SetActive(false);
		_stopwatch.Stop();
		
		SetSubscribtionToEvents(false);
	}
	override public void CleanUpGame() {
		_manager.PlanetoidsManager.CashAllPlanetoids();
	}
	
	void SetSubscribtionToEvents(bool sub) {
		_manager.PlanetoidsManager.PlanetoidLeftPlay.SetSubscription(sub, OnPlanetoidsLeftPlay);
		_manager.Spaceship.Died.SetSubscription(sub, OnGameLost);
	}
	
	
	
	void OnGameLost() {
		float percentage = Mathf.Clamp01(1 - (CalculateAreaInPlay() / _areaAtStart));
		_manager.WinLooseMessage.text = _looseMessages.GetRandom() + "\n" + (percentage * 100.0f).ToString("0.0") + "% done";
		_manager.ProgressText.text = "You lost, try again?";
		
		_manager.StopGameMode();
	}
	void OnGameWon() {
		_stopwatch.Stop();
		TimeSpan currentTime = _stopwatch.Elapsed;
		if (ReportTime(currentTime)) {
			// a new record!
			_manager.WinLooseMessage.text =  _recordMessages.GetRandom() + "\n" + currentTime.ToString(TIME_FORMAT);
			_manager.ProgressText.text = "A new record! Try to beat it again?";
		}
		else {
			_manager.WinLooseMessage.text =  _winMessages.GetRandom() + "\n" + currentTime.ToString(TIME_FORMAT);
			_manager.ProgressText.text = "But no new record, try again?";
		}
		
		_manager.StopGameMode();
	}
	// returns true if it's a record
	public bool ReportTime(TimeSpan time) {
		if (_curRecord.CompareTo(time) > 0) { //smaller is better
			_curRecord = time;
			UpdateRecordText();
			SaveRecord();
			return true;
		}
		else return false;
	}
	void UpdateRecordText() {
		_recordText.text = "Record:\n" + _curRecord.ToString(TIME_FORMAT);
	}
	void SaveRecord() {
		// In playerprefs
		PlayerPrefsExt.SetTimeSpanEncrypted(TIME_TRIAL_RECORD_PLAYERPREF_ID, _curRecord);
		// In google play services if active
		if (Social.localUser.authenticated) {
			long miliseconds = _curRecord.Ticks / TimeSpan.TicksPerMillisecond;
			Social.ReportScore(miliseconds, TIME_TRIAL_LEADERBOARD_ID, (bool success) => {});
		}
	}
	void LoadKnownRecord() {
		ReportTime(PlayerPrefsExt.GetTimeSpanEncrypted(TIME_TRIAL_RECORD_PLAYERPREF_ID, TimeSpan.MaxValue));
	}
	void LoadGooglePlayRecord() {
		// TODO Add GooglePlayServices stuff
	}
	
	
	
	
	void Update () {
		_manager.ProgressText.text = _stopwatch.Elapsed.ToString(TIME_FORMAT);
	}
	
	void OnPlanetoidsLeftPlay() {
		UpdateProgressInfo();
		
		if (_manager.PlanetoidsManager.PlanetoidsInPlayCount == 0) {
			OnGameWon();
		}
	}
	
	void UpdateProgressInfo() {
		float percentage = Mathf.Clamp01(1 - (CalculateAreaInPlay() / _areaAtStart));
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
