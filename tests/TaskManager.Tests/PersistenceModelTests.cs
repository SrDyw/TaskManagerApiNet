using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using TaskManager.Api.Data;
using TaskManager.Api.Models;

namespace TaskManager.Tests;

public class PersistenceModelTests
{
    // Builds the SQL Server model without opening a connection.
    private static AppDbContext CreateSqlServerContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=unused;Database=unused")
            .Options);

    [Fact]
    public void TaskState_IsStoredAsIntegerColumn()
    {
        using var db = CreateSqlServerContext();
        var property = db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(TaskItem))!.FindProperty(nameof(TaskItem.State))!;

        Assert.Equal(typeof(TaskState), property.ClrType);
        Assert.Equal(typeof(int), property.GetProviderClrType());
        Assert.Equal("int", property.GetColumnType());
    }

    [Fact]
    public void TaskState_HasCheckConstraintCoveringAllEnumValues()
    {
        using var db = CreateSqlServerContext();
        var constraint = db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(TaskItem))!.GetCheckConstraints().Single();

        Assert.Equal("CK_Tasks_State", constraint.ModelName);
        Assert.Equal("[State] IN (0, 1, 2)", constraint.Sql);
    }
}
