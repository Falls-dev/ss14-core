using Content.Client.Rotation;
using Content.Shared.Buckle.Components;
using Content.Shared.Rotation;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client._White.Rotation;

public sealed class RotationVisualizerWhiteSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly RotationVisualizerSystem _rotation = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RotationVisualsComponent, SpriteComponent, AppearanceComponent>();
        while (query.MoveNext(out var uid, out var component, out var sprite, out var appearance))
        {
            _appearance.TryGetData<bool>(uid, BuckleVisuals.Buckled, out var buckled, appearance);

            if (buckled)
                continue;

            var rotation = _transform.GetWorldRotation(uid) + _eyeManager.CurrentEye.Rotation;
            _appearance.TryGetData<RotationState>(uid, RotationVisuals.RotationState, out var state, appearance);

            if (rotation.GetDir() is Direction.East or Direction.North or Direction.NorthEast or Direction.SouthEast)
            {
                component.HorizontalRotation = Angle.FromDegrees(270);

                if (state is RotationState.Horizontal)
                    _rotation.AnimateSpriteRotation(uid, sprite, component.HorizontalRotation, component.AnimationTime);

                return;
            }

            component.HorizontalRotation = component.DefaultRotation;

            if (state is RotationState.Horizontal)
                _rotation.AnimateSpriteRotation(uid, sprite, component.DefaultRotation, component.AnimationTime);
        }
    }
}
