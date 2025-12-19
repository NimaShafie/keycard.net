// Modules/Folio/Views/FolioView.axaml.cs
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using KeyCard.Desktop.Modules.Folio.ViewModels;
using KeyCard.Desktop.Modules.Folio.Models;

namespace KeyCard.Desktop.Modules.Folio.Views
{
    public partial class FolioView : UserControl
    {
        public FolioView()
        {
            InitializeComponent();

            if (Design.IsDesignMode)
                return;

            // Set up DataContext and wire events
            DataContextChanged += OnDataContextChanged;

            // Wire up button clicks manually after DataGrid is loaded
            this.Loaded += OnViewLoaded;
        }

        private void OnViewLoaded(object? sender, RoutedEventArgs e)
        {
            SetStatus("🔧 View loaded, wiring buttons...");

            // Find the DataGrid
            var dataGrid = this.FindControl<DataGrid>("FolioDataGrid");
            if (dataGrid != null)
            {
                SetStatus("✅ Found DataGrid, subscribing to rows");
                dataGrid.LoadingRow += OnDataGridLoadingRow;
            }
            else
            {
                SetStatus("❌ ERROR: Could not find DataGrid!");
            }
        }

        private void OnDataGridLoadingRow(object? sender, DataGridRowEventArgs e)
        {
            // Find the Details button in this row and wire it manually
            var row = e.Row;
            if (row.DataContext is GuestFolio folio)
            {
                // Use Dispatcher to ensure UI is ready
                Dispatcher.UIThread.Post(() =>
                {
                    // Find button by walking the visual tree
                    var button = FindButtonInRow(row);
                    if (button != null)
                    {
                        button.Tag = folio.FolioId;
                        button.Click -= OnDetailsButtonClickManual; // Unsubscribe first
                        button.Click += OnDetailsButtonClickManual; // Wire manually
                        SetStatus($"🔗 Wired button for {folio.GuestName} ({folio.FolioId})");
                    }
                }, DispatcherPriority.Loaded);
            }
        }

        private Button? FindButtonInRow(Control control)
        {
            // Recursively find the Details button
            if (control is Button button && button.Content?.ToString() == "Details")
                return button;

            if (control is Panel panel)
            {
                foreach (var child in panel.Children)
                {
                    if (child is Control childControl)
                    {
                        var found = FindButtonInRow(childControl);
                        if (found != null) return found;
                    }
                }
            }

            return null;
        }

        private void OnDetailsButtonClickManual(object? sender, RoutedEventArgs e)
        {
            SetStatus("🎯 BUTTON CLICKED!");

            if (DataContext is FolioViewModel vm)
            {
                SetStatus("✅ Got ViewModel");

                if (sender is Button button && button.Tag is string folioId)
                {
                    SetStatus($"📂 Got FolioId: {folioId}");

                    if (vm.OpenGuestDetailCommand.CanExecute(folioId))
                    {
                        SetStatus($"⚡ Executing command for {folioId}...");
                        vm.OpenGuestDetailCommand.Execute(folioId);
                        SetStatus($"✅ Command executed!");
                    }
                    else
                    {
                        SetStatus($"❌ Command CanExecute=false (Busy: {vm.IsBusy})");
                    }
                }
                else
                {
                    SetStatus($"❌ Button={sender != null}, Tag={(sender as Button)?.Tag}");
                }
            }
            else
            {
                SetStatus("❌ DataContext is not FolioViewModel!");
            }
        }

        private void SetStatus(string message)
        {
            if (DataContext is FolioViewModel vm)
            {
                vm.StatusMessage = message;
            }
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            // Wire up the OpenGuestDetailRequested event when DataContext changes
            if (DataContext is FolioViewModel vm)
            {
                Console.WriteLine("[FolioView] Wiring up OpenGuestDetailRequested event");

                // Unsubscribe first to prevent double subscription
                vm.OpenGuestDetailRequested -= OnOpenGuestDetailRequested;
                // Subscribe to the event
                vm.OpenGuestDetailRequested += OnOpenGuestDetailRequested;

                Console.WriteLine("[FolioView] Event wired successfully");

                // Initialize the view model
                Dispatcher.UIThread.Post(async () =>
                {
                    try
                    {
                        await vm.InitializeAsync(null);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[FolioView] Error initializing FolioViewModel: {ex.Message}");
                    }
                });
            }
            else
            {
                Console.WriteLine($"[FolioView] DataContext is not FolioViewModel, it is: {DataContext?.GetType().Name ?? "null"}");
            }
        }

        /// <summary>
        /// Click event handler for Details button in DataGrid
        /// </summary>
        public void OnDetailsButtonClick(object? sender, RoutedEventArgs e)
        {
            Console.WriteLine("[FolioView] OnDetailsButtonClick called");

            // IMMEDIATELY set status to prove button was clicked
            if (DataContext is FolioViewModel vmTest)
            {
                vmTest.StatusMessage = "⚠️ BUTTON CLICKED! ⚠️";
            }

            if (sender is Button button && button.Tag is string folioId)
            {
                Console.WriteLine($"[FolioView] Details button clicked for folio: {folioId}");

                if (DataContext is FolioViewModel vm)
                {
                    Console.WriteLine($"[FolioView] Executing OpenGuestDetailCommand");

                    // Execute the command directly
                    if (vm.OpenGuestDetailCommand.CanExecute(folioId))
                    {
                        Console.WriteLine($"[FolioView] Command can execute, executing now...");
                        vm.OpenGuestDetailCommand.Execute(folioId);
                    }
                    else
                    {
                        Console.WriteLine($"[FolioView] Command CanExecute returned false - IsBusy: {vm.IsBusy}");
                        vm.StatusMessage = $"❌ Cannot execute - IsBusy: {vm.IsBusy}";
                    }
                }
                else
                {
                    Console.WriteLine($"[FolioView] DataContext is not FolioViewModel");
                }
            }
            else
            {
                Console.WriteLine($"[FolioView] Button or Tag is invalid. Sender type: {sender?.GetType().Name}, Tag: {(sender as Button)?.Tag}");
                if (DataContext is FolioViewModel vm2)
                {
                    vm2.StatusMessage = "❌ Button or Tag is NULL!";
                }
            }
        }

        private async void OnOpenGuestDetailRequested(object? sender, string folioId)
        {
            try
            {
                SetStatus($"🔔 Event fired for {folioId}");

                var owner = this.VisualRoot as Window;

                if (owner == null)
                {
                    SetStatus("❌ No owner window found!");
                    return;
                }

                SetStatus("🏗️ Creating detail window...");

                // Create the detail window
                var detailWindow = new GuestDetailWindow();

                // Get IFolioService from parent ViewModel using reflection
                GuestDetailViewModel? detailVm = null;

                if (DataContext is FolioViewModel parentVm)
                {
                    SetStatus("🔍 Getting IFolioService...");

                    var folioServiceField = parentVm.GetType()
                        .GetField("_folio", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (folioServiceField != null)
                    {
                        var folioService = folioServiceField.GetValue(parentVm) as KeyCard.Desktop.Modules.Folio.Services.IFolioService;
                        if (folioService != null)
                        {
                            detailVm = new GuestDetailViewModel(folioService);
                            SetStatus("✅ Created GuestDetailViewModel");
                        }
                        else
                        {
                            SetStatus("❌ folioService is null!");
                            return;
                        }
                    }
                    else
                    {
                        SetStatus("❌ Could not find _folio field!");
                        return;
                    }
                }
                else
                {
                    SetStatus("❌ DataContext wrong type!");
                    return;
                }

                if (detailVm == null)
                {
                    SetStatus("❌ Failed to create ViewModel!");
                    return;
                }

                // Load the folio data
                try
                {
                    SetStatus($"📥 Loading data for {folioId}...");
                    await detailVm.LoadAsync(folioId);
                    SetStatus($"✅ Data loaded");
                }
                catch (Exception ex)
                {
                    SetStatus($"❌ Load error: {ex.Message}");
                    return;
                }

                // Set the DataContext
                detailWindow.DataContext = detailVm;

                // Subscribe to data changes and refresh main view immediately
                detailVm.DataChanged += async (s, e) =>
                {
                    SetStatus("🔄 Data changed, refreshing main view...");
                    if (DataContext is FolioViewModel vm)
                    {
                        if (vm.RefreshCommand.CanExecute(null))
                        {
                            vm.RefreshCommand.Execute(null);
                        }
                    }
                };

                SetStatus("🪟 Showing window...");

                // Show the window and wait for it to close
                await detailWindow.ShowDialog(owner);

                if (DataContext is FolioViewModel vm)
                {
                    SetStatus("🔄 Refreshing folio list...");

                    // Call the RefreshCommand to reload all folios with updated balances
                    if (vm.RefreshCommand.CanExecute(null))
                    {
                        vm.RefreshCommand.Execute(null);
                    }
                }

                SetStatus("✅ Window closed, data refreshed");
            }
            catch (Exception ex)
            {
                SetStatus($"❌ Error: {ex.Message}");
            }
        }
    }
}
