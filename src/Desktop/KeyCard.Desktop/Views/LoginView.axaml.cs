// Views/LoginView.axaml.cs
using System;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Threading;

using KeyCard.Desktop.ViewModels;

namespace KeyCard.Desktop.Views;

public partial class LoginView : UserControl, IDisposable
{
    private LoginViewModel? _viewModel;
    private CancellationTokenSource? _cts;
    private bool _initialized;
    private bool _disposed;

    public LoginView()
    {
        InitializeComponent();
        System.Diagnostics.Debug.WriteLine("=== LoginView constructor called ===");

        this.DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_initialized || DataContext is not LoginViewModel vm)
            return;

        _initialized = true;

        // Unsubscribe from old view model if any
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _viewModel = vm;

        // Subscribe to property changes
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        System.Diagnostics.Debug.WriteLine($"LoginView: IsMockMode = {vm.IsMockMode}");

        // In Mock mode, set BackendReady immediately
        if (vm.IsMockMode)
        {
            System.Diagnostics.Debug.WriteLine("LoginView: Mock mode - setting BackendReady = true");
            SetBackendReady(true);
            return;
        }

        // In Live mode, start backend check
        System.Diagnostics.Debug.WriteLine("LoginView: Live mode - starting backend check");
        _ = StartBackendCheckAsync();
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LoginViewModel.Error))
        {
            UpdateErrorDisplay();
        }
    }

    private void UpdateErrorDisplay()
    {
        var errorPanel = this.FindControl<Border>("ErrorPanel");
        var errorText = this.FindControl<TextBlock>("ErrorText");

        if (errorPanel != null && errorText != null && _viewModel != null)
        {
            var hasError = !string.IsNullOrWhiteSpace(_viewModel.Error);
            errorPanel.IsVisible = hasError;
            errorText.Text = _viewModel.Error ?? string.Empty;

            System.Diagnostics.Debug.WriteLine($"Error display updated: {hasError}");
        }
    }

    private async Task StartBackendCheckAsync()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        var maxAttempts = 100; // 30 seconds (300ms * 100)
        var attempt = 0;

        try
        {
            while (attempt < maxAttempts && !_cts.Token.IsCancellationRequested)
            {
                attempt++;

                // Check backend
                var alive = await PingBackendAsync();

                if (alive)
                {
                    System.Diagnostics.Debug.WriteLine($"Backend is alive! (attempt {attempt})");
                    SetBackendReady(true);
                    return;
                }

                // Wait 300ms before next attempt
                await Task.Delay(300, _cts.Token);
            }

            // Max attempts reached - backend not available
            System.Diagnostics.Debug.WriteLine("Backend check failed - max attempts reached");
            SetBackendReady(false);
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine("Backend check cancelled");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Backend check error: {ex.Message}");
            SetBackendReady(false);
        }
    }

    private static async Task<bool> PingBackendAsync()
    {
        try
        {
            using var client = new System.Net.Http.HttpClient
            {
                Timeout = TimeSpan.FromMilliseconds(500)
            };

            // Get backend URL - try localhost:7224 first (from your screenshot)
            var urls = new[]
            {
                "https://localhost:7224",
                "http://localhost:5149",
                "https://localhost:5001",
                "http://localhost:5000"
            };

            foreach (var baseUrl in urls)
            {
                try
                {
                    client.BaseAddress = new Uri(baseUrl);

                    var endpoints = new[] { "/api/v1/Health", "/health", "/api/health", "/" };

                    foreach (var endpoint in endpoints)
                    {
                        try
                        {
                            var response = await client.GetAsync(endpoint);
                            if (response.IsSuccessStatusCode)
                            {
                                System.Diagnostics.Debug.WriteLine($"Backend alive at {baseUrl}{endpoint}");
                                return true;
                            }
                        }
                        catch
                        {
                            // Try next endpoint
                        }
                    }
                }
                catch
                {
                    // Try next URL
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private void SetBackendReady(bool isReady)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_viewModel != null)
            {
                // Set BackendReady property using reflection
                var prop = _viewModel.GetType().GetProperty("BackendReady");
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(_viewModel, isReady);
                    System.Diagnostics.Debug.WriteLine($"BackendReady set to: {isReady}");

                    // Also set IsCheckingBackend to false
                    var checkingProp = _viewModel.GetType().GetProperty("IsCheckingBackend");
                    if (checkingProp != null && checkingProp.CanWrite)
                    {
                        checkingProp.SetValue(_viewModel, false);
                        System.Diagnostics.Debug.WriteLine($"IsCheckingBackend set to: false");
                    }

                    // Trigger CanExecuteChanged to update button state
                    TriggerCommandUpdate();
                }
            }
        });
    }

    private void TriggerCommandUpdate()
    {
        if (_viewModel?.LoginCommand == null) return;

        try
        {
            // Try to trigger RaiseCanExecuteChanged using reflection
            var commandType = _viewModel.LoginCommand.GetType();
            var raiseMethod = commandType.GetMethod("RaiseCanExecuteChanged");
            raiseMethod?.Invoke(_viewModel.LoginCommand, null);
            System.Diagnostics.Debug.WriteLine("LoginCommand CanExecuteChanged triggered");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Could not trigger command update: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Unsubscribe from events
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        // Cancel and dispose CancellationTokenSource
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        GC.SuppressFinalize(this);
    }
}
