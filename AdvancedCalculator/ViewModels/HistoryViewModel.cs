using System.Collections.ObjectModel;
using System.Collections.Specialized;
using BenScr.AdvancedCalculator.Core;
using BenScr.AdvancedCalculator.Models;

namespace BenScr.AdvancedCalculator.ViewModels;

public sealed class HistoryViewModel : ObservableObject
{
    public HistoryViewModel()
    {
        Entries.CollectionChanged += OnEntriesCollectionChanged;
    }

    public ObservableCollection<CalculationHistoryItem> Entries { get; } = [];

    public bool HasEntries => Entries.Count > 0;

    public bool IsEmpty => Entries.Count == 0;

    public void SetEntries(IEnumerable<CalculationHistoryItem> items)
    {
        Entries.Clear();

        foreach (var item in items.OrderByDescending(static item => item.Timestamp))
        {
            Entries.Add(item);
        }

        TrimToMaximum();
        RaiseEntryStateChanged();
    }

    public void Add(CalculationHistoryItem item)
    {
        Entries.Insert(0, item);
        TrimToMaximum();
        RaiseEntryStateChanged();
    }

    public void Remove(CalculationHistoryItem? item)
    {
        if (item is null)
        {
            return;
        }

        if (Entries.Remove(item))
        {
            RaiseEntryStateChanged();
        }
    }

    public void Clear()
    {
        Entries.Clear();
        RaiseEntryStateChanged();
    }

    public IReadOnlyList<CalculationHistoryItem> ToList() => Entries.ToList();

    private void TrimToMaximum()
    {
        while (Entries.Count > 100)
        {
            Entries.RemoveAt(Entries.Count - 1);
        }
    }

    private void OnEntriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RaiseEntryStateChanged();
    }

    private void RaiseEntryStateChanged()
    {
        OnPropertyChanged(nameof(HasEntries));
        OnPropertyChanged(nameof(IsEmpty));
    }
}
