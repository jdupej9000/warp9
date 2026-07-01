using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Warp9.Model;

namespace Warp9.Avalonia;

public partial class TextEditorPage : ContentPage
{
    public TextEditorPage()
    {
        InitializeComponent();
    }

    public void AttachProject(Project p)
    {
        DataContext = p;
    }
}