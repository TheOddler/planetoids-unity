using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LaserManager : MonoBehaviour {
	
	public float _fadeTime = 1;
	public float FadeTime { get { return _fadeTime; } }
	
	Mesh _laserMesh;
	public Mesh LaserMesh { get { return _laserMesh; } }
	
	public float _laserWidthHalf = 0.1f;
	public float _laserLength = 100.0f;
	
	public Laser _laserPrefab;
	List<Laser> _cashedLasers = new List<Laser>(20);
	
	
	
	void Start () {
		Vector3[] vertices = new Vector3[3];
		vertices[0] = new Vector3(_laserWidthHalf,0,0);
		vertices[1] = new Vector3(-_laserWidthHalf,0,0);
		vertices[2] = new Vector3(0,0,_laserLength);
		
		int[] triangles = new int[3];
		triangles[0] = 0;
		triangles[1] = 2;
		triangles[2] = 1;
		
		_laserMesh = new Mesh();
		_laserMesh.vertices = vertices;
		_laserMesh.triangles = triangles;
		_laserMesh.colors32 = new Color32[3];
	}
	
	public void StartLaser(Ray2D laser) {
		Laser laserToStart;
		
		if (_cashedLasers.Count > 0) {
			laserToStart = _cashedLasers[0];
			laserToStart.gameObject.SetActive(true);
			_cashedLasers.Remove(laserToStart);
		}
		else {
			GameObject newLaserObject = Instantiate(_laserPrefab.gameObject, Vector3.zero, Quaternion.identity) as GameObject;
			laserToStart = newLaserObject.GetComponent<Laser>();
			laserToStart.Initialize(this);
		}
		
		laserToStart.StartLaser(laser);
	}
	
	public void EndLaser(Laser toCash) {
		toCash.gameObject.SetActive(false);
		if (!_cashedLasers.Contains(toCash)) {
			_cashedLasers.Add(toCash);
		}
	}
}
