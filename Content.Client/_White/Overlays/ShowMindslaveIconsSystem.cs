using Content.Client.Overlays;
using Content.Shared._White.Implants.Mindslave.Components;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client._White.Overlays;

public sealed class ShowMindslaveIconsSystem : EquipmentHudSystem<MindslaveComponent>
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindslaveComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
    }

    private void OnGetStatusIconsEvent(
        EntityUid uid,
        MindslaveComponent mindslaveComponent,
        ref GetStatusIconsEvent args)
    {
        if (!IsActive || args.InContainer)
        {
            return;
        }

        var mindSlaveIcon = MindslaveIcon(uid, mindslaveComponent);

        args.StatusIcons.AddRange(mindSlaveIcon);
    }

    private IEnumerable<StatusIconPrototype> MindslaveIcon(EntityUid uid, MindslaveComponent mindslave)
    {
        var result = new List<StatusIconPrototype>();

        string? iconType;
        if (GetEntity(mindslave.Master) == uid)
        {
            iconType = mindslave.MasterStatusIcon;
        }
        else if (mindslave.Slaves.Contains(GetNetEntity(uid)))
        {
            iconType = mindslave.SlaveStatusIcon;
        }
        else
        {
            return result;
        }

        if (_prototype.TryIndex<StatusIconPrototype>(iconType, out var mindslaveIcon))
        {
            result.Add(mindslaveIcon);
        }

        return result;
    }
}
