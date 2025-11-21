using Avalonia;
using Avalonia.Controls;
using WSMDeployer.ViewModels;

namespace WSMDeployer.Views
{
    public partial class DeploymentDialog : Window
    {
        public DeploymentDialog(DeploymentDialogViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            // Handle dialog close events
            viewModel.DeploymentStarted += () =>
            {
                // Keep dialog open to show deployment started message
            };

            viewModel.Cancelled += () =>
            {
                Close();
            };
        }
    }
}
