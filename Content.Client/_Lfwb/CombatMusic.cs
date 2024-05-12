using Content.Client.Audio;
using Content.Client.CombatMode;
using Content.Shared.CombatMode;
using Content.Shared.GameTicking;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._Lfwb;

public sealed class CombatMusic : EntitySystem
{
    #region Values

    private EntityUid? _combatMusicStream;

    #endregion

    #region Dependencies

    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ContentAudioSystem _contentAudio = default!;
    [Dependency] private readonly CombatModeSystem _combat = default!;
    [Dependency] private readonly IResourceCache _resource = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    #endregion

    #region Event Handling

    public override void Initialize()
    {
        base.Initialize();

        PreloadTracks();

        SubscribeLocalEvent<CombatModeComponent, PlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRestart);

        _combat.LocalPlayerCombatModeUpdated += OnCombatModeUpdated;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _combat.LocalPlayerCombatModeUpdated -= OnCombatModeUpdated;

        _audio.Stop(_combatMusicStream);
    }

    #endregion


    #region Main Process

    private void OnCombatModeUpdated(bool inCombatMode)
    {
        (inCombatMode ? (Action) StartCombatMusic : StopCombatMusic).Invoke();
    }

    private void OnPlayerDetached(EntityUid uid, CombatModeComponent component, PlayerDetachedEvent args)
    {
        if (args.Player != _playerManager.LocalSession)
            return;

        StopCombatMusic();
    }

    private void OnRestart(RoundRestartCleanupEvent args)
    {
        StopCombatMusic();
    }

    #endregion


    #region Helpers

    private void PreloadTracks()
    {
        foreach (var audio in _proto.Index<SoundCollectionPrototype>("CombatMusic").PickFiles)
        {
            _resource.GetResource<AudioResource>(audio.ToString());
        }
    }

    private void StartCombatMusic()
    {
        var stream = _audio.PlayGlobal(new SoundCollectionSpecifier("CombatMusic"), Filter.Local(), false, AudioParams.Default.WithLoop(true).WithVolume(-4));

        if (!stream.HasValue)
            return;

        _combatMusicStream = stream.Value.Entity;
        _contentAudio.FadeIn(_combatMusicStream, stream.Value.Component, 3f);
    }

    private void StopCombatMusic()
    {
        _contentAudio.FadeOut(_combatMusicStream);
        _combatMusicStream = null;
    }

    #endregion
}
