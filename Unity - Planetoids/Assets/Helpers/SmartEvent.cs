﻿using System;
using UnityEngine;
using System.Collections;

public class SmartEvent {
	
	MonoBehaviour _owner;
	bool _beingCalled;
	event Action _event;
	
	public SmartEvent(MonoBehaviour owner) {
		_owner = owner;
		_event += () => _beingCalled = false;
	}
	
	public void SetSubscription(bool sub, Action action) {
		if (sub) {
			_event += action;
		}
		else {
			_event -= action;
		}
	}
	
	public void CallOnceAtEndOfFrame() {
		if (!_beingCalled) {
			_beingCalled = true;
			_owner.StartCoroutine(CallEventAtEndOfFrame());
		}
	}
	
	IEnumerator CallEventAtEndOfFrame() {
		yield return new WaitForEndOfFrame();
		_event();
	}
	
}
