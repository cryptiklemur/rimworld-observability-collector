using Cryptiklemur.RimObs.Collector.Storage;
using FluentAssertions;
using Xunit;

namespace Cryptiklemur.RimObs.Collector.Tests;

public class DynamicPatchStoreTests {
    private static DynamicPatchStore Open() => DynamicPatchStore.OpenInMemory();

    [Fact]
    public void Insert_then_list_returns_row() {
        using DynamicPatchStore store = Open();
        long id = store.Insert("Verse.PathFinder", "FindPath", "Verse.IntVec3;Verse.IntVec3");

        IReadOnlyList<DynamicPatchRow> rows = store.List();

        rows.Should().HaveCount(1);
        rows[0].Id.Should().Be(id);
        rows[0].TypeFullName.Should().Be("Verse.PathFinder");
        rows[0].LastStatus.Should().Be("pending");
    }

    [Fact]
    public void Insert_is_idempotent_on_signature_triple() {
        using DynamicPatchStore store = Open();
        long first = store.Insert("A", "B", "P");
        long second = store.Insert("A", "B", "P");
        second.Should().Be(first);
        store.List().Should().HaveCount(1);
    }

    [Fact]
    public void Update_status_persists() {
        using DynamicPatchStore store = Open();
        long id = store.Insert("A", "B", "");
        store.UpdateStatus(id, "stale", "mod uninstalled");

        DynamicPatchRow row = store.List()[0];
        row.LastStatus.Should().Be("stale");
        row.LastError.Should().Be("mod uninstalled");
    }

    [Fact]
    public void Delete_removes_row() {
        using DynamicPatchStore store = Open();
        long id = store.Insert("A", "B", "");
        store.Delete(id).Should().BeTrue();
        store.List().Should().BeEmpty();
    }
}
