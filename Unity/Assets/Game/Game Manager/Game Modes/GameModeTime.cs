using UnityEngine;
using System.Collections;

public class GameModeTime : AGameMode {
	
	GameManager _manager;
	int _planetoidCount = 10;
	
	float _areaAtStart;
	
	public GameModeTime(int planetoidCount, GameManager manager) {
		_planetoidCount = planetoidCount;
		_manager = manager;
	}
	
	
	
	override public void Start() {
		_manager.ProgressBar.SetProgress(1.0f);
		_manager.Spaceship.Reset();
		_manager.PlanetoidsManager.CreatePlanetoids(_planetoidCount);
		
		PlanetoidsManager planetoidsManager = _manager.PlanetoidsManager;
		planetoidsManager.PlanetoidEnteredPlay	.Subscribe(UpdateProgressBar);
		planetoidsManager.PlanetoidLeftPlay		.Subscribe(UpdateProgressBar);
		
		_areaAtStart = CalculateAreaInPlay();
	}
	override public void End() {
		_manager.PlanetoidsManager.CashAllPlanetoids();
		
		PlanetoidsManager planetoidsManager = _manager.PlanetoidsManager;
		planetoidsManager.PlanetoidEnteredPlay	.Unsubscribe(UpdateProgressBar);
		planetoidsManager.PlanetoidLeftPlay		.Unsubscribe(UpdateProgressBar);
	}
	
	void UpdateProgressBar() {
		_manager.ProgressBar.SetProgress(CalculateAreaInPlay() / _areaAtStart);
	}
	
	float CalculateAreaInPlay() {
		float totalArea = 0;
		foreach (var planetoid in _manager.PlanetoidsManager.PlanetoidsInPlay) {
			totalArea += planetoid.GetArea();
		}
		return totalArea;
	}
	
}
