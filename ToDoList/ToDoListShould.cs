using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace ToDoList
{
    public class ToDoListShould
    {
        private const int userA = 10;
        private const int userB = 11;
        private const int userC = 14;

        private IToDoList list;

        [SetUp]
        public void SetUp()
        {
            list = new ToDoList();
        }

        [Test]
        public void Add_Entry()
        {
            list.AddEntry(42, userA, "Build project", 100);

            AssertEntries(Entry.Undone(42, "Build project"));
        }

        [Test]
        public void Not_Remove_Entry_If_Removal_Timestamp_Is_Less_Than_Entry_Timestamp(
            [Values(userA, userB, userC)] int removingUserId)
        {
            list.AddEntry(42, userA, "Build project", 100);

            list.RemoveEntry(42, removingUserId, 99);

            AssertEntries(Entry.Undone(42, "Build project"));
        }

        [Test]
        public void Remove_Entry(
            [Values(userA, userB, userC)] int removingUserId,
            [Values(200, 101, 100)] long removingTimestamp)
        {
            list.AddEntry(42, userA, "Build project", 100);

            list.RemoveEntry(42, removingUserId, removingTimestamp);

            AssertListEmpty();
        }

        [Test]
        public void Updates_Name_When_Entry_With_Greater_Timestamp_Added([Values(userA, userB, userC)] int updatingUserId)
        {
            list.AddEntry(42, userB, "Build project", 100);

            list.AddEntry(42, updatingUserId, "Create project", 105);

            AssertEntries(Entry.Undone(42, "Create project"));
        }

        [Test]
        public void Not_Add_Item_When_It_Was_Removed_With_Greater_Timestamp(
            [Values(userA, userB, userC)] int removingUserId,
            [Values(200, 101, 100)] long removingTimestamp)
        {
            list.RemoveEntry(42, removingUserId, removingTimestamp);
            list.AddEntry(42, userA, "Build project", 100);
            AssertListEmpty();
        }

        [Test]
        public void Not_Update_Name_When_Less_Experienced_User_Adds_Entry()
        {
            list.AddEntry(42, userA, "Create project", 100);

            list.AddEntry(42, userB, "Build project", 100);

            AssertEntries(Entry.Undone(42, "Create project"));
        }

        [Test]
        public void Add_Several_Entries()
        {
            list.AddEntry(42, userA, "Create audio subsystem", 150);
            list.AddEntry(90, userB, "Create video subsystem", 125);
            list.AddEntry(74, userC, "Create input subsystem", 117);

            AssertEntries(
                Entry.Undone(42, "Create audio subsystem"),
                Entry.Undone(90, "Create video subsystem"),
                Entry.Undone(74, "Create input subsystem")
            );
        }

        [Test]
        public void Mark_Entry_Done(
            [Values(userA, userB, userC)] int markingUserId,
            [Values(100, 95, 107)] long markTimestamp)
        {
            list.AddEntry(42, userB, "Create project", 100);

            list.MarkDone(42, markingUserId, markTimestamp);

            AssertEntries(Entry.Done(42, "Create project"));
        }

        [Test]
        public void Mark_Entry_Done_When_Entry_Does_Not_Exists(
            [Values(userA, userB, userC)] int markingUserId,
            [Values(100, 95, 107)] long markTimestamp)
        {
            list.MarkDone(42, markingUserId, markTimestamp);

            list.AddEntry(42, userB, "Create project", 100);

            AssertEntries(Entry.Done(42, "Create project"));
        }

        [Test]
        public void Mark_Undone()
        {
            list.AddEntry(42, userA, "Create project", 100);
            list.MarkDone(42, userB, 105);

            list.MarkUndone(42, userC, 106);

            AssertEntries(Entry.Undone(42, "Create project"));
        }

        [Test]
        public void Not_Mark_Undone_When_Timestamp_Less_Than_Done_Mark_Timestamp2()
        {
            list.AddEntry(42, userA, "Create project", 100);
            list.MarkDone(42, userB, 105);

            list.MarkUndone(42, userC, 107);
            list.MarkUndone(42, userC, 99);

            AssertEntries(Entry.Undone(42, "Create project"));
        }

        [Test]
        public void Dismiss_User_That_Did_Nothing()
        {
            list.AddEntry(42, userA, "Introduce autotests", 100);
            list.MarkDone(42, userB, 105);

            list.DismissUser(userC);

            AssertEntries(Entry.Done(42, "Introduce autotests"));
        }

        [Test]
        public void Dismiss_Creation()
        {
            list.AddEntry(42, userA, "Introduce autotests", 100);
            list.MarkDone(42, userB, 105);

            list.DismissUser(userA);

            AssertListEmpty();
        }

        [Test]
        public void Dismiss_Name_Updates()
        {
            list.AddEntry(42, userA, "Introduce autotests", 100);
            list.AddEntry(42, userB, "Introduce nice autotests", 105);

            list.DismissUser(userB);

            AssertEntries(Entry.Undone(42, "Introduce autotests"));
        }

        [Test]
        public void Dismiss_Done()
        {
            list.AddEntry(42, userA, "Introduce autotests", 100);
            list.MarkDone(42, userB, 105);

            list.DismissUser(userB);

            AssertEntries(Entry.Undone(42, "Introduce autotests"));
        }

        [Test]
        public void Dismiss_Undone()
        {
            list.AddEntry(42, userA, "Introduce autotests", 100);
            list.MarkDone(42, userA, 105);
            list.MarkUndone(42, userB, 107);

            list.DismissUser(userB);

            AssertEntries(Entry.Done(42, "Introduce autotests"));
        }

        [Test]
        public void Allow_User_That_Did_Nothing()
        {
            list.AddEntry(42, userA, "Introduce autotests", 100);
            list.MarkDone(42, userB, 105);
            list.DismissUser(userC);

            list.AllowUser(userC);

            AssertEntries(Entry.Done(42, "Introduce autotests"));
        }

        [Test]
        public void Allow_Creation()
        {
            list.AddEntry(42, userA, "Introduce autotests", 100);
            list.MarkDone(42, userB, 105);
            list.DismissUser(userA);

            list.AllowUser(userA);

            AssertEntries(Entry.Done(42, "Introduce autotests"));
        }

        [Test]
        public void Allow_Name_Updates()
        {
            list.AddEntry(42, userA, "Introduce autotests", 100);
            list.AddEntry(42, userB, "Introduce nice autotests", 105);
            list.DismissUser(userB);

            list.AllowUser(userB);

            AssertEntries(Entry.Undone(42, "Introduce nice autotests"));
        }

        [Test]
        public void Allow_Done()
        {
            list.AddEntry(42, userA, "Introduce autotests", 100);
            list.MarkDone(42, userB, 105);
            list.DismissUser(userB);

            list.AllowUser(userB);

            AssertEntries(Entry.Done(42, "Introduce autotests"));
        }

        [Test]
        public void Allow_Undone()
        {
            list.AddEntry(42, userA, "Introduce autotests", 100);
            list.MarkDone(42, userA, 105);
            list.MarkUndone(42, userB, 107);
            list.DismissUser(userB);

            list.AllowUser(userB);

            AssertEntries(Entry.Undone(42, "Introduce autotests"));
        }

        private void AssertListEmpty()
        {
            list.Should().BeEmpty();
            list.Count.Should().Be(0);
        }

        private void AssertEntries(params Entry[] expected)
        {
            list.Should().BeEquivalentTo(expected.AsEnumerable());
            list.Count.Should().Be(expected.Length);
        }
    }
}