using Microsoft.UI.Xaml.Controls;
using FufuLauncher.ViewModels;

namespace FufuLauncher.Views
{
    public sealed partial class AgreementPage : Page
    {
        public AgreementViewModel ViewModel { get; }

        public AgreementPage()
        {
            ViewModel = App.GetService<AgreementViewModel>();
            InitializeComponent();
        }
    }
}