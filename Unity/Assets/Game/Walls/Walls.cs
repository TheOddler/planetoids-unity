using UnityEngine;
using System.Collections;

[RequireComponent (typeof (EdgeCollider2D))]
public class Walls : MonoBehaviour {
	
	EdgeCollider2D _collider;
	
	void Awake () {
		_collider = GetComponent<EdgeCollider2D>();
	}
	
	void Start () {
		PlaceWalls();
	}
	
	void PlaceWalls () {
		Vector2 bottomLeft = Camera.main.ViewportToWorldPoint(Vector2.zero);
		Vector2 topRight = Camera.main.ViewportToWorldPoint(Vector2.one);
		
		Vector2[] points = new Vector2[5];
		points[0] = points[4] = bottomLeft;
		points[1] = new Vector2(bottomLeft.x, topRight.y);
		points[2] = topRight;
		points[3] = new Vector2(topRight.x, bottomLeft.y);
		
		_collider.points = points;
	}
}
