using UnityEngine;
using UnityEngine.UI;

namespace GameTools.DataBindSystem
{
    [RequireComponent(typeof(Slider))]
    public class FloatSliderBinding : BindingTarget<Slider, RangedFloat>
    {
        private void GetUIInput(float value)
        {
            RangedFloat rangedFloat = sourceValue;
            rangedFloat.Value = value;
            sourceValue = rangedFloat;
        }
        protected override void OnSourceChange(RangedFloat value)
        {
            if (component.maxValue != value.max || component.minValue != value.min)
            {
                component.minValue = float.MinValue;
                component.maxValue = float.MaxValue;
                component.SetValueWithoutNotify(value);
                component.maxValue = value.max;
                component.minValue = value.min;
            }
            else component.SetValueWithoutNotify(value);
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