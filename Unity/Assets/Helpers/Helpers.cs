using UnityEngine;
using System.Collections;

public static class Helpers {
	
	static public void UpdateMeshColor (Mesh mesh, Color32 color) {
		Color32[] colors = mesh.colors32;
		for (int i = 0; i < colors.Length; ++i) {
			colors[i] = color;
		}
		mesh.colors32 = colors;
	}
	
	static public string SecondsToStringMMSShh(this float second) {
		return Mathf.FloorToInt(second/60).ToString("00") + ":" + (second % 60).ToString("00.00");
	}
	
}
