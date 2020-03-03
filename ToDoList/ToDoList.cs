using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ToDoList
{
    public class ToDoList : IToDoList
    {
        private readonly Dictionary<int, List<Operation>> operationsByEntryId = new Dictionary<int, List<Operation>>();
        private readonly Dictionary<int, List<Operation>> operationsByUserId = new Dictionary<int, List<Operation>>();
        private readonly Dictionary<int, Entry> currentEntries = new Dictionary<int, Entry>();
        private readonly HashSet<int> bannedUsers = new HashSet<int>();

        private static void AddToIndex<T>(IDictionary<T, List<Operation>> index, Operation operation, Func<Operation, T> keySelector)
        {
            var key = keySelector(operation);
            if (!index.ContainsKey(key))
                index[key] = new List<Operation>();
            index[key].Add(operation);
        }

        private void RecordOperation(Operation operation)
        {
            AddToIndex(operationsByEntryId, operation, o => o.EntryId);
            AddToIndex(operationsByUserId, operation, o => o.UserId);
        }

        private bool AnyConflictingOperations(Operation current, Predicate<Operation> filter,
            Predicate<Operation> condition)
        {
            return operationsByEntryId[current.EntryId]
                .Where(operation => filter(operation) && !bannedUsers.Contains(operation.UserId))
                .Any(operation => condition(operation));
        }

        public void AddEntry(int entryId, int userId, string name, long timestamp)
        {
            var operation = Operation.Add(entryId, userId, name, timestamp);
            AddEntryIfNeeded(operation);
            RecordOperation(operation);
        }

        private void AddEntryIfNeeded(Operation current)
        {
            if (!operationsByEntryId.ContainsKey(current.EntryId) 
                || !AnyConflictingOperations(current, 
                    o => o.Type.IsAddOrRemove(), 
                    o => o.Timestamp > current.Timestamp 
                         || o.Timestamp == current.Timestamp 
                         && (o.Type is OperationType.RemoveEntry || o.UserId < current.UserId)))
                currentEntries[current.EntryId] = new Entry(current.EntryId, current.Name, GetCorrectStateFor(current.EntryId));
        }

        public void RemoveEntry(int entryId, int userId, long timestamp)
        {
            var operation = Operation.Remove(entryId, userId, timestamp);
            RemoveEntryIfNeeded(operation);
            RecordOperation(operation);
        }

        private void RemoveEntryIfNeeded(Operation current)
        {
            if (operationsByEntryId.ContainsKey(current.EntryId) 
                && !AnyConflictingOperations(current, 
                    o => o.Type.IsAddOrRemove(),
                    o => o.Timestamp > current.Timestamp))
                currentEntries.Remove(current.EntryId);
        }
        
        private EntryState GetCorrectStateFor(int entryId)
        {
            if (!operationsByEntryId.ContainsKey(entryId))
                return EntryState.Undone;
            var relevantOperations =
                from operation in operationsByEntryId[entryId]
                where !bannedUsers.Contains(operation.UserId)
                      && operation.Type.IsDoneOrUndone()
                orderby operation.Timestamp descending, operation.Type
                select operation;
            var type = relevantOperations.FirstOrDefault()?.Type ?? OperationType.MarkUndone;
            return type == OperationType.MarkDone 
                ? EntryState.Done 
                : EntryState.Undone;
        }

        public void MarkDone(int entryId, int userId, long timestamp)
        {
            var operation = Operation.Done(entryId, userId, timestamp);
            MarkDoneIfNeeded(operation);
            RecordOperation(operation);
        }
        
        private void MarkDoneIfNeeded(Operation current)
        {
            if (currentEntries.ContainsKey(current.EntryId)
                && !AnyConflictingOperations(current,
                    o => o.Type.IsDoneOrUndone(),
                    o => o.Timestamp > current.Timestamp
                    || o.Type is OperationType.MarkUndone || o.Timestamp == current.Timestamp))
                currentEntries[current.EntryId] = currentEntries[current.EntryId].MarkDone();
        }

        public void MarkUndone(int entryId, int userId, long timestamp)
        {
            var operation = Operation.Undone(entryId, userId, timestamp);
            MarkUndoneIfNeeded(operation);
            RecordOperation(operation);
        }

        private void MarkUndoneIfNeeded(Operation current)
        {
            if (currentEntries.ContainsKey(current.EntryId)
                && !AnyConflictingOperations(current,
                    o => o.Type.IsDoneOrUndone(),
                    o => o.Timestamp > current.Timestamp))
                currentEntries[current.EntryId] = currentEntries[current.EntryId].MarkUndone();
        }

        public void DismissUser(int userId)
        {
            bannedUsers.Add(userId);
            RevertOperationsIfNeeded(userId);
        }

        private void RevertOperationsIfNeeded(int userId)
        {
            if (!operationsByUserId.ContainsKey(userId))
                return;
            foreach (var operation in operationsByUserId[userId])
            foreach (var relatedOperation in operationsByEntryId[operation.EntryId].Where(o => o.Type.IsInSameCategoryAs(operation.Type)))
                ReapplyOperation(relatedOperation);
        }

        public void AllowUser(int userId)
        {
            bannedUsers.Remove(userId);
            RestoreOperationsIfNeeded(userId);
        }

        private void RestoreOperationsIfNeeded(int userId)
        {
            if (!operationsByUserId.ContainsKey(userId))
                return;
            foreach (var operation in operationsByUserId[userId])
                ReapplyOperation(operation);
        }

        private void ReapplyOperation(Operation operation)
        {
            switch (operation.Type)
            {
                case OperationType.AddEntry:
                    AddEntryIfNeeded(operation);
                    //RemoveEntryIfNeeded(Operation.Remove(operation.EntryId, operation.UserId, long.MaxValue));
                    break;
                case OperationType.RemoveEntry:
                    RemoveEntryIfNeeded(operation);
                    break;
                case OperationType.MarkDone:
                    MarkDoneIfNeeded(operation);
                    break;
                case OperationType.MarkUndone:
                    MarkUndoneIfNeeded(operation);
                    break;
            }
        }

        public IEnumerator<Entry> GetEnumerator()
        {
            return currentEntries.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => currentEntries.Values.ToList().Count;
    }

    public enum OperationType
    {
        RemoveEntry,
        AddEntry,
        MarkUndone,
        MarkDone
    }

    public static class OperationTypeExtensions
    {
        public static bool IsAddOrRemove(this OperationType operationType) =>
            operationType is OperationType.AddEntry || operationType is OperationType.RemoveEntry;

        public static bool IsDoneOrUndone(this OperationType operationType) =>
            operationType is OperationType.MarkDone || operationType is OperationType.MarkUndone;

        public static bool IsInSameCategoryAs(this OperationType operationType, OperationType other) =>
            IsAddOrRemove(operationType) == IsAddOrRemove(other) &&
            IsDoneOrUndone(operationType) == IsDoneOrUndone(other);
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

        public static Operation Add(int entryId, int userId, string name, long timestamp) =>
            new Operation(OperationType.AddEntry, entryId, userId, name, timestamp);
        public static Operation Remove(int entryId, int userId, long timestamp) =>
            new Operation(OperationType.RemoveEntry, entryId, userId, null, timestamp);
        public static Operation Done(int entryId, int userId, long timestamp) =>
            new Operation(OperationType.MarkDone, entryId, userId, null, timestamp);
        public static Operation Undone(int entryId, int userId, long timestamp) =>
            new Operation(OperationType.MarkUndone, entryId, userId, null, timestamp);
    }
}