using System;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Apocalypse
{
    public sealed class BattleTargetPointer : MonoBehaviour,IPointerClickHandler
    {
        public Action selected;
        public void OnPointerClick(PointerEventData data){if(data.button==PointerEventData.InputButton.Left)selected?.Invoke();}
    }
}

