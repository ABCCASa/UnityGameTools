using UnityEngine;

namespace GameTools.DataBindSystem
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioSourceVolumeBinding: BindingTarget<AudioSource, RangedInt>
    {
        protected override void OnBind() { }
        protected override void OnUnbind() { }
        protected override void OnSourceChange(RangedInt value)
        {
            component.volume = Mathf.InverseLerp(value.min, value.max, value);
        }
    }
}