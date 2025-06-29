using Markdig;
using Markdig.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xaml;
using Warp9.ProjectExplorer;
using XamlReader = System.Windows.Markup.XamlReader;

namespace Warp9.Navigation
{
    /// <summary>
    /// Interaction logic for MdViewPage.xaml
    /// </summary>
    public partial class MdViewPage : Page, IWarp9View
    {
        public MdViewPage()
        {
            InitializeComponent();
        }

        Warp9ViewModel? viewModel;

        public void AttachViewModel(Warp9ViewModel vm)
        {
            viewModel = vm;
        }

        public void DetachViewModel()
        {
            viewModel = null;
        }

        public void RenderMarkdown(string md)
        {
            string? xaml = Markdig.Wpf.Markdown.ToXaml(md, BuildPipeline());
            using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(xaml));
            using XamlXmlReader reader = new XamlXmlReader(stream, new MyXamlSchemaContext());
                
            if (XamlReader.Load(reader) is FlowDocument document)
                Viewer.Document = document;            
        }

        private void OpenHyperlink(object sender, ExecutedRoutedEventArgs e)
        {
            Process.Start(e.Parameter.ToString());
        }

        private static MarkdownPipeline BuildPipeline()
        {
            return new MarkdownPipelineBuilder()
                .UseSupportedExtensions()
                .Build();
        }
    }

    class MyXamlSchemaContext : XamlSchemaContext
    {
        public override bool TryGetCompatibleXamlNamespace(string xamlNamespace, out string compatibleNamespace)
        {
            if (xamlNamespace.Equals("clr-namespace:Markdig.Wpf", StringComparison.Ordinal))
            {
                compatibleNamespace = $"clr-namespace:Markdig.Wpf;assembly={Assembly.GetAssembly(typeof(Markdig.Wpf.Styles)).FullName}";
                return true;
            }
            return base.TryGetCompatibleXamlNamespace(xamlNamespace, out compatibleNamespace);
        }
    }
}
