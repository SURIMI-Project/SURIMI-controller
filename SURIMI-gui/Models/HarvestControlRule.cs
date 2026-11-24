using Grpc.Surimi;

namespace SURIMI_gui.Models;

public class HarvestControlRule
{
    public bool Selected { get; set; }
    public required Species Species { get; init; }
    public double BiomassLimit { get; set; }
    public double BiomassBase { get; set; }
    public double FMax { get; set; }

    public static HarvestControlRule FromInitialData(InitialHCRData data) => new()
    {
        Species = (data.Species ?? throw new InvalidDataException("Initial HCR data is missing a species.")).Clone(),
        BiomassLimit = data.DefaultBiomassLimit,
        BiomassBase = data.DefaultBiomassBase,
        FMax = data.DefaultFMax
    };

    public TargetFishingMortality ToTargetFishingMortality() => new()
    {
        Species = Species.Clone(),
        BiomassLimit = BiomassLimit,
        BiomassBase = BiomassBase,
        FMax = FMax
    };
}
