using System.Collections;
using System.Collections.Generic;

namespace ToDoList
{
    public class ToDoList : IToDoList
    {
        private readonly Dictionary<long, List<Operation>> _operations = new Dictionary<long, List<Operation>>();
        private readonly HashSet<int> _bannedUsers = new HashSet<int>();
        private int CurrentVersion => _operations.GetHashCode();
        private int _lastBuildVersion;
        private ToDoListView _lastBuild;

        private void AddOperation(Operation operation)
        {
            if (!_operations.ContainsKey(operation.Timestamp))
                _operations[operation.Timestamp] = new List<Operation>();
            _operations[operation.Timestamp].Add(operation);
        }
        
        public void AddEntry(int entryId, int userId, string name, long timestamp)
        {
            var operation = new Operation(OperationType.AddEntry, entryId, userId, name, timestamp);
            AddOperation(operation);
        }

        public void RemoveEntry(int entryId, int userId, long timestamp)
        {
            var operation = new Operation(OperationType.RemoveEntry, entryId, userId, null, timestamp);
            AddOperation(operation);
        }

        public void MarkDone(int entryId, int userId, long timestamp)
        {
            var operation = new Operation(OperationType.MarkDone, entryId, userId, null, timestamp);
            AddOperation(operation);
        }

        public void MarkUndone(int entryId, int userId, long timestamp)
        {
            var operation = new Operation(OperationType.MarkUndone, entryId, userId, null, timestamp);
            AddOperation(operation);
        }

        public void DismissUser(int userId)
        {
            _bannedUsers.Add(userId);
        }

        public void AllowUser(int userId)
        {
            _bannedUsers.Remove(userId);
        }
        
        private ToDoListView GetLastBuild()
        {
            if (_lastBuildVersion != CurrentVersion)
            {
                _lastBuild = BuildList();
                _lastBuildVersion = CurrentVersion;
            }
            return _lastBuild;
        }

        private ToDoListView BuildList()
        {
            var list = new ToDoListView();
            foreach (var (_, concurrentOperations) in _operations)
            {
                foreach (var operation in concurrentOperations)
                {
                    if (_bannedUsers.Contains(operation.UserId))
                        continue;
                    var _ = operation.Type switch
                    {
                        OperationType.AddEntry => list.AddEntry(operation.EntryId, operation.Name),
                        OperationType.RemoveEntry => list.RemoveEntry(operation.EntryId),
                        OperationType.MarkDone => list.MarkDone(operation.EntryId),
                        OperationType.MarkUndone => list.MarkUndone(operation.EntryId)
                    };
                }
            }

            return list;
        }

        public IEnumerator<Entry> GetEnumerator()
        {
            return GetLastBuild().EntryList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count { get; }
    }

    public class ToDoListView
    {
        private readonly Dictionary<int, Entry> _entries = new Dictionary<int, Entry>();
        public IEnumerable<Entry> EntryList => _entries.Values;

        public ToDoListView AddEntry(int entryId, string name)
        {
            _entries[entryId] = new Entry(entryId, name, EntryState.Undone);
            return this;
        }

        public ToDoListView RemoveEntry(int entryId)
        {
            _entries.Remove(entryId);
            return this;
        }

        public ToDoListView MarkDone(int entryId)
        {
            var entry = _entries[entryId];
            _entries[entryId] = Entry.Done(entryId, entry.Name);
            return this;
        }

        public ToDoListView MarkUndone(int entryId)
        {
            var entry = _entries[entryId];
            _entries[entryId] = Entry.Undone(entryId, entry.Name);
            return this;
        }
    }

    public enum OperationType
    {
        AddEntry,
        RemoveEntry,
        MarkDone,
        MarkUndone
    }

    public class Operation
    {
        public readonly OperationType Type;
        public readonly int EntryId;
        public readonly int UserId;
        public readonly string Name;
        public readonly long Timestamp;

        public Operation(OperationType type, int entryId, int userId, string name, long timestamp)
        {
            Type = type;
            EntryId = entryId;
            UserId = userId;
            Name = name;
            Timestamp = timestamp;
        }
    }
}