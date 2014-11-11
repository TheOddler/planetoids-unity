using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AdMobPlugin))]
public class AdMobManager : MonoBehaviour {
	
	private const string AD_UNIT_ID = "ca-app-pub-0565056629754288/2906286258";

	private AdMobPlugin admob;

	void Start() {
		admob = GetComponent<AdMobPlugin>();
		admob.CreateBanner(
			adUnitId: AD_UNIT_ID,
			adSize: AdMobPlugin.AdSize.SMART_BANNER,
			isTopPosition: true,
			
			isTestDevice: false);
		
		admob.RequestAd();
	}

	void OnEnable() {
		AdMobPlugin.AdLoaded += HandleAdLoaded;
	}

	void OnDisable() {
		AdMobPlugin.AdLoaded -= HandleAdLoaded;
	}

	void HandleAdLoaded() {
#if !UNITY_EDITOR
		admob.ShowBanner();
#endif
	}
}
