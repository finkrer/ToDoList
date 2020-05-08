using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ToDoList
{
    public class ToDoList : IToDoList
    {
        private readonly Dictionary<int, SortedSet<NameOperation>> nameOperations 
            = new Dictionary<int, SortedSet<NameOperation>>();
        private readonly Dictionary<int, SortedSet<StateOperation>> stateOperations 
            = new Dictionary<int, SortedSet<StateOperation>>();
        private readonly Dictionary<int, string> currentNames = new Dictionary<int, string>();
        private readonly Dictionary<int, EntryState> currentStates = new Dictionary<int, EntryState>();
        private readonly HashSet<int> bannedUsers = new HashSet<int>();

        private static void AddToIndex<T>(IDictionary<int, SortedSet<T>> index, int key, T operation)
            where T : Operation
        {
            if (!index.ContainsKey(key))
                index[key] = new SortedSet<T>(operation.Comparer);
            index[key].Add(operation);
        }

        private void RecordOperation(Operation operation)
        {
            switch (operation)
            {
                case NameOperation n:
                    AddToIndex(nameOperations, n.EntryId, n);
                    break;
                case StateOperation s:
                    AddToIndex(stateOperations, s.EntryId, s);
                    break;
                default:
                    throw new ArgumentException("Unknown operation type");
            }
        }

        public void AddEntry(int entryId, int userId, string name, long timestamp)
        {
            var operation = Operation.Add(entryId, userId, timestamp, name);
            RecordOperation(operation);
            UpdateEntryName(entryId);
        }

        public void RemoveEntry(int entryId, int userId, long timestamp)
        {
            var operation = Operation.Remove(entryId, userId, timestamp);
            RecordOperation(operation);
            UpdateEntryName(entryId);
        }

        public void MarkDone(int entryId, int userId, long timestamp)
        {
            var operation = Operation.Done(entryId, userId, timestamp);
            RecordOperation(operation);
            UpdateEntryState(entryId);
        }

        public void MarkUndone(int entryId, int userId, long timestamp)
        {
            var operation = Operation.Undone(entryId, userId, timestamp);
            RecordOperation(operation);
            UpdateEntryState(entryId);
        }
        
        private T GetEffectiveOperation<T>(SortedSet<T> source)
            where T : Operation 
            => source.FirstOrDefault(o => !bannedUsers.Contains(o.UserId));

        public void DismissUser(int userId)
        {
            var namesToUpdate = GetEffectiveOperationsByUser(nameOperations, userId);
            var statesToUpdate = GetEffectiveOperationsByUser(stateOperations, userId);
            bannedUsers.Add(userId);
            foreach (var id in namesToUpdate) 
                UpdateEntryName(id);
            foreach (var id in statesToUpdate) 
                UpdateEntryState(id);
        }

        public void AllowUser(int userId)
        {
            bannedUsers.Remove(userId);
            var namesToUpdate = GetEffectiveOperationsByUser(nameOperations, userId);
            var statesToUpdate = GetEffectiveOperationsByUser(stateOperations, userId);
            foreach (var id in namesToUpdate) 
                UpdateEntryName(id);
            foreach (var id in statesToUpdate) 
                UpdateEntryState(id);
        }

        private HashSet<int> GetEffectiveOperationsByUser<T>(Dictionary<int, SortedSet<T>> source, int userId)
            where T : Operation
        {
            var result = new HashSet<int>();
            foreach (var (id, operations) in source)
            {
                var effectiveOperation = GetEffectiveOperation(operations);
                if (effectiveOperation?.UserId == userId)
                    result.Add(id);
            }

            return result;
        }

        private string GetUpdatedEntryName(int id)
        {
            var effectiveNameOperation = GetEffectiveOperation(nameOperations[id]);
            var name = effectiveNameOperation?.Name;
            return name;
        }

        private EntryState GetUpdatedEntryState(int id)
        {
            var effectiveStateOperation = GetEffectiveOperation(stateOperations[id]);
            var state = effectiveStateOperation?.State ?? EntryState.Undone;
            return state;
        }

        private void UpdateEntryName(int id)
        {
            if (GetUpdatedEntryName(id) is var newName && newName is null && currentNames.ContainsKey(id))
                currentNames.Remove(id);
            else if (!(newName is null))
                currentNames[id] = newName;
        }

        private void UpdateEntryState(int id)
        {
            currentStates[id] = GetUpdatedEntryState(id);
        }
        
        public IEnumerator<Entry> GetEnumerator()
        {
            foreach (var (id, name) in currentNames)
            {
                currentStates.TryGetValue(id, out var state);
                yield return new Entry(id, name, state);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => currentNames.Count;
    }

    #region Operations
    public class Operation
    {
        public readonly int EntryId;
        public readonly int UserId;
        public readonly long Timestamp;
        private static readonly IComparer<Operation> comparer = new OperationComparer();
        public virtual IComparer<Operation> Comparer => comparer;
        public Operation(int entryId, int userId, long timestamp)
        {
            EntryId = entryId;
            UserId = userId;
            Timestamp = timestamp;
        }

        public static Operation Add(int entryId, int userId, long timestamp, string name)
            => new NameOperation(entryId, userId, timestamp, NameOperation.NameOperationType.Add, name);
        public static Operation Remove(int entryId, int userId, long timestamp)
            => new NameOperation(entryId, userId, timestamp, NameOperation.NameOperationType.Remove, null);
        public static Operation Done(int entryId, int userId, long timestamp)
            => new StateOperation(entryId, userId, timestamp, EntryState.Done);
        public static Operation Undone(int entryId, int userId, long timestamp)
            => new StateOperation(entryId, userId, timestamp, EntryState.Undone);
    }
    
    public class NameOperation : Operation
    {
        public enum NameOperationType
        {
            Remove,
            Add
        }

        public readonly string Name;
        public readonly NameOperationType Type;
        private static readonly IComparer<Operation> comparer = new NameOperationComparer();
        public override IComparer<Operation> Comparer => comparer;

        public NameOperation(int entryId, int userId, long timestamp, NameOperationType type, string name) 
            : base(entryId, userId, timestamp)
        {
            Type = type;
            Name = name;
        }
    }
    
    public class StateOperation : Operation
    {
        public readonly EntryState State;
        private static readonly IComparer<Operation> comparer = new StateOperationComparer();
        public override IComparer<Operation> Comparer => comparer;

        public StateOperation(int entryId, int userId, long timestamp, EntryState state) 
            : base(entryId, userId, timestamp)
        {
            State = state;
        }
    }
    #endregion
    
    #region Comparers
    public class OperationComparer : IComparer<Operation>
    {
        public int Compare(Operation x, Operation y)
        {
            if (x is null || y is null)
                throw new ArgumentException("Cannot compare with null");
            if (x.EntryId != y.EntryId)
                throw new ArgumentException("Cannot compare operations on different entries");
            return -x.Timestamp.CompareTo(y.Timestamp);
        }
    }

    public class NameOperationComparer : OperationComparer, IComparer<NameOperation>
    {
        public int Compare(NameOperation x, NameOperation y)
        {
            if (base.Compare(x, y) is var result && result != 0)
                return result;
            if (x.Type < y.Type || x.Type == y.Type && x.UserId < y.UserId)
                return -1;
            if (x.Type == y.Type && x.UserId == y.UserId)
                return 0;
            return 1;
        }
    }
    
    public class StateOperationComparer : OperationComparer, IComparer<StateOperation>
    {
        public int Compare(StateOperation x, StateOperation y)
        {
            if (base.Compare(x, y) is var result && result != 0)
                return result;
            if (x.State < y.State)
                return -1;
            if (x.State == y.State && x.UserId == y.UserId)
                return 0;
            return 1;
        }
    }
    #endregion
    
    public static class Deconstructor
    {
        //чтобы не переписывать с .NET Core
        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> keyValuePair,
            out TKey key, out TValue value)
        {
            key = keyValuePair.Key;
            value = keyValuePair.Value;
        }
    }
}