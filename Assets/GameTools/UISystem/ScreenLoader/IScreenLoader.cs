namespace GameTools.UISystem
{
    public interface IScreenLoader
    {
        public T GetScreen<T>() where T : ScreenBase;
        public void ReleaseScreen(ScreenBase screen);
        public void Dispose();
    }
}