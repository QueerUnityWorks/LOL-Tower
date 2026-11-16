using System;
using System.Collections.Generic;
using System.Linq;

namespace ElementalTactics
{
    public sealed class FormationState
    {
        private readonly int[] slots = Enumerable.Repeat(-1, 6).ToArray();
        public int RosterCount { get; private set; }
        public int Count => slots.Count(index => index >= 0);
        public int this[int slot] => slots[slot];

        private readonly int startingCount;

        public FormationState(int rosterCount, int startingCount = 1)
        {
            RosterCount = Math.Max(0, rosterCount);
            this.startingCount = Math.Max(0, Math.Min(startingCount, RosterCount));
            Reset();
        }

        public int FindSlot(int rosterIndex) { return Array.IndexOf(slots, rosterIndex); }

        public bool Place(int rosterIndex, int destination)
        {
            if (rosterIndex < 0 || rosterIndex >= RosterCount || destination < 0 || destination >= slots.Length) return false;
            int origin = FindSlot(rosterIndex);
            if (origin == destination) return true;
            if (origin >= 0) slots[origin] = slots[destination];
            slots[destination] = rosterIndex;
            return true;
        }

        public void Remove(int slot)
        {
            if (slot >= 0 && slot < slots.Length) slots[slot] = -1;
        }

        public void Reset()
        {
            for (int slot = 0; slot < slots.Length; slot++) slots[slot] = slot < startingCount ? slot : -1;
        }

        public List<CharacterDefinition> Lineup(List<CharacterDefinition> roster)
        {
            return slots.Select(index => index >= 0 && index < roster.Count ? roster[index] : null).ToList();
        }
    }
}
