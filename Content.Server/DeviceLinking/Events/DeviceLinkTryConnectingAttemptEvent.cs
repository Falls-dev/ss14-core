using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.DeviceLinking.Events;
public sealed class DeviceLinkTryConnectingAttemptEvent : CancellableEntityEventArgs
{
    /// <summary>
    /// The other entity the user tries to connect us to.
    /// </summary>
    public EntityUid? other;
    public EntityUid User;

    //public string cancellationReason = "network-configurator-link-mode-cancelled-generic";
    public DeviceLinkTryConnectingAttemptEvent(EntityUid User, EntityUid? uidOther)
    {
        this.User = User;
        other = uidOther;
    }
}

