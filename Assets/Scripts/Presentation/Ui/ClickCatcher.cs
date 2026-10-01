using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HellPoker.Presentation.Ui
{
    /// <summary>Calls back when a graphic that is not a button (a backdrop) is clicked.</summary>
    public sealed class ClickCatcher : MonoBehaviour, IPointerClickHandler
    {
        private Action _clicked;

        public static ClickCatcher Attach(GameObject target, Action clicked)
        {
            var catcher = target.AddComponent<ClickCatcher>();
            catcher._clicked = clicked;
            return catcher;
        }

        public void OnPointerClick(PointerEventData eventData) => _clicked?.Invoke();
    }
}
