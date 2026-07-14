namespace GameTools.UISystem
{
    /// <summary> 控制层级关系 </summary>
    public interface IContainerItem
    {
        /// <summary> 设置item是否可交互 （不影响射线遮挡） </summary>
        internal void SetInteractable(ref bool value);
        internal void SetOrder(ref int order);
    }
}