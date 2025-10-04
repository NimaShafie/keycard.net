// Views/MainWindow.axaml.cs
using System;

using Avalonia;
using Avalonia.Controls;

using KeyCard.Desktop.ViewModels;

namespace KeyCard.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            Console.WriteLine("[MainWindow] InitializeComponent failed: " + ex);
            // Fallback so you still see a window:
            Content = new TextBlock { Text = "Failed to load MainWindow.axaml", Margin = new Thickness(20) };
        }
    }

    public MainWindow(MainViewModel vm) : this()
    {
        DataContext = vm;
    }
}
