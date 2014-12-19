using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent (typeof (Rigidbody2D))]
[RequireComponent (typeof (LaserManager))]
[RequireComponent (typeof (MeshFilter))]
[RequireComponent (typeof (MeshRenderer))]
public class Spaceship : MonoBehaviour {

	Rigidbody2D _rigidbody;
	LaserManager _laserManager;
	MeshRenderer _renderer;

	public GameManager _gameManager;
	public PlanetoidsManager _planetoidsManager;

	public Color32 _color;

	public float _shieldStrength = 100.0f;
	public float _velocityDamageMax = 200.0f;
	public float _initialHealth = 300;
	float _health;
	public ShieldVisuals _shieldVisuals;
	public ParticleSystem _deathParticles;

	public float _moveForce = 10;
	public LayerMask _damagingLayers;

	public float _laserPower = 5;
	public float _laserRecoilForce = -10;

	public float _autoAimAreaThreshold = 3.0f;
	public float _autoAimRadius = 1.0f;

	public float _maxTapTime = 0.5f;
	float _touchStartTime = 0;
	Planetoid _touchStartAutoAimPlanetoid;

	public SmartEvent Died;
	
	public AudioSource _laserSource;
	public AudioSource _thrusterSource;
	float _thrusterSoundDampingVel;
	public List<AudioClip> _laserSounds;

	void Awake () {
		_rigidbody = GetComponent<Rigidbody2D>();
		_laserManager = GetComponent<LaserManager>();
		_renderer = GetComponent<MeshRenderer>();

		Died = new SmartEvent(this);
	}

	void Start () {
		Helpers.UpdateMeshColor(GetComponent<MeshFilter>().mesh, _color);
		Died.SetSubscription(true, DoDeathEffect);
	}

	public void Reset() {
		_health = _initialHealth;
		transform.position = Vector2.zero;

		StopDeathEffect();

		UpdateShieldColor();
	}
	void DoDeathEffect() {
		StartCoroutine(StartDeathEffect());
	}
	IEnumerator StartDeathEffect() {
		_deathParticles.Clear();
		_deathParticles.Play();

		_shieldVisuals.gameObject.SetActive(false);
		_rigidbody.simulated = false;

		yield return new WaitForSeconds(1.5f);

		_renderer.enabled = false;
	}
	void StopDeathEffect() {
		StopAllCoroutines();

		_deathParticles.Stop();
		_shieldVisuals.gameObject.SetActive(true);
		_renderer.enabled = true;
		_rigidbody.simulated = true;
	}

	void Update () {
		if (_gameManager.GameRunning) {
			if (Input.touchCount > 0) {
				var firstTouch = Input.GetTouch(0);
				if (firstTouch.phase == TouchPhase.Began) {
					_touchStartTime = Time.timeSinceLevelLoad;
					_touchStartAutoAimPlanetoid = AutoAim(firstTouch.position);
				}
				else if (firstTouch.phase == TouchPhase.Ended) {
					if (Time.timeSinceLevelLoad - _touchStartTime < _maxTapTime) {
						FireLaser(_touchStartAutoAimPlanetoid, firstTouch.position);
					}
				}
				else if (Input.touchCount >= 2) {
					var secondTouch = Input.GetTouch(1);
					if (secondTouch.phase == TouchPhase.Began) {
						FireLaser(AutoAim(firstTouch.position), secondTouch.position);
					}
				}
			}
			else if (Input.GetMouseButtonDown(0)) {
				_touchStartTime = Time.timeSinceLevelLoad;
				_touchStartAutoAimPlanetoid = AutoAim(Input.mousePosition);
			}
			else if (Input.GetMouseButtonUp(0)) {
				if (Time.timeSinceLevelLoad - _touchStartTime < _maxTapTime) {
					FireLaser(_touchStartAutoAimPlanetoid, Input.mousePosition);
				}
			}
		}
	}
	void FixedUpdate () {
		if (_gameManager.GameRunning) {
			if (Input.touchCount > 0) {
				var firstTouch = Input.GetTouch(0);
				MovementUpdate(firstTouch.position);
			}
			else if (Input.GetMouseButton(0) && !Input.GetMouseButtonDown(0)) {
				MovementUpdate(Input.mousePosition);
			}
			else {
				NoMovementUpdate();
			}
		}
		else {
			NoMovementUpdate();
		}
	}
	void MovementUpdate(Vector2 screenTouchPos) {
		Vector2 touchPos = screenTouchPos;
		Vector2 thisPos = Camera.main.WorldToScreenPoint(transform.position);
		
		Vector2 dirVec = (touchPos - thisPos).normalized;
		_rigidbody.AddForce(dirVec * _moveForce);
		_rigidbody.rotation = Mathf.Atan2(dirVec.y,dirVec.x) * Mathf.Rad2Deg;
	
		_thrusterSource.volume = Mathf.SmoothDamp(_thrusterSource.volume, 1, ref _thrusterSoundDampingVel, .15f);
	}
	void NoMovementUpdate() {
		_thrusterSource.volume = Mathf.SmoothDamp(_thrusterSource.volume, 0, ref _thrusterSoundDampingVel, .15f);
	}

	void FireLaser(Planetoid autoAimPlanetoid, Vector2 screenTouchPos) {
		Vector2 shipScreenPos = Camera.main.WorldToScreenPoint(transform.position);

		if (autoAimPlanetoid != null) {
			screenTouchPos = (Vector2)Camera.main.WorldToScreenPoint(autoAimPlanetoid.WorldCenterOfMass);
		}
		Vector2 laserDir = screenTouchPos - shipScreenPos;
		laserDir.Normalize();
		
		
		Ray2D laserRay = new Ray2D(transform.position, laserDir);
		_planetoidsManager.SlicePlanetoids(laserRay, _laserPower);
		_laserManager.StartLaser(laserRay);
		
		_rigidbody.AddForce(laserDir * _laserRecoilForce);
		
		_laserSource.PlayOneShot(_laserSounds.GetRandom(), 1.0f);
	}

	Planetoid AutoAim (Vector2 touchScreenPos) {
		// auto aim
		Vector2 worldTouchPos = Camera.main.ScreenToWorldPoint(touchScreenPos);
		#if UNITY_EDITOR
		Debug.DrawLine(worldTouchPos - Vector2.up * _autoAimRadius, worldTouchPos + Vector2.up * _autoAimRadius, Color.green, 2);
		Debug.DrawLine(worldTouchPos - Vector2.right * _autoAimRadius, worldTouchPos + Vector2.right * _autoAimRadius, Color.green, 2);
		#endif

		Collider2D tappedPlanetoidCollider = Physics2D.OverlapCircle(worldTouchPos, _autoAimRadius, _planetoidsManager.PlanetoidsLayer);
		if (tappedPlanetoidCollider != null) {
			Planetoid tappedPlanetoid = tappedPlanetoidCollider.GetComponent<Planetoid>();
			if (!tappedPlanetoid.Fading && tappedPlanetoid.GetArea() <= _autoAimAreaThreshold) {
				//touchScreenPos = (Vector2)Camera.main.WorldToScreenPoint(tappedPlanetoid.WorldCenterOfMass);
				#if UNITY_EDITOR
				Debug.DrawLine(tappedPlanetoid.WorldCenterOfMass - Vector2.up, tappedPlanetoid.WorldCenterOfMass + Vector2.up, Color.red, 2);
				Debug.DrawLine(tappedPlanetoid.WorldCenterOfMass - Vector2.right, tappedPlanetoid.WorldCenterOfMass + Vector2.right, Color.red, 2);
				#endif
				return tappedPlanetoid;
			}
		}

		return null;
	}

	void OnCollisionEnter2D(Collision2D collision) {
		if ((_damagingLayers.value & (1 << collision.gameObject.layer)) > 0) {
			Damage(CalculateDamage(collision.relativeVelocity));
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

	float CalculateDamage(Vector2 relativeVelocity) {
		float magnitude = relativeVelocity.sqrMagnitude;
		return Mathf.Min(magnitude, _velocityDamageMax) / _shieldStrength;
	}

	void UpdateShieldColor() {
		float healthPercentage = _health / _initialHealth;
		_shieldVisuals.UpdateColor(healthPercentage);
	}



	#if UNITY_EDITOR && false
	void OnGUI () {
		var screenPos = Camera.main.WorldToScreenPoint(_rigidbody.worldCenterOfMass);
		screenPos.y = Screen.height - screenPos.y; //fix y-flip

		var countContent = new GUIContent(_health.ToString());
		var halfSize = GUI.skin.label.CalcSize(countContent) / 2.0f;
		GUI.Label(new Rect(screenPos.x - halfSize.x, screenPos.y - halfSize.y, screenPos.x + halfSize.x, screenPos.y + halfSize.y), countContent);
	}
	#endif
}
