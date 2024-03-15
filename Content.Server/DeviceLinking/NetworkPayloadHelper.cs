using Content.Server.DeviceLinking.Components;
using Content.Shared.DeviceNetwork;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.DeviceLinking;

/// <summary>
/// Helper methods for providing compatibility between different signal keys.
/// </summary>
public static class NetworkPayloadHelper
{
    public static bool TryGetState(this NetworkPayload payload, [NotNullWhen(true)] out SignalState value)
    {
        if (payload.TryGetValue("logic_state", out value)) // DeviceNetworkConstants is in Content.Server, which cannot be accessed from shared. Fuck you whoever designed this.
        {
            return true; // if a proper logic_state is present, return that. Hopefully noone does that, since it's probably going to confuse the players
        }
        //otherwise try using mechcomp signal
        if (payload.TryGetValue("mechcomp_data", out string? sig)) // DeviceNetworkConstants is in Content.Server, which cannot be accessed from shared. Fuck you whoever designed this.
        {
            // this is, more or less, the same as it worked in 13
            if (int.TryParse(sig, out int signal_number))
            {
                value = signal_number != 0 ? SignalState.High : SignalState.Low;
                return true;
            }
            value = sig.Length > 0 ? SignalState.High : SignalState.Low;
            return true;
        }
        // add any other snowflake-ish checks here as needed
        value = SignalState.Momentary;
        return false;
    }
}
