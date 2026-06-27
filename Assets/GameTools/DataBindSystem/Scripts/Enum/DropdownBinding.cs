using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameTools.DataBindSystem
{
    [RequireComponent(typeof(TMP_Dropdown))]
    public class DropdownBinding : BindingTarget<TMP_Dropdown, Enum>
    {
        private List<Enum> allValues;
        private Type enumType;

        protected override void OnBind()
        {
            component.onValueChanged.AddListener(GetUIInput);
        }

        protected override void OnUnbind()
        {
            enumType = null;
            allValues = null;
            component.onValueChanged.RemoveListener(GetUIInput);
        }

        private void GetUIInput(int value)
        {
            if (enumType == null) { throw new ArgumentNullException("无法通过null的Enum获取Enum的类型"); }
            sourceValue = allValues[value];
        }
        
        protected override void OnSourceChange(Enum value)
        {
            if (value == null) { throw new ArgumentNullException("无法通过null的Enum获取Enum的类型"); }
            Type newType = value.GetType();
            if (enumType != newType) // 这里包括了初始化时的判断类别以及更新dropdown的展示
            {
                enumType = newType;
                component.ClearOptions();
                allValues = Enum.GetValues(enumType).Cast<Enum>().ToList();
                List<string> options = new();
                foreach (var item in allValues)
                {
                    options.Add(item.ToString());
                }
                component.AddOptions(options);
            }
            component.SetValueWithoutNotify(allValues.IndexOf(value));
            component.RefreshShownValue();
        }
    }
}