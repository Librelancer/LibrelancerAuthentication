using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LibrelancerAuthentication;

public class UserDbContext : DbContext
{
    private readonly string path;

    public UserDbContext(string path)
    {
        this.path = path;
    }

    public DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var connectionStringBuilder = new SqliteConnectionStringBuilder {DataSource = path};
        var connectionString = connectionStringBuilder.ToString();
        var connection = new SqliteConnection(connectionString);

        optionsBuilder.UseSqlite(connection);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>();
    }
}