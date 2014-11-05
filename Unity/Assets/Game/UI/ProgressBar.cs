using UnityEngine;
using System.Collections;

public class ProgressBar : MonoBehaviour {
	
	public RectTransform _bar;
	
	public void SetProgress(float percentage) {
		float barTotalWidth = _bar.rect.width - _bar.sizeDelta.x;
		_bar.sizeDelta = new Vector2(
			(percentage-1) * barTotalWidth,
			0);
	}
	
}
