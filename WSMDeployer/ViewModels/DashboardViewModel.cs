using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using WSMDeployer.Models;
using WSMDeployer.Services;

namespace WSMDeployer.ViewModels
{
    /// <summary>
    /// Dashboard ViewModel - displays overview metrics and recent activity
    /// </summary>
    public class DashboardViewModel : ViewModelBase
    {
        private readonly DatabaseService _db;
        private readonly Timer _refreshTimer;

        private int _totalTargets;
        private int _activeDeployments;
        private double _successRate;
        private int _healthyTargets;

        public DashboardViewModel(DatabaseService db)
        {
            _db = db;

            RecentDeployments = new ObservableCollection<Deployment>();

            // Refresh data immediately
            RefreshData(null);

            // Set up auto-refresh every 30 seconds
            _refreshTimer = new Timer(RefreshData, null,
                TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        // Metric properties
        public int TotalTargets
        {
            get => _totalTargets;
            set => SetProperty(ref _totalTargets, value);
        }

        public int ActiveDeployments
        {
            get => _activeDeployments;
            set => SetProperty(ref _activeDeployments, value);
        }

        public double SuccessRate
        {
            get => _successRate;
            set => SetProperty(ref _successRate, value);
        }

        public int HealthyTargets
        {
            get => _healthyTargets;
            set => SetProperty(ref _healthyTargets, value);
        }

        public ObservableCollection<Deployment> RecentDeployments { get; }

        /// <summary>
        /// Refresh dashboard data from database
        /// </summary>
        private void RefreshData(object? state)
        {
            try
            {
                // Get total targets
                TotalTargets = _db.GetAllTargets().Count;

                // Get active deployments
                ActiveDeployments = _db.GetDeploymentsByStatus(DeploymentStatus.InProgress).Count;

                // Calculate success rate (last 30 days)
                var thirtyDaysAgo = DateTime.Now.AddDays(-30);
                var recentDeployments = _db.GetDeploymentsForTarget(0)
                    .Where(d => d.CreatedDate >= thirtyDaysAgo)
                    .ToList();

                if (recentDeployments.Any())
                {
                    var successCount = recentDeployments
                        .Count(d => d.Status == DeploymentStatus.Success);
                    SuccessRate = Math.Round((double)successCount / recentDeployments.Count * 100, 1);
                }
                else
                {
                    SuccessRate = 0;
                }

                // Get healthy targets
                HealthyTargets = _db.GetAllTargets()
                    .Count(t => t.Status == TargetStatus.Online);

                // Get recent deployments
                var recent = _db.GetDeploymentsForTarget(0)
                    .OrderByDescending(d => d.CreatedDate)
                    .Take(10)
                    .ToList();

                // Update on UI thread
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    RecentDeployments.Clear();
                    foreach (var deployment in recent)
                    {
                        // Load target info
                        deployment.Target = _db.GetTargetById(deployment.TargetId);
                        RecentDeployments.Add(deployment);
                    }
                });
            }
            catch (Exception ex)
            {
                // Log error (in production, use proper logging)
                System.Diagnostics.Debug.WriteLine($"Error refreshing dashboard: {ex.Message}");
            }
        }
    }
}
