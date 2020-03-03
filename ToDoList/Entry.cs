using System;

namespace ToDoList
{
    public sealed class Entry : IEquatable<Entry>
    {
        public Entry(int id, string name, EntryState state)
        {
            Id = id;
            Name = name;
            State = state;
        }

        public int Id { get; }
        public string Name { get; }
        public EntryState State { get; }

        public bool Equals(Entry other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id && Name == other.Name && State == other.State;
        }

        public Entry MarkDone()
        {
            return State == EntryState.Done
                ? this
                : new Entry(Id, Name, EntryState.Done);
        }

        public Entry MarkUndone()
        {
            return State == EntryState.Undone
                ? this
                : new Entry(Id, Name, EntryState.Undone);
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is Entry other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id;
                hashCode = (hashCode * 397) ^ Name.GetHashCode();
                hashCode = (hashCode * 397) ^ (int) State;
                return hashCode;
            }
        }

        public static Entry Undone(int id, string name) => new Entry(id, name, EntryState.Undone);
        public static Entry Done(int id, string name) => new Entry(id, name, EntryState.Done);
    }
}