using dotnet_kanban.Data;
using dotnet_kanban.Models;
using Microsoft.EntityFrameworkCore;

namespace dotnet_kanban.Services;

public class TaskService
{
    private readonly IDbContextFactory<AppDBContext> _factory;

    public TaskService(IDbContextFactory<AppDBContext> factory) => _factory = factory;
}