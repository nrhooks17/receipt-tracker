using System.Collections.Generic;
using System.Linq;

namespace SpendingTracker.Models;

public class Receipt
{
    public string Name { get; set; } = string.Empty;
    public List<Item> Items { get; set; } = new();
    public DateTime DateAdded { get; set; }
    public decimal TotalCost => Items.Sum(i => i.Cost);
}