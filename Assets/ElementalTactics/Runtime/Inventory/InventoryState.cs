using System;
using System.Collections.Generic;
using UnityEngine;

namespace ElementalTactics
{
    public sealed class InventoryItem
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public int Quantity { get; internal set; }

        internal InventoryItem(string id, string name, string description, int quantity)
        {
            Id = id;
            Name = name;
            Description = description ?? "";
            Quantity = quantity;
        }
    }

    public static class InventoryState
    {
        private static readonly List<InventoryItem> items = new List<InventoryItem>();
        private static readonly IReadOnlyList<InventoryItem> view = items.AsReadOnly();
        public static IReadOnlyList<InventoryItem> Items => view;
        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            Changed = null;
            items.Clear();
        }

        public static void Reset()
        {
            items.Clear();
            Changed?.Invoke();
        }

        public static bool TryAdd(string id, string name, string description, int quantity = 1)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name) || quantity <= 0) return false;
            InventoryItem item = items.Find(entry => entry.Id == id);
            if (item == null) items.Add(new InventoryItem(id, name, description, quantity));
            else
            {
                if (quantity > int.MaxValue - item.Quantity) return false;
                item.Quantity += quantity;
            }
            Changed?.Invoke();
            return true;
        }

        public static bool TryRemove(string id, int quantity = 1)
        {
            InventoryItem item = items.Find(entry => entry.Id == id);
            if (item == null || quantity <= 0 || quantity > item.Quantity) return false;
            item.Quantity -= quantity;
            if (item.Quantity == 0) items.Remove(item);
            Changed?.Invoke();
            return true;
        }
    }
}
