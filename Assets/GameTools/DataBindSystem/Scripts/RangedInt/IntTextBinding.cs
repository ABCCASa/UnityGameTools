using GameTools.DataBindSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
namespace GameTools.DataBindSystem
{
    [RequireComponent(typeof(TMP_Text))]
    public class IntTextBinding : BindingTarget<TMP_Text, RangedInt>
    {
        [SerializeField] private string _format;
        public string format
        {
            get => _format;
            set
            {
                _format = value;
                if (isBind)
                {
                    OnSourceChange(sourceValue);
                }
            }
        }
        protected override void OnBind() { }
        protected override void OnUnbind() { }
        protected override void OnSourceChange(RangedInt value)
        {
            int intValue = value;
            component.text = intValue.ToString(format);
        }
    }
}