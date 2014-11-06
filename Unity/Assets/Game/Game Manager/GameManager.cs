using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviour {
	
	public Spaceship _spaceship;
	public Spaceship Spaceship { get { return _spaceship; } }
	
	public PlanetoidsManager _planetoidsManager;
	public PlanetoidsManager PlanetoidsManager { get { return _planetoidsManager; } }
	
	public RectTransform _gameModeMenu;
	
	public ProgressBar _progressBar;
	public ProgressBar ProgressBar { get { return _progressBar; } }
	public Text _progressText;
	public Text ProgressText { get { return _progressText; } }
	
	AGameMode _gameMode;
	public List<AGameMode> _gameModes;
	public bool GameRunning { get { return _gameMode != null && _gameMode.Running; } }
	
	public void StartGameMode(int index) {
		if (index >= _gameModes.Count) throw new UnityException("Game Mode index out of range.");
		
		if (_gameMode != null) {
			_gameMode.CleanUpGame();
		}
		
		_gameMode = _gameModes[index];
		_gameMode.StartGame();
		
		_gameModeMenu.gameObject.SetActive(false);
	}
	public void StopGameMode() {
		_gameMode.EndGame();
		
		_gameModeMenu.gameObject.SetActive(true);
	}
	
}
