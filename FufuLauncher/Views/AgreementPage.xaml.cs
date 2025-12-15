using FufuLauncher.ViewModels;

namespace FufuLauncher.Views
{
    public sealed partial class AgreementPage
    {
        public AgreementViewModel ViewModel { get; }

        public AgreementPage()
        {
            ViewModel = App.GetService<AgreementViewModel>();
            InitializeComponent();
        }
    }
}