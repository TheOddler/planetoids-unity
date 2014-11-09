using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class Helpers {
	
	static public void UpdateMeshColor (Mesh mesh, Color32 color) {
		Color32[] colors = mesh.colors32;
		for (int i = 0; i < colors.Length; ++i) {
			colors[i] = color;
		}
		mesh.colors32 = colors;
	}
	
	static public string SecondsToStringMMSShh(this float seconds) {
		return Mathf.FloorToInt(seconds/60).ToString("00") + ":" + (seconds % 60).ToString("00.00");
	}
	static public long SecondsToMiliseconds(this float seconds) {
		return (long)(seconds * 1000.0f + 0.5f);
	}
	
	static public T GetRandom<T>(this List<T> list) {
		return list[Random.Range(0,list.Count)];
	}
	
}
