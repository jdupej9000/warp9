using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Warp9.Avalonia.Navigation;

namespace Warp9.Avalonia;

public partial class ViewerPage : ContentPage, IWarp9View
{
    public ViewerPage()
    {
        InitializeComponent();
    }

    public void AttachViewModel(Warp9ProjectModel vm)
    {
    }

    public void DetachViewModel()
    {
    }
}