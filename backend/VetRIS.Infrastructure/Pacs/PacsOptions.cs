namespace VetRIS.Infrastructure.Pacs;

public class PacsOptions
{
    public const string SectionName = "Pacs";

    public string Host { get; set; } = default!;
    public int Port { get; set; } = 104;
    public string CallingAeTitle { get; set; } = "VETRIS";
    public string CalledAeTitle { get; set; } = "ERAD_PACS";
    /// <summary>AE title C-MOVE results are sent to — this application's own SCP, once one exists.</summary>
    public string DestinationAeTitle { get; set; } = "VETRIS";
}
