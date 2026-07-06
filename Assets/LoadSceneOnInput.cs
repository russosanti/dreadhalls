using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class LoadSceneOnInput : MonoBehaviour, IPointerClickHandler {

	void Update () {
		if (Keyboard.current != null &&
		    (Keyboard.current.enterKey.wasPressedThisFrame ||
		     Keyboard.current.numpadEnterKey.wasPressedThisFrame)) {
			LoadPlay();
		}
	}

	public void OnPointerClick(PointerEventData eventData) {
		LoadPlay();
	}

	public void LoadPlay() {
		SceneManager.LoadScene("Play");
	}
}
