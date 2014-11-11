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
		return seconds.SecondsToMiliseconds().MilisecondsToStringMMSShh();
		//return Mathf.FloorToInt(seconds/60).ToString("00") + ":" + (seconds % 60).ToString("00.00");
	}
	static public string MilisecondsToStringMMSShh(this long miliseconds) {
		return (miliseconds / 1000 / 60).ToString("00") + ":" + (miliseconds / 1000 % 60).ToString("00") + "." + (miliseconds / 10 % 100).ToString("00");
	}
	static public long SecondsToMiliseconds(this float seconds) {
		return (long)(seconds * 1000.0f + 0.5f);
	}
	
	static public long AreaToLongTwoDecimal(this float area) {
		return (long)(area * 100.0f + 0.5f);
	}
	static public string TwoDecimalAreaToString(this long area) {
		return (area / 100).ToString() + "." + (area % 100).ToString();
	}
	
	static public T GetRandom<T>(this List<T> list) {
		return list[Random.Range(0,list.Count)];
	}
	
	static public long Max(long a, long b) {
		return a > b ? a : b;
	}
	
}
