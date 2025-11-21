using Avalonia.Controls;
using WSMDeployer.ViewModels;

namespace WSMDeployer.Views
{
    public partial class TargetDialog : Window
    {
        public TargetDialog()
        {
            InitializeComponent();
        }

        public TargetDialog(TargetDialogViewModel viewModel) : this()
        {
            DataContext = viewModel;

            // Handle events
            viewModel.Saved += () => Close(true);
            viewModel.Cancelled += () => Close(false);
        }
    }
}
