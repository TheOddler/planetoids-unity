using System;
using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour {
	
	public Spaceship _spaceship;
	public Spaceship Spaceship { get { return _spaceship; } }
	
	public PlanetoidsManager _planetoidsManager;
	public PlanetoidsManager PlanetoidsManager { get { return _planetoidsManager; } }
	
	public ProgressBar _progressBar;
	public ProgressBar ProgressBar { get { return _progressBar; } }
	
	AGameMode _currentGameMode;
	
	void Awake () {
		_currentGameMode = new GameModeTime(10, this);
	}

	// Use this for initialization
	void Start () {
		_currentGameMode.Start();
	}
	
}
