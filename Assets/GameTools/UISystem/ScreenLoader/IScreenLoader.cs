namespace GameTools.UISystem
{
    internal interface IScreenLoader
    {
        public T GetScreen<T>() where T : ScreenBase;
        public void ReleaseScreen(ScreenBase screen);
    }
}