using AmbermoonServer.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AmbermoonServer.Data;

public class AppDbContext : DbContext
{
	private const string DatabaseFile = "ambermoon.db";
	private const string DatabaseSource = $"Data Source={DatabaseFile}";

	public DbSet<Savegame> Savegame { get; set; }
	public DbSet<User> User { get; set; }
	public DbSet<UserState> UserState { get; set; }
    public DbSet<Source> Source { get; set; }
    public DbSet<GameVersion> GameVersion { get; set; }
    public DbSet<Language> Language { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		optionsBuilder.UseSqlite(DatabaseSource);
	}
}
