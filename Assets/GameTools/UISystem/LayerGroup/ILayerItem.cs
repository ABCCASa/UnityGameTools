namespace GameTools.UISystem
{
    /// <summary> 控制层级关系 </summary>
    public interface ILayerItem
    {
        public bool blockInput { get; } // 是否要禁用下层Layer的交互
        internal void SetInteractable(bool value);
        internal void SetOrder(int order);
    }
}