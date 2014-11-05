using UnityEngine;
using System.Collections;

public struct SlicePoint {
	public SlicePoint(Vector2 point, int side) {
		this.point = point;
		this.side = side;
	}

	public Vector2 point;
	public int side;
}

[RequireComponent (typeof (Rigidbody2D))]
[RequireComponent (typeof (PolygonCollider2D))]
[RequireComponent (typeof (MeshFilter))]
public class Planetoid : MonoBehaviour {

	const float STEP_OFFSET = 0.05f; // in %
	const float MINIMUM_AREA = .01f;

	Rigidbody2D _rigidbody;
	PolygonCollider2D _collider;
	MeshFilter _meshFilter;
	MeshRenderer _renderer;

	public float _density = 1.0f;
	public bool _fading = false;
	public Color32 _color;

	public Vector2 WorldCenterOfMass {
		get { return _rigidbody.worldCenterOfMass; }
	}

	void Awake () {
		_rigidbody = GetComponent<Rigidbody2D>();
		_collider = GetComponent<PolygonCollider2D>();
		_meshFilter = GetComponent<MeshFilter>();
		_renderer = GetComponent<MeshRenderer>();
	}

	IEnumerator FadeAway(PlanetoidsManager manager) {
		float fadeTime = manager.FadeTime;
		float fullMass = _rigidbody.mass;

		do {
			float fadePercentage = fadeTime / manager.FadeTime;
			
			_color.a = (byte)(fadePercentage * 255.0f);
			Helpers.UpdateMeshColor(_meshFilter.mesh, _color);
			
			_rigidbody.mass = fadePercentage * fullMass;
			
			fadeTime -= Time.deltaTime;
			yield return null;
		} while (fadeTime > 0);

		manager.CashPlanetoid(this);
		yield break;
	}
	
	void OnDisable() {
		StopAllCoroutines();
		//to make sure the fade away coroutine isn't still running when
		//cashing all Planetoids and then recreating some.
	}

	public void Initialize (float size, int sideCount, PlanetoidsManager manager) {
		// generate points
		Vector2[] points = new Vector2[sideCount];
		float step = Mathf.PI * 2.0f / sideCount;
		for (int i = 0; i < sideCount; ++i) {
			float angle = i * step + Random.Range(STEP_OFFSET,step - STEP_OFFSET);
			points[i] = new Vector2(Mathf.Cos(angle) * size, Mathf.Sin(angle) * size);
		}
		// Initialize further using those points
		Initialize(points, manager);
	}
	public void Initialize (Vector2[] points, PlanetoidsManager manager) {
		int sideCount = points.Length;
		Vector3[] pointsV3 = new Vector3[sideCount];
		Color32[] colors = new Color32[sideCount];
		for (int i = 0; i < sideCount; ++i) {
			pointsV3[i] = points[i];
			colors[i] = _color;
		}
		// set collider
		_collider.points = points;

		// create triangles
		int[] triangles = new int[(sideCount-2)*3];
		for (int i = 0; i < sideCount-2; ++i) {
			triangles[i*3]   = 0;
			triangles[i*3+1] = i+2;
			triangles[i*3+2] = i+1;
		}
		// set mesh
		_meshFilter.mesh.Clear();
		_meshFilter.mesh.vertices = pointsV3;
		_meshFilter.mesh.triangles = triangles;
		_meshFilter.mesh.colors32 = colors;

		// set mass
		float area = GetArea(points);
		_rigidbody.mass = _density * area;

		// fade?
		if (area <= manager.DeathArea) {
			_fading = true;
			_renderer.sharedMaterial = manager.MatTransparant;
			StartCoroutine(FadeAway(manager));
		}
		else {
			_fading = false;
			_renderer.sharedMaterial = manager.MatOpaque;
		}
	}

	// returns null if nothing was sliced
	// even when hit by the laser nothing might get sliced
	//	this is because I limit the minimum size of a new
	//	planetoid for performance reasons.
	//
	public Planetoid Slice(Ray2D laser, float laserPower, PlanetoidsManager manager) {
		if (_fading) return null;

		try {
			Vector2 start = transform.InverseTransformPoint(laser.origin);
			Vector2 end = transform.InverseTransformPoint(laser.origin + laser.direction * Laser.LASER_DISTANCE);

			SlicePoint[] slicePoints = new SlicePoint[2];
			Vector2[] vertices = _collider.points;
			int foundPoints = 0;
			for (int i = 0; i < vertices.Length && foundPoints <= 2; ++i) {
				//Calculate intersectionPoint
				//For formula see http://en.wikipedia.org/wiki/Line-line_intersection
				//I'm using x1, x2, ... since that's easier to write.
				float x1 = start.x, x2 = end.x, x3 = vertices[i].x, x4 = vertices[(i+1)%vertices.Length].x;
				float y1 = start.y, y2 = end.y, y3 = vertices[i].y, y4 = vertices[(i+1)%vertices.Length].y;
				Vector2 intersectionPoint = new Vector2(
					(((x1*y2 - y1*x2)*(x3 - x4)) - ((x1-x2)*(x3*y4 - y3*x4))) / (((x1-x2)*(y3-y4))-((y1-y2)*(x3-x4))),
					(((x1*y2 - y1*x2)*(y3 - y4)) - ((y1-y2)*(x3*y4 - y3*x4))) / (((x1-x2)*(y3-y4))-((y1-y2)*(x3-x4)))
					);

				//See if the point is is actually on the side & laser.

				float startx =	Mathf.Min(vertices[i].x, vertices[(i+1)%vertices.Length].x); //side
				float endx =	Mathf.Max(vertices[i].x, vertices[(i+1)%vertices.Length].x);
				float starty =	Mathf.Min(vertices[i].y, vertices[(i+1)%vertices.Length].y);
				float endy =	Mathf.Max(vertices[i].y,vertices[(i+1)%vertices.Length].y);

				float start2x =	Mathf.Min(start.x, end.x); //laser
				float end2x = 	Mathf.Max(start.x, end.x);
				float start2y =	Mathf.Min(start.y, end.y);
				float end2y = 	Mathf.Max(start.y,end.y);
				if ( intersectionPoint.x >= startx  && intersectionPoint.x < endx  && intersectionPoint.y >= starty  && intersectionPoint.y < endy
				  && intersectionPoint.x >= start2x && intersectionPoint.x < end2x && intersectionPoint.y >= start2y && intersectionPoint.y < end2y) {
					//Add to the slicePoints vector if you find anything
					// throws when too many points were found
					slicePoints[foundPoints] = new SlicePoint(intersectionPoint, i);
					++foundPoints;
				}
			}

			if (foundPoints != 2) {
				throw new UnityException("Wrong number of points found while slicing: " + foundPoints);
			}

			//Calculate the number of sides each part of the rock will have
			int numberOfPoints1 = Mathf.Abs(slicePoints[0].side - slicePoints[1].side);
			int numberOfPoints2 = vertices.Length - numberOfPoints1;
			//numberOfPoints1 = min(numberOfPoints1, b2_maxPolygonVertices-2); //Make sure there aren't too many vertexes (Box2D 'limitation')
			//numberOfPoints2 = min(numberOfPoints2, b2_maxPolygonVertices-2); //I raised the max Polygon verts to 16 (default 8) so it will almost never happen, though when it does it's almost invisible
			//Create vectors to save the sides of each new rock
			Vector2[] sides1 = new Vector2[numberOfPoints1 + 2];
			Vector2[] sides2 = new Vector2[numberOfPoints2 + 2];
			
			//Filling in the first vector
			int counter = slicePoints[0].side +1;
			for (var i = 0; i < numberOfPoints1; ++i) {
				if(counter >= vertices.Length)
					throw new UnityException("Whoops, something wrong with the slicing. Should never happen, please report this."); //Should never go over 0
				sides1[i] = vertices[counter];
				++counter;
			}
			//Lastly add the intersection points to the sides1.
			sides1[numberOfPoints1 + 0] = slicePoints[1].point; //Since I add them to the back the second one found will always be the first one here.
			sides1[numberOfPoints1 + 1] = slicePoints[0].point;
			
			//Filling in the second one
			for (var i = 0; i < numberOfPoints2; ++i) {
				counter = counter % vertices.Length;
				sides2[i] = vertices[counter];
				++counter;
			}
			sides2[numberOfPoints2 + 0] = slicePoints[0].point;
			sides2[numberOfPoints2 + 1] = slicePoints[1].point;
			
			//Check if both new polygons are valid. Their size & winding will be checked, also they have to be convex.
			if (GetArea(sides1) < MINIMUM_AREA || GetArea(sides2) < MINIMUM_AREA) {
				throw new UnityException("At least one of the new planetoids is too small, not slicing for performance reasons.");
			}
			
			//Get some values needed to set later.
			Vector2 linVel = _rigidbody.velocity;
			float angleVel = _rigidbody.angularVelocity;
			Vector2 dir = laser.direction;
			Vector2 lr = new Vector2(-dir.y, dir.x);
			lr.Normalize();
			lr *= laserPower;
			
			//Change this rock.
			Initialize(sides1, manager);
			
			//Make the other part.
			Planetoid newRock = manager.GetNewOrCashedPlanetoid();
			newRock.Initialize(sides2, manager);
			newRock.transform.position = transform.position;
			newRock.transform.rotation = transform.rotation;
			newRock._rigidbody.velocity = linVel;
			newRock._rigidbody.angularVelocity = angleVel;
			newRock._density = _density;
			
			//Set Velocities.
			_rigidbody.AddForce(lr, ForceMode2D.Impulse);
			newRock._rigidbody.AddForce(-lr, ForceMode2D.Impulse);
			
			return newRock;
		}
		catch (UnityException ex) {
			Debug.Log("Failed slicing: " + ex.Message);
			return null;
		}
		catch {
			Debug.Log("Failed slicing for an unknown reason");
			return null;
		}
	}

	static public float GetArea(Vector2[] points) {
		float area = 0;
		for (var i = 0; i < points.Length; ++i) {
			Vector2 p1 = points[i];
			Vector2 p2 = points[(i+1)%points.Length];
			
			area += (p1.x*p2.y - p2.x*p1.y);
		}
		area /= 2.0f;
		area = Mathf.Abs(area);
		return area;
	}

	public float GetArea() {
		return GetArea(_collider.points);
	}
	
	#if UNITY_EDITOR && FALSE
	void OnGUI () {
		var screenPos = Camera.main.WorldToScreenPoint(_rigidbody.worldCenterOfMass);
		screenPos.y = Screen.height - screenPos.y; //fix y-flip
		
		var countContent = new GUIContent(GetArea().ToString());
		var halfSize = GUI.skin.label.CalcSize(countContent) / 2.0f;
		GUI.Label(new Rect(screenPos.x - halfSize.x, screenPos.y - halfSize.y, screenPos.x + halfSize.x, screenPos.y + halfSize.y), countContent);
	}
	#endif
}
