using System.Collections;
using GameTools.UISystem;
using UnityEngine;

namespace GameTools.SceneSystem
{
    public class DarkTransitionScreen: UGUIScreen
    {
        protected override bool blockInput => true;
        protected override void OnAnimation(string animKey, float progress)
        {
          alpha = progress;
        }
    }
}