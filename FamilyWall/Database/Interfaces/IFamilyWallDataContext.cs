using FamilyWall.Database.Entities;
using LiteDB;

namespace FamilyWall.Database.Interfaces;

public interface IFamilyWallDataContext 
{
    ILiteCollection<FamilyWallConfiguration> Configuration { get; }

    ILiteCollection<FamilyWallBackgrounds> Backgrounds { get; }

    ILiteCollection<FamilyWallTaskIconMapping> TaskIconMappings { get; }
}