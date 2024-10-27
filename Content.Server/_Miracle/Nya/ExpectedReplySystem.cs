using Content.Server.Chat.Managers;
using Robust.Shared.Player;
using Content.Shared._Miracle.Nya;
using Robust.Shared.Enums;
using Robust.Shared.Timing;

namespace Content.Server._Miracle.Nya
{
    public sealed class ExpectedReplySystem : EntitySystem
    {
        [Dependency] private readonly ISharedPlayerManager _playMan = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;

        private readonly Dictionary<ICommonSession, PendingReply> _pendingReplies = new();
        private const float ReplyTimeoutSeconds = 5.0f;

        public override void Initialize()
        {
            base.Initialize();
            _playMan.PlayerStatusChanged += OnPlayerStatusChanged;
        }

        private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            if (e is { OldStatus: SessionStatus.InGame, NewStatus: SessionStatus.Disconnected })
            {
                _pendingReplies.Remove(e.Session);
            }
        }

        public void ExpectReply<TRequest, TResponse>(
            ICommonSession player,
            TRequest request,
            Action<TResponse, EntitySessionEventArgs> handler)
            where TRequest : ExpectedReplyEntityEventArgs
            where TResponse : EntityEventArgs
        {
            var timeout = _timing.CurTime + TimeSpan.FromSeconds(ReplyTimeoutSeconds);

            void WrapHandler(EntityEventArgs ev, EntitySessionEventArgs args)
            {
                if (ev is TResponse response)
                    handler(response, args);
            }

            _pendingReplies[player] = new PendingReply(request, timeout, WrapHandler);
            RaiseNetworkEvent(request, player.Channel);
        }

        public bool HandleReply(EntityEventArgs ev, EntitySessionEventArgs args)
        {
            if (!_pendingReplies.TryGetValue(args.SenderSession, out var pending))
            {
                LogSuspiciousActivity(args.SenderSession, "Unexpected response");
                return false;
            }

            if (pending.Request.ExpectedReplyType != ev.GetType())
            {
                LogSuspiciousActivity(args.SenderSession, $"Wrong reply type. Expected {pending.Request.ExpectedReplyType}, got {ev.GetType()}");
                return false;
            }

            pending.Handler(ev, args);
            _pendingReplies.Remove(args.SenderSession);
            return true;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var currentTime = _timing.CurTime;
            var timeoutPlayers = new List<ICommonSession>();

            foreach (var (player, pending) in _pendingReplies)
            {
                if (currentTime > pending.TimeoutTime)
                    timeoutPlayers.Add(player);
            }

            foreach (var player in timeoutPlayers)
            {
                HandleTimeout(player);
                _pendingReplies.Remove(player);
            }
        }

        private void HandleTimeout(ICommonSession player)
        {
            LogSuspiciousActivity(player, $"No reply within {ReplyTimeoutSeconds} seconds");
        }

        private void LogSuspiciousActivity(ICommonSession player, string reason)
        {
            var warningMsg = $"[color=red][Anticheat][/color] Внимание! Подозрительная активность:\n" +
                             $"Игрок {player.Name} возможно читер!\n" +
                             $"Причина обнаружения: {reason}";
            _chatManager.SendAdminAnnouncement(warningMsg);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _playMan.PlayerStatusChanged -= OnPlayerStatusChanged;
            _pendingReplies.Clear();
        }
    }
}
