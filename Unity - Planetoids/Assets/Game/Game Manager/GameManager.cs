using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using GooglePlayGames;
using UnityEngine.SocialPlatforms;

public class GameManager : MonoBehaviour {
	
	public Spaceship _spaceship;
	public Spaceship Spaceship { get { return _spaceship; } }
	
	public PlanetoidsManager _planetoidsManager;
	public PlanetoidsManager PlanetoidsManager { get { return _planetoidsManager; } }
	
	public RectTransform _gameMenu;
	public float _gameMenuShowDelay = 2.0f;
	
	public ProgressBar _progressBar;
	public ProgressBar ProgressBar { get { return _progressBar; } }
	public Text _progressText;
	public Text ProgressText { get { return _progressText; } }
	public Text _winLooseMessage;
	public Text WinLooseMessage { get { return _winLooseMessage; } }
	
	AGameMode _gameMode;
	public List<AGameMode> _gameModes;
	public bool GameRunning { get { return _gameMode != null && _gameMode.Running; } }
	
	
	public SmartEvent GameModeEnded;
	public SmartEvent SetupFinished;
	public SmartEvent GooglePlayAuthenticated;
	
	public GameObject _notLoggedInIndicator;
	
	void Awake () {
		GameModeEnded = new SmartEvent(this);
		SetupFinished = new SmartEvent(this);
		GooglePlayAuthenticated = new SmartEvent(this);
		
		GooglePlayAuthenticated.SetSubscription(true, () => _notLoggedInIndicator.SetActive(false));
	}
	
	void Start () {
		#if UNITY_EDITOR
		//PlayGamesPlatform.DebugLogEnabled = true;
		#endif
		PlayGamesPlatform.Activate();
		
		SetupFinished.CallOnceAtEndOfFrame();
	}
	void TryAuthenticateGooglePlay(Action successCallback) {
		Social.localUser.Authenticate((bool success) => {
			if (success) {
				GooglePlayAuthenticated.CallOnceAtEndOfFrame();
				successCallback();
			}
			else {
				_notLoggedInIndicator.SetActive(true);
			}
		});
	}
	
	
	void Update () {
		if (GameRunning && Input.GetKeyDown(KeyCode.Escape)) {
			WinLooseMessage.text = "Why give up?";
			ProgressText.text = "Want to try again?";
			
			StopGameMode(true);
		}
	}
	
	public void StartGameMode(int index) {
		if (index >= _gameModes.Count) throw new UnityException("Game Mode index out of range.");
		
		if (_gameMode != null) {
			_gameMode.CleanUpGame();
		}
		
		_gameMode = _gameModes[index];
		_gameMode.StartGame();
		
		_gameMenu.gameObject.SetActive(false);
	}
	public void StopGameMode(bool instantly = false) {
		_gameMode.EndGame();
		
		GameModeEnded.CallOnceAtEndOfFrame();
		
		if (instantly) {
			_gameMenu.gameObject.SetActive(true);
		}
		else {
			StartCoroutine(DelayShowGameMenu());
		}
	}
	IEnumerator DelayShowGameMenu() {
		yield return new WaitForSeconds(_gameMenuShowDelay);
		_gameMenu.gameObject.SetActive(true);
	}
	
	public void ShowLeaderboards() {
		if (Social.localUser.authenticated) {
			Social.ShowLeaderboardUI();
		}
		else {
			TryAuthenticateGooglePlay(Social.ShowLeaderboardUI);
		}
	}
	
}
