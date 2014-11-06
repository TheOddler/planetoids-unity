using UnityEngine;
using System.Collections;

public abstract class AGameMode : MonoBehaviour {
	
	abstract public bool Running { get; }
	
	abstract public void StartGame();
	abstract public void EndGame();
	abstract public void CleanUpGame();
	
}
