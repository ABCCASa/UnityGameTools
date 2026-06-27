
using UnityEngine;
using TMPro;

namespace GameTools.DataBindSystem
{
    [RequireComponent(typeof(TMP_InputField))]
    public class StringInputFieldBinding : BindingTarget<TMP_InputField, string>
    {
        private void GetUIInput(string value)
        {
            sourceValue = value;
        }

        protected override void OnSourceChange(string value)
        {
            component.SetTextWithoutNotify(value);
        }

        protected override void OnBind()
        {
            component.onEndEdit.AddListener(GetUIInput);
        }

        protected override void OnUnbind()
        {
            component.onEndEdit.RemoveListener(GetUIInput);
        }
    }
}