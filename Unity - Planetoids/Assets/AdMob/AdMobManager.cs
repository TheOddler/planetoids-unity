using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AdMobPlugin))]
public class AdMobManager : MonoBehaviour {
	
	private const string AD_UNIT_ID = "ca-app-pub-0565056629754288/2906286258";
	
	private AdMobPlugin admob;
	public float _retryDelay = 10.0f;

	void Start() {
		admob = GetComponent<AdMobPlugin>();
		admob.CreateBanner(
			adUnitId: AD_UNIT_ID,
			adSize: AdMobPlugin.AdSize.SMART_BANNER,
			isTopPosition: true,
			
			isTestDevice: false);
		
		admob.RequestAd();
		
		AdMobPlugin.AdLoaded += HandleAdLoaded;
		AdMobPlugin.AdFailedToLoad += OnAdFailedToLoad;
	}
	
	void OnAdFailedToLoad() {
	#if !UNITY_EDITOR
		StartCoroutine(DelayedRetryRequestAd());
	#endif
	}
	IEnumerator DelayedRetryRequestAd() {
		yield return new WaitForSeconds(_retryDelay);
	#if !UNITY_EDITOR
		admob.RequestAd();
	#endif
	}

	void HandleAdLoaded() {
	#if !UNITY_EDITOR
		admob.ShowBanner();
	#endif
	}
}
