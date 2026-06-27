
using UnityEngine;
using TMPro;

namespace GameTools.DataBindSystem
{
    [RequireComponent(typeof(TMP_Text))]
    public class StringTextBinding : BindingTarget<TMP_Text, string>
    {
        protected override void OnSourceChange(string value)
        {
            component.text = value;
        }

        protected override void OnBind()
        {
          
        }

        protected override void OnUnbind()
        {
        }
    }
}