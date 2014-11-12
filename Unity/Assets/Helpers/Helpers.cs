using System;
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
	
	// workaround for missing formatting for TimeSpan in c# versions lower than 4.0
	static public string ToString(this TimeSpan timeSpan, string format) {
		DateTime dtime = new DateTime(0).Add(timeSpan);
		return dtime.ToString(format);
	}
	
	static public T GetRandom<T>(this List<T> list) {
		return list[UnityEngine.Random.Range(0,list.Count)];
	}
	
}
