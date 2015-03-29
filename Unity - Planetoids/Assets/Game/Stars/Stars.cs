using UnityEngine;
using System.Collections;

[RequireComponent (typeof (MeshFilter))]
public class Stars : MonoBehaviour {
	
	MeshFilter _meshFilter;
	public int _count = 100;
	public float _minSize = .05f;
	public float _maxSize = .1f;
	
	void Awake() {
		_meshFilter = GetComponent<MeshFilter>();
	}
	
	void Start () {
		GenerateStars(_count);
	}
	
	void GenerateStars(int count) {
		Vector2 bottomLeft = Camera.main.ViewportToWorldPoint(Vector2.zero);
		Vector2 topRight = Camera.main.ViewportToWorldPoint(Vector2.one);
		
		Mesh mesh = _meshFilter.mesh;
		Vector3[] vertices = new Vector3[count * 3];
		int[] triangles = new int[count * 3];
		
		for (int i = 0; i < count; ++i) {
			Vector2 point = new Vector2(Random.Range(bottomLeft.x, topRight.x), Random.Range(bottomLeft.y, topRight.y));
			float size = Random.Range(_minSize, _maxSize);
			
			float dir = Random.Range(0.0f, Mathf.PI * 2.0f);
			vertices[i*3 + 0] = new Vector3(point.x + Mathf.Cos(dir) * size, point.y + Mathf.Sin(dir) * size, 0);
			dir += Mathf.PI * 2.0f / 3.0f;
			vertices[i*3 + 1] = new Vector3(point.x + Mathf.Cos(dir) * size, point.y + Mathf.Sin(dir) * size, 0);
			dir += Mathf.PI * 2.0f / 3.0f;
			vertices[i*3 + 2] = new Vector3(point.x + Mathf.Cos(dir) * size, point.y + Mathf.Sin(dir) * size, 0);
			
			triangles[i*3 + 0] = i*3 + 0;
			triangles[i*3 + 1] = i*3 + 2;
			triangles[i*3 + 2] = i*3 + 1;
		}
		
		mesh.Clear();
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		_meshFilter.mesh = mesh;
	}
}
