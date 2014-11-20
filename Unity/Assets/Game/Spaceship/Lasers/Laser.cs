using UnityEngine;
using System.Collections;

[RequireComponent (typeof (MeshFilter))]
public class Laser : MonoBehaviour {
	
	public const float LASER_DISTANCE = 100.0f;
	public const float LASER_Z_DEPTH = 1.0f;
	
	MeshFilter _meshFilter;
	
	LaserManager _manager;
	public Color32 _color;
	float fadeTime;
	
	void Awake () {
		_meshFilter = GetComponent<MeshFilter>();
	}
	
	public void Initialize(LaserManager manager) {
		_manager = manager;
		GetComponent<MeshFilter>().mesh = manager.LaserMesh;
	}
	
	public void StartLaser(Ray2D laser) {
		fadeTime = _manager.FadeTime;
		transform.position = new Vector3(laser.origin.x,laser.origin.y,LASER_Z_DEPTH);
		transform.rotation = Quaternion.LookRotation(laser.direction, Vector3.forward);
		UpdateColor();
	}
	
	void Update () {
		fadeTime -= Time.deltaTime;
		
		UpdateColor();
		transform.localScale = new Vector3(fadeTime / _manager.FadeTime, 1, 1);
		
		if (fadeTime <= 0) {
			_manager.EndLaser(this);
		}
	}
	
	void UpdateColor() {
		_color.a = (byte)(fadeTime / _manager.FadeTime * 255.0f);
		Helpers.UpdateMeshColor(_meshFilter.mesh, _color);
	}
}
