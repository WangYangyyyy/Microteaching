using Furion.DatabaseAccessor;
using Microsoft.EntityFrameworkCore;

namespace BehaviorTest.EntityFramework.Core;

[AppDbContext("BehaviorTest", DbProvider.MySqlOfficial)]
public class DefaultDbContext : AppDbContext<DefaultDbContext>
{
    public DefaultDbContext(DbContextOptions<DefaultDbContext> options) : base(options)
    {
    }
}