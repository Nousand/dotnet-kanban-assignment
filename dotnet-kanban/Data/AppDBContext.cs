using Microsoft.EntityFrameworkCore;
using dotnet_kanban.Models;

namespace dotnet_kanban.Data;

public class AppDBContext : DbContext
{
    public AppDBContext(DbContextOptions<AppDBContext> options) : base(options) { }

    // Tables for each model
    public DbSet<Board> Boards => Set<Board>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<Tag> Tags => Set<Tag>();


    // Creates model relationships in db
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Board>(b =>
        {
            b.Property(x => x.Name).HasMaxLength(100); // Restrictions for imported format

            b.HasMany(x => x.Groups).WithOne(g => g.Board).HasForeignKey(g => g.BoardId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.Tags).WithOne(t => t.Board).HasForeignKey(t => t.BoardId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Group>(g =>
        {
            g.Property(x => x.Name).HasMaxLength(100);
            g.Property(x => x.Colour).HasMaxLength(7); // Hex format

            g.HasMany(x => x.Tasks).WithOne(t => t.Group).HasForeignKey(t => t.GroupId).OnDelete(DeleteBehavior.Cascade);
            g.HasIndex(x => new { x.BoardId, x.Position});
        });

        modelBuilder.Entity<TaskItem>(t =>
        {
            t.Property(x => x.Title).HasMaxLength(200);

            t.HasMany(x => x.Tags).WithMany();
            t.HasIndex(x => new { x.GroupId, x.Position});
        });

        modelBuilder.Entity<Tag>(t =>
        {
            t.Property(x => x.Title).HasMaxLength(50);
            t.Property(x => x.Colour).HasMaxLength(7); // Hex format
        });
    }
}