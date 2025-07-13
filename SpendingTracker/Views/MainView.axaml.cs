using System;
using System.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SpendingTracker.Models;

namespace SpendingTracker.Views;

public partial class MainView : UserControl, INotifyPropertyChanged
{
    private readonly string _dataFilePath;

    // --- Backing fields for properties ---
    private string _newReceiptName = string.Empty;
    private string _newItemName = string.Empty;
    private decimal _newItemCost;
    private decimal _monthlyTotal;

    // --- Public Properties for UI Binding ---
    public ObservableCollection<Receipt> Receipts { get; set; } = new();
    public ObservableCollection<Item> CurrentItems { get; set; } = new();
    
    public string NewReceiptName { get => _newReceiptName; set => SetField(ref _newReceiptName, value); }
    public string NewItemName { get => _newItemName; set => SetField(ref _newItemName, value); }
    public decimal NewItemCost { get => _newItemCost; set => SetField(ref _newItemCost, value); }
    public decimal MonthlyTotal { get => _monthlyTotal; set => SetField(ref _monthlyTotal, value); }

    // --- Constructor ---
    public MainView()
    {
        InitializeComponent();
        this.DataContext = this; // Set DataContext to this instance for bindings

        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "SpendingTracker");
        Directory.CreateDirectory(appFolder);
        _dataFilePath = Path.Combine(appFolder, "receipts.json");

        LoadData();
    }

    // --- Button Click Event Handlers ---
    private void AddItemButton_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NewItemName) || NewItemCost <= 0) return;
        CurrentItems.Add(new Item { Name = NewItemName, Cost = NewItemCost });
        NewItemName = string.Empty;
        NewItemCost = 0;
    }

    private async void AddReceiptButton_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NewReceiptName) || !CurrentItems.Any()) return;
        var newReceipt = new Receipt
        {
            Name = NewReceiptName,
            Items = new List<Item>(CurrentItems),
            DateAdded = DateTime.UtcNow
        };
        Receipts.Add(newReceipt);
        NewReceiptName = string.Empty;
        CurrentItems.Clear();
        UpdateMonthlyTotal();
        await SaveDataAsync();
    }

    private async void EditReceiptButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { CommandParameter: Receipt receiptToEdit }) return;

        // Load the receipt data back into the form
        NewReceiptName = receiptToEdit.Name;
        CurrentItems.Clear();
        foreach (var item in receiptToEdit.Items)
        {
            CurrentItems.Add(item);
        }

        // Remove the old receipt. The user will save it again after making changes.
        Receipts.Remove(receiptToEdit);
        UpdateMonthlyTotal();
        await SaveDataAsync();
    }

    private async void DeleteReceiptButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { CommandParameter: Receipt receiptToDelete }) return;

        Receipts.Remove(receiptToDelete);
        UpdateMonthlyTotal();
        await SaveDataAsync();
    }

    private async void ClearAllDataButton_Click(object? sender, RoutedEventArgs e)
    {
        Receipts.Clear();
        CurrentItems.Clear();
        UpdateMonthlyTotal();
        if (File.Exists(_dataFilePath))
        {
            File.Delete(_dataFilePath);
        }
        await Task.CompletedTask;
    }

    // --- Data and UI Logic ---
    private void UpdateMonthlyTotal()
    {
        MonthlyTotal = Receipts
            .Where(r => r.DateAdded.Month == DateTime.UtcNow.Month && r.DateAdded.Year == DateTime.UtcNow.Year)
            .Sum(r => r.TotalCost);
    }
    
    private async Task SaveDataAsync()
    {
        var json = JsonSerializer.Serialize(Receipts, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_dataFilePath, json);
    }

    private void LoadData()
    {
        if (!File.Exists(_dataFilePath)) return;
        try
        {
            var json = File.ReadAllText(_dataFilePath);
            var loadedReceipts = JsonSerializer.Deserialize<List<Receipt>>(json);
            if (loadedReceipts != null)
            {
                // Clear the existing collection and add the loaded items.
                // This preserves the UI binding.
                Receipts.Clear();
                foreach (var receipt in loadedReceipts)
                {
                    Receipts.Add(receipt);
                }
                UpdateMonthlyTotal();
            }
        }
        catch { Receipts.Clear(); } // Start fresh if file is broken
    }
    
    // --- INotifyPropertyChanged Implementation for UI updates ---
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
