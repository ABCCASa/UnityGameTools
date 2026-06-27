using UnityEngine;
using UnityEngine.UI;

namespace GameTools.DataBindSystem
{
    [RequireComponent(typeof(Slider))]
    public class IntSliderBinding : BindingTarget<Slider, RangedInt>
    {
        private void GetUIInput(float value)
        {
            RangedInt rangedInt = sourceValue;
            rangedInt.Value = Mathf.RoundToInt(value);
            sourceValue = rangedInt;
        }
        protected override void OnSourceChange(RangedInt value)
        {
            if (component.maxValue != value.max || component.minValue != value.min)
            {
                component.minValue = float.MinValue;
                component.maxValue = float.MaxValue;
                component.SetValueWithoutNotify(value);
                component.maxValue = value.max;
                component.minValue = value.min;
            }
            else
            {
                component.SetValueWithoutNotify(value);
            }
        }

        protected override void OnBind()
        {
            component.wholeNumbers = true;
            component.onValueChanged.AddListener(GetUIInput);
        }

        protected override void OnUnbind()
        {
            component.onValueChanged.RemoveListener(GetUIInput);
        }
    }
}