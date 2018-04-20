using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowUiError : MonoBehaviour {

	[Range(1, 30)]
	public float ErrorDisplayDuration = 5;
	public UnityEngine.RectTransform ErrorText;

	public static void ShowError(string Text)
	{
		var This = GameObject.FindObjectOfType<ShowUiError>();
		This.ShowTextError(Text);
	}

	public void ShowTextError(string Text)
	{
		ErrorText.gameObject.SetActive(true);
		var t = ErrorText.GetComponentInChildren<UnityEngine.UI.Text>();
		t.text = Text;
		StartCoroutine("DoHideError", ErrorDisplayDuration);
	}

	void OnEnable()
	{
		HideError();
	}

	void HideError()
	{
		ErrorText.gameObject.SetActive(false);
	}

	// every 2 seconds perform the print()
	IEnumerator DoHideError()
	{
		HideError();
		yield return null;
	}
}
