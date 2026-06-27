using UnityEngine.UI;
using UnityEngine;

namespace GameTools.DataBindSystem
{
    [RequireComponent(typeof(Toggle))]
    public class ToggleBinding : BindingTarget<Toggle, bool>
    {
        protected override void OnSourceChange(bool value)
        {
            component.SetIsOnWithoutNotify(value);
        }
        private void GetUIInput(bool value)
        {
            sourceValue = value;
        }
        protected override void OnBind()
        {
            component.onValueChanged.AddListener(GetUIInput);
        }
        protected override void OnUnbind()
        {
            component.onValueChanged.RemoveListener(GetUIInput);
        }
    }
}