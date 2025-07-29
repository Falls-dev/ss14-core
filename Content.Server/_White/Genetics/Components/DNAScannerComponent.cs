using Robust.Shared.Audio;

namespace Content.Server._White.Genetics.Components;

/// <summary>
///
/// </summary>
public sealed partial class DNAScannerComponent : Component
{
    /// <summary>
    /// How long it takes to scan someone.
    /// </summary>
    [DataField("UseDelay")]
    public TimeSpan ScanDelay = TimeSpan.FromSeconds(0.8);

    /// <summary>
    /// The maximum range in tiles at which the analyzer can receive continuous updates
    /// </summary>
    [DataField]
    public float MaxScanRange = 2.5f;

    /// <summary>
    /// Sound played on scanning begin
    /// </summary>
    [DataField]
    public SoundSpecifier? ScanningBeginSound;

    /// <summary>
    /// Sound played on scanning end
    /// </summary>
    [DataField]
    public SoundSpecifier? ScanningEndSound;

    /// <summary>
    /// Genome that was scanned.
    /// </summary>
    [DataField]
    public GenomeComponent? ScannedGenome;
}
