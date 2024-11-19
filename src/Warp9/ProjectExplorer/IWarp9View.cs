namespace Warp9.ProjectExplorer
{
    public interface IWarp9View
    {
        public void AttachViewModel(Warp9ViewModel vm);
        public void DetachViewModel();
    }
}
