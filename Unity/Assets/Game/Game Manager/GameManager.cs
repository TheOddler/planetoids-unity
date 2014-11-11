using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using GooglePlayGames;
using UnityEngine.SocialPlatforms;

public class GameManager : MonoBehaviour {
	
	public const string PP_USES_GOOGLEPLAY = "PlayerUsesGooglePlay";
	public const int PP_TRUE = 1;
	public const int PP_FALSE = 0;
	
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
		
		TryAuthenticateGooglePlay(false, () => SetupFinished.CallOnceAtEndOfFrame());
	}
	void TryAuthenticateGooglePlay(bool forceRetry, Action onFinished) {
		_gameMenu.gameObject.SetActive(false);
		
		bool isFirstTime = !PlayerPrefs.HasKey(PP_USES_GOOGLEPLAY);
		bool usesGooglePlay = PlayerPrefs.GetInt(PP_USES_GOOGLEPLAY,PP_FALSE) == PP_TRUE;
		if (forceRetry || isFirstTime || usesGooglePlay ) {
			Social.localUser.Authenticate((bool success) => {
				onFinished();
				if (success) {
					GooglePlayAuthenticated.CallOnceAtEndOfFrame();
				}
				_gameMenu.gameObject.SetActive(true);
				
				if (isFirstTime || forceRetry) { //remember preference
					PlayerPrefs.SetInt(PP_USES_GOOGLEPLAY, success ? PP_TRUE : PP_FALSE );
				}
			});
		}
		else /* !forceRetry && !isFirstTime && !usesGoogleplay */ {
			onFinished();
			_gameMenu.gameObject.SetActive(true);
		}
	}
	
	
	void Update () {
		if (Input.GetKeyDown(KeyCode.Escape)) {
			WinLooseMessage.text = "Why did you give up?";
			ProgressText.text = "Want to try again?";
			
			StopGameModeImmediately();
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
	public void StopGameMode() {
		_gameMode.EndGame();
		
		GameModeEnded.CallOnceAtEndOfFrame();
		
		StartCoroutine(DelayShowGameMenu());
	}
	public void StopGameModeImmediately() {
		_gameMode.EndGame();
		
		GameModeEnded.CallOnceAtEndOfFrame();
		
		_gameMenu.gameObject.SetActive(true);
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
			TryAuthenticateGooglePlay(true, ()=>{});
		}
	}
	
}
