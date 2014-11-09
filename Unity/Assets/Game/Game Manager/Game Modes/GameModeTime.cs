using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameModeTime : AGameMode {
	
	public GameManager _manager;
	public int _planetoidCount = 10;
	
	float _areaAtStart;
	float _startTime;
	
	public List<string> _winMessages;
	public List<string> _recordMessages;
	public List<string> _looseMessages;
	
	void Awake () {
		gameObject.SetActive(false);
	}
	
	override public bool Running { get { return gameObject.activeSelf; } }
	
	override public void StartGame() {
		gameObject.SetActive(true);
		
		_manager.Spaceship.Reset();
		_manager.PlanetoidsManager.CreatePlanetoids(_planetoidCount);
		
		_areaAtStart = CalculateAreaInPlay();
		_startTime = Time.timeSinceLevelLoad;
		
		UpdateProgressInfo();
		
		SetSubscribtionToEvents(true);
	}
	override public void EndGame() {
		gameObject.SetActive(false);
		
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
		float runningTime = Time.timeSinceLevelLoad - _startTime;
		if (false) { //TODO New Record
			_manager.WinLooseMessage.text =  _recordMessages.GetRandom() + "\n" + runningTime.SecondsToStringMMSShh();
			_manager.ProgressText.text = "A new record! Try to beat it again?";
		}
		else {
			_manager.WinLooseMessage.text =  _winMessages.GetRandom() + "\n" + runningTime.SecondsToStringMMSShh();
			_manager.ProgressText.text = "But no new record, try again?";
		}
		
		_manager.StopGameMode();
	}
	
	
	
	void Update () {
		float runningTime = Time.timeSinceLevelLoad - _startTime;
		_manager.ProgressText.text = runningTime.SecondsToStringMMSShh();
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
