using System.Security.Cryptography.Pkcs;
using LiteDB;

namespace FamilyWall.Database.Context;

public interface IFamilyWallDataContext 
{
    ILiteCollection<FamilyWallConfiguration> Configuration { get; }

    ILiteCollection<FamilyWallBackgrounds> Backgrounds { get; }
}

public class FamilyWallDbContext : IFamilyWallDataContext, IDisposable
{
    public FamilyWallDbContext(string connectionString)
    {
        var mapper = BsonMapper.Global;
        mapper.Entity<FamilyWallConfiguration>().Id(x => x.Id);
        mapper.Entity<FamilyWallBackgrounds>().Id(x => x.FileName);

        _db = new LiteDatabase(connectionString, mapper);
        _db.UtcDate = true;
    }

    private readonly LiteDatabase _db;

    public ILiteCollection<FamilyWallConfiguration> Configuration => _db.GetCollection<FamilyWallConfiguration>("configuration");

    public ILiteCollection<FamilyWallBackgrounds> Backgrounds =>
        _db.GetCollection<FamilyWallBackgrounds>("backgrounds");

    public void Dispose() => _db.Dispose();
}

public class FamilyWallBackgrounds
{
    public string Name { get; set; }

    public string FileName { get; set; }
}

public class FamilyWallConfiguration
{
    public int Id { get; set; }

    public string Name { get; set; }

    public string Background { get; set; }
}