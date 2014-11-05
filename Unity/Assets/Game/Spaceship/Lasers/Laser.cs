using UnityEngine;
using System.Collections;

[RequireComponent (typeof (MeshFilter))]
public class Laser : MonoBehaviour {
	
	public const float LASER_DISTANCE = 100.0f;
	
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
		transform.position = laser.origin;
		transform.rotation = Quaternion.LookRotation(laser.direction, Vector3.forward);
		UpdateColor();
	}
	
	void Update () {
		fadeTime -= Time.deltaTime;
		
		UpdateColor();
		
		if (fadeTime <= 0) {
			_manager.EndLaser(this);
		}
	}
	
	void UpdateColor() {
		_color.a = (byte)(fadeTime / _manager.FadeTime * 255.0f);
		Color32[] colors = _meshFilter.mesh.colors32;
		for (int i = 0; i < _meshFilter.mesh.vertices.Length; ++i) {
			colors[i] = _color;
		}
		_meshFilter.mesh.colors32 = colors;
	}
}
