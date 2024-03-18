using Content.Shared._White.MechComp;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;
using Content.Shared.Interaction;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Content.Client.Salvage.FultonSystem;

namespace Content.Client._White.MechComp;

public sealed partial class MechCompDeviceSystem : SharedMechCompDeviceSystem
{
    //[Dependency] private readonly DeviceLinkSystem _link = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly AnimationPlayerSystem _anim = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    Dictionary<(EntityUid, TimeSpan, RSI.StateId, string, object), Animation> _cachedAnims = new();
    /// <summary>
    ///     Start playing an animation. If an animation under given key is already playing, replace it instead of the default behaviour (Shit pants and die)
    /// </summary>
    [Obsolete("This was added in a fit of rage: may be removed later.")]


    private bool GetMode<T>(EntityUid uid, out T val)
    {
        return _appearance.TryGetData(uid, MechCompDeviceVisuals.Mode, out val);
    }
    private Animation _prepFlickAnim(string state, float durationSeconds, object layer)
    {
        return new Animation()
        {
            Length = TimeSpan.FromSeconds(durationSeconds),
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick()
                {
                    LayerKey = layer,
                    KeyFrames = { new AnimationTrackSpriteFlick.KeyFrame(state, 0f) }
                }
            }
        };
    }


    public override void Initialize()
    {
        SubscribeLocalEvent<MechCompTeleportComponent, ComponentInit>(OnTeleportInit);
        SubscribeLocalEvent<MechCompTeleportComponent, AppearanceChangeEvent>(OnTeleportAppearanceChange);

        SubscribeLocalEvent<MechCompButtonComponent, ComponentInit>(OnButtonInit);
        SubscribeLocalEvent<MechCompButtonComponent, AppearanceChangeEvent>(OnButtonAppearanceChange);

        SubscribeLocalEvent<MechCompSpeakerComponent, ComponentInit>(OnSpeakerInit);
        SubscribeLocalEvent<MechCompSpeakerComponent, AppearanceChangeEvent>(OnSpeakerAppearanceChange);


    }
    private void OnButtonInit(EntityUid uid, MechCompButtonComponent comp, ComponentInit args)
    {
        comp.pressedAnimation = _prepFlickAnim("pressed", 0.5f, MechCompDeviceVisualLayers.Base);
    }

    private void OnButtonAppearanceChange(EntityUid uid, MechCompButtonComponent comp, AppearanceChangeEvent args)
    {
        if (GetMode(uid, out string _)) // not expecting any specific value, only the fact that it's under the MechCompVisuals.Mode key.
        {
            _anim.SafePlay(uid, (Animation) comp.pressedAnimation, "button");
            args.Sprite?.LayerSetAnimationTime(MechCompDeviceVisualLayers.Base, 0); // hack: Stop()ing and immediately Play()ing an animation does not seem to restart it
        }
    }

    private void OnSpeakerInit(EntityUid uid, MechCompSpeakerComponent comp, ComponentInit args)
    {
        comp.speakAnimation = _prepFlickAnim("speak", 0.6f, MechCompDeviceVisualLayers.Effect1);
    }

    private void OnSpeakerAppearanceChange(EntityUid uid, MechCompSpeakerComponent comp, AppearanceChangeEvent args)
    {
        if (GetMode(uid, out string _)) // not expecting any specific value, only the fact that it's under the MechCompVisuals.Mode key.
        {
            _anim.SafePlay(uid, (Animation) comp.speakAnimation, "speaker");
            args.Sprite?.LayerSetAnimationTime(MechCompDeviceVisualLayers.Effect1, 0); // hack: Stop()ing and immediately Play()ing an animation does not seem to restart it
        }
    }

    private void OnTeleportInit(EntityUid uid, MechCompTeleportComponent comp, ComponentInit args)
    {
        comp.firingAnimation = _prepFlickAnim("firing", 0.5f, MechCompDeviceVisualLayers.Effect2);
        var sprite = Comp<SpriteComponent>(uid);
        sprite.LayerSetState(sprite.LayerMapGet(MechCompDeviceVisualLayers.Effect1), "ready");
    }

    private void OnTeleportAppearanceChange(EntityUid uid, MechCompTeleportComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null) 
            return;
        //_handleAnchored(args);

        var effectlayer1 = args.Sprite.LayerMapGet(MechCompDeviceVisualLayers.Effect1); // used for ready and charging states
        //var effectlayer2 = args.Sprite.LayerMapGet(MechCompDeviceVisualLayers.Effect2); // used for firing animation; layer specified in animation itself
        //var effectlayer3 = args.Sprite.LayerMapGet(MechCompDeviceVisualLayers.Effect3); // (will be) used for a glow effect

        if (GetMode(uid, out string mode)){
            switch (mode)
            {
                case "ready":
                    args.Sprite?.LayerSetState(effectlayer1, "ready");
                    break;
                case "firing":
                    args.Sprite?.LayerSetState(effectlayer1, "charging");   
                    _anim.SafePlay(uid, (Animation)comp.firingAnimation, "teleport");
                    break;
                case "charging":
                    args.Sprite?.LayerSetState(effectlayer1, "charging");
                    break;
            }
        }
    }
}


/// <summary>
/// Kinda like a GenericVisualizerComponent, except preconfigured to work with mechcomp devices, but less customisable overall.
/// Also changes DrawDepth to FloorObjects when anchrored and back to SmallObjects when unanchored.
/// </summary>
[RegisterComponent]
[Access(typeof(MechCompAnchoredVisualizerSystem))]
public sealed partial class MechCompAnchoredVisualizerComponent : Component {
    [DataField("disabled")]
    public bool Disabled = false; // todo: figure out how to remove a component from prototye if it's defined in prototype's parent. And get rid of this shit afterwards.
    //[DataField("layer")]
    //public int Layer = (int)MechCompDeviceVisualLayers.Base;
    [DataField("anchoredState")]
    public string AnchoredState = "anchored";
    [DataField("unanchoredState")]
    public string UnanchoredState = "icon";
    [DataField("anchoredDepth")]
    public int AnchoredDepth = (int)DrawDepth.FloorObjects; // todo: figure out how to pass enums in prototypes
    [DataField("unanchoredDepth")]
    public int UnanchoredDepth = (int)DrawDepth.SmallObjects;
    [DataField("hideShowEffectsLayer")]
    public bool HideShowEffectsLayer = true;

}
public sealed class MechCompAnchoredVisualizerSystem : VisualizerSystem<MechCompAnchoredVisualizerComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, MechCompAnchoredVisualizerComponent comp, ref AppearanceChangeEvent args)
    {
        var layerKey = MechCompDeviceVisualLayers.Base;

        if (comp.Disabled || args.Sprite == null)
            return;

        //var layer = args.Sprite.LayerMapGet(layer);

        if (AppearanceSystem.TryGetData(uid, MechCompDeviceVisuals.Anchored, out bool anchored))
        {
            args.Sprite.LayerSetState(layerKey, anchored ? comp.AnchoredState : comp.UnanchoredState);
            args.Sprite.DrawDepth = (int) (anchored ? comp.AnchoredDepth : comp.UnanchoredDepth);
            if (comp.HideShowEffectsLayer)
            {
                args.Sprite.LayerSetVisible(args.Sprite.LayerMapGet(MechCompDeviceVisualLayers.Effect1), anchored);
                args.Sprite.LayerSetVisible(args.Sprite.LayerMapGet(MechCompDeviceVisualLayers.Effect2), anchored);
                args.Sprite.LayerSetVisible(args.Sprite.LayerMapGet(MechCompDeviceVisualLayers.Effect3), anchored);
                args.Sprite.LayerSetVisible(args.Sprite.LayerMapGet(MechCompDeviceVisualLayers.Effect4), anchored);
                args.Sprite.LayerSetVisible(args.Sprite.LayerMapGet(MechCompDeviceVisualLayers.Effect5), anchored);
                args.Sprite.LayerSetVisible(args.Sprite.LayerMapGet(MechCompDeviceVisualLayers.Effect6), anchored);

            }
        }
    }
}

/// <summary>
/// Move up from this namespace as needed
/// </summary>
public static class SafePlayExt
{
    /// <summary>
    ///     Start playing an animation. If an animation under given key is already playing, replace it instead of the default behaviour (Shit pants and die)
    /// </summary>
    [Obsolete("This was added in a fit of rage: may be removed later.")]
    public void SafePlay(this AnimationPlayerSystem anim, EntityUid uid, Animation animation, string key)
    {
        var component = EnsureComp<AnimationPlayerComponent>(uid);
        anim.Stop(component, key);
        anim.Play(new Entity<AnimationPlayerComponent>(uid, component), animation, key);
    }
    /// <summary>
    ///     Start playing an animation. If an animation under given key is already playing, replace it instead of the default behaviour (Shit pants and die)
    /// </summary>
    [Obsolete("This was added in a fit of rage: may be removed later.")]
    public void SafePlay(this AnimationPlayerSystem anim, EntityUid uid, AnimationPlayerComponent? component, Animation animation, string key)
    {
        component ??= EntityManager.EnsureComponent<AnimationPlayerComponent>(uid);
        anim.Stop(component, key);
        anim.Play(new Entity<AnimationPlayerComponent>(uid, component), animation, key);
    }
}
