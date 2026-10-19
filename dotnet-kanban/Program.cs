using Microsoft.EntityFrameworkCore;
using dotnet_kanban.Data;
using System.ComponentModel.DataAnnotations;
using dotnet_kanban.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// DB services
builder.Services.AddDbContextFactory<AppDBContext> (
    options => options.UseSqlite("Data Source=kanban.db"));

builder.Services.AddScoped<IBoardService, BoardService>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<ITagService, TagService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDBContext>>();
    using var db = factory.CreateDbContext();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
