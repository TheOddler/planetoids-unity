using System;
using UnityEngine;
using System.Collections;

[RequireComponent (typeof (Rigidbody2D))]
[RequireComponent (typeof (LaserManager))]
[RequireComponent (typeof (MeshFilter))]
public class Spaceship : MonoBehaviour {
	
	Rigidbody2D _rigidbody;
	LaserManager _laserManager;
	
	public GameManager _gameManager;
	public PlanetoidsManager _planetoidsManager;
	
	public Color32 _color;
	
	public float _shieldStrength = 100.0f;
	public float _velocityDamageMax = 200.0f;
	public float _initialHealth = 300;
	float _health;
	public ShieldVisuals _shieldVisuals;
	
	public float _moveForce = 10;
	public LayerMask _damagingLayers;
	
	public float _laserPower = 5;
	
	public float _autoAimAreaThreshold = 3.0f;
	public float _autoAimRadius = 1.0f;
	
	public float _maxTapTime = 0.5f;
	float _touchStartTime = 0;
	Vector2 _touchStartPos;
	
	public SmartEvent Died;
	
	void Awake () {
		_rigidbody = GetComponent<Rigidbody2D>();
		_laserManager = GetComponent<LaserManager>();
		
		Died = new SmartEvent(this);
	}
	
	void Start () {
		Helpers.UpdateMeshColor(GetComponent<MeshFilter>().mesh, _color);
	}
	
	public void Reset() {
		_health = _initialHealth;
		transform.position = Vector2.zero;
		
		UpdateShieldColor();
	}
	
	void Update () {
		if (_gameManager.GameRunning && Input.touchCount > 0) {
			var firstTouch = Input.GetTouch(0);
			if (firstTouch.phase == TouchPhase.Began) {
				_touchStartTime = Time.timeSinceLevelLoad;
				_touchStartPos = firstTouch.position;
			}
			else if (firstTouch.phase == TouchPhase.Ended) {
				#if UNITY_EDITOR && FALSE
				Debug.Log("Tap time: " + (Time.timeSinceLevelLoad - _touchStartTime));
				#endif
				if (Time.timeSinceLevelLoad - _touchStartTime < _maxTapTime) {
					Vector2 touchScreenPos = AutoAimPosition(_touchStartPos);
					Vector2 shipScreenPos = Camera.main.WorldToScreenPoint(transform.position);
					
					Vector2 laserDir = touchScreenPos - shipScreenPos;
					
					Ray2D laserRay = new Ray2D(transform.position, laserDir);
					_planetoidsManager.SlicePlanetoids(laserRay, _laserPower);
					_laserManager.StartLaser(laserRay);
				}
			}
		}
	}
	
	Vector2 AutoAimPosition (Vector2 touchScreenPos) {
		// auto aim
		Vector2 worldTouchPos = Camera.main.ScreenToWorldPoint(touchScreenPos);
		#if UNITY_EDITOR
		Debug.DrawLine(worldTouchPos - Vector2.up * _autoAimRadius, worldTouchPos + Vector2.up * _autoAimRadius, Color.green, 2);
		Debug.DrawLine(worldTouchPos - Vector2.right * _autoAimRadius, worldTouchPos + Vector2.right * _autoAimRadius, Color.green, 2);
		#endif
		
		Collider2D tappedPlanetoidCollider = Physics2D.OverlapCircle(worldTouchPos, _autoAimRadius, _planetoidsManager.PlanetoidsLayer);
		if (tappedPlanetoidCollider != null) {
			Planetoid tappedPlanetoid = tappedPlanetoidCollider.GetComponent<Planetoid>();
			if (tappedPlanetoid.GetArea() <= _autoAimAreaThreshold) {
				touchScreenPos = (Vector2)Camera.main.WorldToScreenPoint(tappedPlanetoid.WorldCenterOfMass);
				#if UNITY_EDITOR
				Debug.DrawLine(tappedPlanetoid.WorldCenterOfMass - Vector2.up, tappedPlanetoid.WorldCenterOfMass + Vector2.up, Color.red, 2);
				Debug.DrawLine(tappedPlanetoid.WorldCenterOfMass - Vector2.right, tappedPlanetoid.WorldCenterOfMass + Vector2.right, Color.red, 2);
				#endif
			}
		}
		
		return touchScreenPos;
	}
	
	void FixedUpdate () {
		if (_gameManager.GameRunning && Input.touchCount > 0) {
			var firstTouch = Input.GetTouch(0);
			Vector2 touchPos = firstTouch.position;
			Vector2 thisPos = Camera.main.WorldToScreenPoint(transform.position);
			
			Vector2 posDiff = touchPos - thisPos;
			_rigidbody.AddForce(posDiff.normalized * _moveForce);
			_rigidbody.rotation = Mathf.Atan2(posDiff.y,posDiff.x) * Mathf.Rad2Deg;
		}
	}
	
	void OnCollisionEnter2D(Collision2D collision) {
		if ((_damagingLayers.value & (1 << collision.gameObject.layer)) > 0) {
			Damage(CalculateDamage(collision.relativeVelocity, collision.rigidbody.mass));
		}
	}
	
	void Damage(float damage) {
		if (_gameManager.GameRunning && _health > 0) {
			_health -= damage;
			UpdateShieldColor();
			
			if (_health <= 0) {
				Died.CallOnceAtEndOfFrame();
			}
		}
	}
	
	float CalculateDamage(Vector2 relativeVelocity, float otherMass) {
		float magnitude = relativeVelocity.sqrMagnitude;
		#if UNITY_EDITOR
		if (magnitude > _velocityDamageMax) {
			Debug.Log("Hit velocity damage max, would have been: " + magnitude + "; other mass: " + otherMass);
		}
		#endif
		return Mathf.Min(magnitude, _velocityDamageMax) * otherMass / _shieldStrength;
	}
	
	void UpdateShieldColor() {
		float healthPercentage = _health / _initialHealth;
		_shieldVisuals.UpdateColor(healthPercentage);
	}
	
	
	
	#if UNITY_EDITOR && FALSE
	void OnGUI () {
		var screenPos = Camera.main.WorldToScreenPoint(_rigidbody.worldCenterOfMass);
		screenPos.y = Screen.height - screenPos.y; //fix y-flip
		
		var countContent = new GUIContent(_health.ToString());
		var halfSize = GUI.skin.label.CalcSize(countContent) / 2.0f;
		GUI.Label(new Rect(screenPos.x - halfSize.x, screenPos.y - halfSize.y, screenPos.x + halfSize.x, screenPos.y + halfSize.y), countContent);
	}
	#endif
}
