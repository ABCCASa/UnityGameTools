
using UnityEngine;
using TMPro;

namespace GameTools.DataBindSystem
{

    [RequireComponent(typeof(TMP_InputField))]
    public class IntInputFieldBinding : BindingTarget<TMP_InputField, RangedInt>
    {
        private void GetUIInput(string value)
        {
            if (int.TryParse(value, out int result))
            {
                RangedInt rangedInt = sourceValue;
                rangedInt.Value = result;
                sourceValue = rangedInt;
            }
            else
            {
                OnSourceChange(sourceValue);
            }
        }

        protected override void OnSourceChange(RangedInt value)
        {
            component.SetTextWithoutNotify(value.Value.ToString());
        }

        protected override void OnBind()
        {
            component.onEndEdit.AddListener(GetUIInput);
            component.contentType = TMP_InputField.ContentType.IntegerNumber;
        }

        protected override void OnUnbind()
        {
            component.onEndEdit.RemoveListener(GetUIInput);
        }
    }
}