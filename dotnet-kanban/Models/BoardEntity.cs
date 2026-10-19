namespace dotnet_kanban.Models;

// Common base class for everything that lives on a board (boards, groups,
// tags and tasks). Each type stores its text in a different property - a board
// and a group use Name, a tag and a task use Title - so the base class exposes
// them all through one polymorphic member: DisplayName.
//
// The abstract / virtual members have no setter, so EF Core does not map them:
// the database schema stays exactly the same, but callers (such as the
// dashboard) can treat the four types through their shared base class.
public abstract class BoardEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Overridden by each derived type to return its own name.
    public abstract string DisplayName { get; }

    // Overridden only by the types that actually have a colour (groups and tags).
    public virtual string DisplayColour => "#808080";
}

public static class BoardEntityExtensions
{
    // Turns any collection of board entities into dashboard breakdown rows.
    // Because the parameter is a collection of the base type, the same helper
    // works for groups and for tags, calling whichever DisplayName /
    // DisplayColour each derived class implemented.
    public static List<CountRow> ToCountRows<T>(this IEnumerable<T> items, Func<T, int> count)
        where T : BoardEntity =>
        items.Select(item => new CountRow(item.DisplayName, item.DisplayColour, count(item))).ToList();
}
