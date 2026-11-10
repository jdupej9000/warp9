using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Warp9.Avalonia.Navigation;
using Warp9.Model;

namespace Warp9.Avalonia;

public partial class TextEditorPage : ContentPage, IWarp9View
{
    public TextEditorPage()
    {
        InitializeComponent();
    }

    public void AttachViewModel(Warp9ProjectModel vm)
    {
        DataContext = vm.Project;
     
    }

    public void DetachViewModel()
    {        
    }
}