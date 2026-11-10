using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Warp9.Avalonia.Navigation;

namespace Warp9.Avalonia;

public partial class SummaryPage : ContentPage, IWarp9View
{
    public SummaryPage()
    {
        InitializeComponent();
    }

    public void SetSummaryText(string txt)
    {
        txtSummary.Text = txt;
    }

    public void AttachViewModel(Warp9ProjectModel vm)
    {
    }

    public void DetachViewModel()
    {
    }
}