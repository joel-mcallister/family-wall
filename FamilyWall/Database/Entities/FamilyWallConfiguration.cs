namespace FamilyWall.Database.Entities;

public class FamilyWallConfiguration
{
    public int Id { get; set; }

    public string Name { get; set; }

    public string State { get; set; }

    public string Background { get; set; }

    public List<FamilyWallWeatherObservationStations>? ObservationStations { get; set; }
}