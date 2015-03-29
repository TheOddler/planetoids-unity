using UnityEngine;
using System.Collections;

public class ShieldVisuals : MonoBehaviour {
	
	public Color32 _fullHealthColor;
	public Color32 _noHealthColor;
	
	public Color32 _innerColor;
	
	public MeshFilter _outerMeshFilter;
	public MeshFilter _innerMeshFilter;
	
	void Awake () {
		//add colors to the shield mesh
		_outerMeshFilter.mesh.colors32 = new Color32[_outerMeshFilter.mesh.vertices.Length];
		_innerMeshFilter.mesh.colors32 = new Color32[_innerMeshFilter.mesh.vertices.Length];
		
		Helpers.UpdateMeshColor(_innerMeshFilter.mesh, _innerColor);
	}
	
	public void UpdateColor (float percentage) {
		Color32 color = Color32.Lerp(_noHealthColor, _fullHealthColor, percentage);
		Helpers.UpdateMeshColor(_outerMeshFilter.mesh, color);
	}
}
