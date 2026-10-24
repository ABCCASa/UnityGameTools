using UnityEngine;
using UnityEngine.UIElements;

namespace GameTools.UISystem
{
    public abstract class UIToolkitScreenBase: ScreenBase
    {
        [SerializeField] private UIDocument document;
        protected VisualElement root => document.rootVisualElement;
        protected override void OnAnimation(string animKey, float progress)
        {
            document.rootVisualElement.style.opacity = 0.5f;
        }
        private protected override void SetInteractable(bool value)
        {
            document.rootVisualElement.SetEnabled(value);
        }
        private protected override void SetOrder(int order)
        {
            document.sortingOrder = order;
        }
    }
}