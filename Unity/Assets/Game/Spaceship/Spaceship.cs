using UnityEngine;
using System.Collections;

[RequireComponent (typeof (Rigidbody2D))]
[RequireComponent (typeof (LaserManager))]
public class Spaceship : MonoBehaviour {
	
	Rigidbody2D _rigidbody;
	LaserManager _laserManager;
	
	public PlanetoidsManager _planetoidsManager;
	
	public float _moveForce = 10;
	public LayerMask _damagingLayers;
	
	public float _laserPower = 5;
	
	public float _autoAimAreaThreshold = 3.0f;
	public float _autoAimRadius = 1.0f;
	
	public float _maxTapMove = 5.0f;
	float _touchMoved = 0;
	
	void Awake () {
		_rigidbody = GetComponent<Rigidbody2D>();
		_laserManager = GetComponent<LaserManager>();
	}
	
	void Update () {
		if (Input.touchCount > 0) {
			var firstTouch = Input.GetTouch(0);
			if (firstTouch.phase == TouchPhase.Began) {
				_touchMoved = 0;
			}
			else if (firstTouch.phase == TouchPhase.Ended) {
				if (_touchMoved < _maxTapMove) {
					Vector2 touchScreenPos = AutoAimPosition(firstTouch.position);
					Vector2 shipScreenPos = Camera.main.WorldToScreenPoint(transform.position);
					
					Vector2 laserDir = touchScreenPos - shipScreenPos;
					
					Ray2D laserRay = new Ray2D(transform.position, laserDir);
					_planetoidsManager.SlicePlanetoids(laserRay, _laserPower);
					_laserManager.StartLaser(laserRay);
				}
			}
			else if (firstTouch.phase == TouchPhase.Moved) {
				_touchMoved += firstTouch.deltaPosition.magnitude;
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
		if (Input.touchCount > 0) {
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
			//Debug.Log("Damage: " + CalculateDamage(collision.relativeVelocity));
		}
	}
	float CalculateDamage(Vector2 relativeVelocity) {
		return relativeVelocity.sqrMagnitude;
	}
}
