namespace Fantasy
{
    public interface IScrollItem<in T>
    {
        void OnCreate(IFantasyScrollView scrollView);
        void OnShowOrRefresh(int idx, T data);
        void OnHide(int idx);
    }
}