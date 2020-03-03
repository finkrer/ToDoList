using System.Collections.Generic;

namespace ToDoList
{
    public interface IToDoList : IReadOnlyCollection<Entry>
    {
        void AddEntry(int entryId, int userId, string name, long timestamp);
        void RemoveEntry(int entryId, int userId, long timestamp);
        void MarkDone(int entryId, int userId, long timestamp);
        void MarkUndone(int entryId, int userId, long timestamp);
        void DismissUser(int userId);
        void AllowUser(int userId);
    }
}