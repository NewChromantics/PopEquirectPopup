using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


[System.Serializable]
public class UnityEvent_Vector2 : UnityEngine.Events.UnityEvent<Vector2> { }


public class RawImageOnClick : MonoBehaviour, IPointerClickHandler
{
	public UnityEvent_Vector2 OnLeftClickScreen;
	public UnityEvent_Vector2 OnMiddleClickScreen;
	public UnityEvent_Vector2 OnRightClickScreen;

	UnityEvent_Vector2 GetEventForButton(PointerEventData.InputButton Button)
	{
		switch ( Button )
		{
			case PointerEventData.InputButton.Left: return OnLeftClickScreen;
			case PointerEventData.InputButton.Middle: return OnMiddleClickScreen;
			case PointerEventData.InputButton.Right: return OnRightClickScreen;
			default: throw new System.Exception("Unhandled button " + Button);
		}
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		var Event = GetEventForButton(eventData.button);
		//	gr: is this screen pos, or rawimage local pos?
		var ScreenPos = eventData.position;	
		Event.Invoke(ScreenPos);
	}
}
