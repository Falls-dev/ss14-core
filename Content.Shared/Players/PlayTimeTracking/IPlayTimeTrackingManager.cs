using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Robust.Shared.Player;

namespace Content.Shared.Players.PlayTimeTracking;

public interface IPlayTimeTrackingManager
{
    Dictionary<string, TimeSpan> GetTrackerTimes(ICommonSession id);
}
