using FamilyWall.Database.Entities;
using FamilyWall.Database.Interfaces;
using FamilyWall.Models;
using LiteDB;

namespace FamilyWall.Database.Context;

public class FamilyWallDbContext : IFamilyWallDataContext, IDisposable
{
    public FamilyWallDbContext(string connectionString)
    {
        var mapper = BsonMapper.Global;
        mapper.Entity<FamilyWallConfiguration>().Id(x => x.Id);
        mapper.Entity<FamilyWallBackgrounds>().Id(x => x.FileName);
        mapper.Entity<FamilyWallTaskIconMapping>().Id(x => x.Icon);
        mapper.Entity<FamilyWallPhoto>().Id(x => x.FileName);

        _db = new LiteDatabase(connectionString, mapper);
        _db.UtcDate = true;
    }

    private readonly LiteDatabase _db;

    public ILiteCollection<FamilyWallConfiguration> Configuration => _db.GetCollection<FamilyWallConfiguration>("configuration");

    public ILiteCollection<FamilyWallBackgrounds> Backgrounds => _db.GetCollection<FamilyWallBackgrounds>("backgrounds");

    public ILiteCollection<FamilyWallTaskIconMapping> TaskIconMappings => _db.GetCollection<FamilyWallTaskIconMapping>("icon-mappings");

    public ILiteCollection<FamilyWallPhoto> Photos => _db.GetCollection<FamilyWallPhoto>("photos");

    public void Dispose() => _db.Dispose();
}

public sealed class FamilyWallPhoto : OneDriveItem
{
    public string FileName { get; set; }
}