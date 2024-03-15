using Content.Server.DeviceLinking.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server._White.MechComp;


//[RegisterComponent]
//public sealed partial class DisconnectOnUnanchorComponent : Component
//{
//}
//
//
//public sealed partial class DisconnectOnUnanchorSystem : EntitySystem
//{
//    [Dependency] private readonly DeviceLinkSystem _link = default!;
//    private void OnUnanchor(EntityUid uid, DisconnectOnUnanchorComponent component, AnchorStateChangedEvent args)
//    {
//        if (!args.Anchored)
//        {
//            _link.RemoveAllFromSink(uid);
//            _link.RemoveAllFromSource(uid);
//        }
//    }
//    public override void Initialize()
//    {
//        SubscribeLocalEvent<DisconnectOnUnanchorComponent, AnchorStateChangedEvent>(OnUnanchor);
//        
//    }
//}
