using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlanetoidsManager : MonoBehaviour {
	
	public int _initialPlanetoidCount = 10;
	public float _planetoidSize = 3;
	public int _planetoidPointCount = 5;
	public float _centerDeadZoneSize = 3;
	
	public float _deathArea = 1.0f;
	public float DeathArea { get { return _deathArea; } }
	
	public float _fadeTime = 1;
	public float FadeTime { get { return _fadeTime; } }
	
	public Material _matOpaque;
	public Material MatOpaque { get { return _matOpaque; } }
	public Material _matTransparant;
	public Material MatTransparant { get { return _matTransparant; } }
	
	public Planetoid _planetoidPrefab;
	
	public int PlanetoidsLayer { get { return 1 << _planetoidPrefab.gameObject.layer; } }
	
	List<Planetoid> _planetoids = new List<Planetoid>(30);
	public IEnumerable<Planetoid> PlanetoidsInPlay { get { return _planetoids; } }
	public int PlanetoidsInPlayCount { get { return _planetoids.Count; } }
	List<Planetoid> _cashedPlanetoids = new List<Planetoid>(30);
	
	public SmartEvent PlanetoidLeftPlay;
	public SmartEvent PlanetoidEnteredPlay;
	
	void Awake() {
		PlanetoidLeftPlay = new SmartEvent(this);
		PlanetoidEnteredPlay = new SmartEvent(this);
	}
	
	public void CreatePlanetoids(int count) {
		for (int i = 0; i < count; ++i) {
			CreatePlanetoid();
		}
	}
	
	public void CreatePlanetoid () {
		Vector2 bottomLeft = Camera.main.ViewportToWorldPoint(Vector2.zero);
		bottomLeft.x += _planetoidSize;
		bottomLeft.y += _planetoidSize;
		Vector2 topRight = Camera.main.ViewportToWorldPoint(Vector2.one);
		topRight.x -= _planetoidSize;
		topRight.y -= _planetoidSize;
		
		Vector2 pos = new Vector2();
		do {
			pos.x = UnityEngine.Random.Range(bottomLeft.x, topRight.x);
			pos.y = UnityEngine.Random.Range(bottomLeft.y, topRight.y);
		} while (Vector2.Distance(pos, Vector2.zero) < _centerDeadZoneSize);
		
		CreatePlanetoid(pos);
	}
	
	public void CreatePlanetoid(Vector2 pos) {
		Planetoid newPlanetoid = GetNewOrCashedPlanetoid();
		newPlanetoid.transform.position = pos;
		newPlanetoid.Initialize(_planetoidSize, _planetoidPointCount, this);
	}
	
	
	
	public void SlicePlanetoids(Ray2D laser, float laserPower) {
		
		Debug.DrawLine(laser.origin, laser.origin + laser.direction * Laser.LASER_DISTANCE, Color.red, 2.0f);
		
		RaycastHit2D[] hits = Physics2D.RaycastAll(laser.origin, laser.direction, Laser.LASER_DISTANCE, 1 << _planetoidPrefab.gameObject.layer);
		foreach (var hit in hits) {
			Planetoid hitPlanetoid = hit.transform.GetComponent<Planetoid>();
			hitPlanetoid.Slice(laser, laserPower, this);
		}
	}
	
	
	
	
	public Planetoid GetNewOrCashedPlanetoid() {
		Planetoid planetoid;
		if (_cashedPlanetoids.Count > 0) {
			planetoid = _cashedPlanetoids[0];
			planetoid.gameObject.SetActive(true);
			_cashedPlanetoids.Remove(planetoid);
			_planetoids.Add(planetoid);
		}
		else {
			GameObject newPlanetoidObject = Instantiate(_planetoidPrefab.gameObject, Vector3.zero, Quaternion.identity) as GameObject;
			newPlanetoidObject.transform.parent = transform;
			planetoid = newPlanetoidObject.GetComponent<Planetoid>();
			_planetoids.Add(planetoid);
		}
		
		PlanetoidEnteredPlay.CallOnceAtEndOfFrame();
		
		return planetoid;
	}
	
	public void CashPlanetoid(Planetoid toCash) {
		toCash.gameObject.SetActive(false);
		if (!_cashedPlanetoids.Contains(toCash)) {
			_cashedPlanetoids.Add(toCash);
			_planetoids.Remove(toCash);
		}
		
		PlanetoidLeftPlay.CallOnceAtEndOfFrame();
	}
	
	public void CashAllPlanetoids() {
		foreach (var planetoid in _planetoids) {
			planetoid.gameObject.SetActive(false);
		}
		_cashedPlanetoids.AddRange(_planetoids);
		_planetoids.Clear();
		
		PlanetoidLeftPlay.CallOnceAtEndOfFrame();
	}
}
