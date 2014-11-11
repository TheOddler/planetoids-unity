using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class GameModeArea : AGameMode {
	
	public const string AREA_ATTACK_LEADERBOARD_ID = "CgkIgr_5uO8aEAIQAQ";
	
	public GameManager _manager;
	public float _minWantedArea = 10;
	
	float _totalAddedArea;
	float _totalDestroyedArea;
	float _lastInGameArea;
	
	long _curRecord;
	public long CurrentRecord {
		get { return _curRecord; }
		private set {
			_curRecord = value;
			if (_curRecord > 0) {
				_recordText.text = "Record:\n" + _curRecord.TwoDecimalAreaToString() + " m²";
			}
			else {
				_recordText.text = "Record:\nNone";
			}
		}
	}
	public bool UpdateRecord(long newRecord) {
		if (newRecord > _curRecord) {
			CurrentRecord = newRecord;
			return true;
		}
		else {
			return false;
		}
	}
	
	public List<string> _finishMessages;
	public List<string> _recordMessages;
	public Text _recordText; 
	
	void Awake () {
		gameObject.SetActive(false);
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
		long currentScore = _totalDestroyedArea.AreaToLongTwoDecimal();
		
		if (Social.localUser.authenticated) {
			Social.ReportScore(currentScore, AREA_ATTACK_LEADERBOARD_ID, (bool success) => {
				CheckScore(currentScore);
				_manager.StopGameMode();
			});
		}
		else {
			CheckScore(currentScore);
			_manager.StopGameMode();
		}
	}
	void CheckScore(long currentScore) {
		if (currentScore > CurrentRecord) { //TODO New Record
			CurrentRecord = currentScore;
			_manager.WinLooseMessage.text =  _recordMessages.GetRandom() + "\n" + currentScore.TwoDecimalAreaToString() + " m²";
			_manager.ProgressText.text = "A new record! Try to beat it again?";
		}
		else {
			_manager.WinLooseMessage.text =  _finishMessages.GetRandom() + "\n" + currentScore.TwoDecimalAreaToString() + " m²";
			_manager.ProgressText.text = "But no new record, try again?";
		}
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
