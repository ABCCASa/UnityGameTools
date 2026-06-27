
using UnityEngine;
using TMPro;

namespace GameTools.DataBindSystem
{

    [RequireComponent(typeof(TMP_InputField))]
    public class FloatInputFieldBinding : BindingTarget<TMP_InputField, RangedFloat>
    {
        private void GetUIInput(string value)
        {
            if (float.TryParse(value, out float result))
            {
                RangedFloat rangedFloat = sourceValue;
                rangedFloat.Value = result;
                sourceValue = rangedFloat;
            }
            else 
            {
                OnSourceChange(sourceValue);
            }
        }

        protected override void OnSourceChange(RangedFloat value)
        {
            component.SetTextWithoutNotify(value.Value.ToString());
        }

        protected override void OnBind()
        {
            component.onEndEdit.AddListener(GetUIInput);
            component.contentType = TMP_InputField.ContentType.DecimalNumber;
        }

        protected override void OnUnbind()
        {
            component.onEndEdit.RemoveListener(GetUIInput);
        }
    }
}