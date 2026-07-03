using System;
using System.Collections.Generic;

namespace Statlyn.Data.Shortlists
{
    public enum ShortlistStatus
    {
        Longlist = 0,
        Watchlist = 1,
        ScoutFurther = 2,
        Shortlist = 3,
        StrongTarget = 4,
        DevelopmentTarget = 5,
        LoanTarget = 6,
        FreeAgentTarget = 7,
        Rejected = 8,
        BadFit = 9,
        TooRisky = 10,
        TooExpensive = 11,
        NotForRole = 12
    }

    public enum ShortlistPriority
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Urgent = 3
    }

    public enum ShortlistFollowUpAction
    {
        None = 0,
        ScoutAgain = 1,
        WatchMore = 2,
        CompareAlternatives = 3,
        CheckAvailability = 4,
        CheckWage = 5,
        CheckMedical = 6,
        CheckWorkPermit = 7,
        ReviewRoleFit = 8,
        Reject = 9
    }

    public sealed class ShortlistRecord
    {
        public ShortlistRecord(long id, string name, string description, DateTimeOffset createdAtUtc, DateTimeOffset updatedAtUtc, bool isArchived, int playerCount)
        {
            Id = id;
            Name = name ?? string.Empty;
            Description = description ?? string.Empty;
            CreatedAtUtc = createdAtUtc;
            UpdatedAtUtc = updatedAtUtc;
            IsArchived = isArchived;
            PlayerCount = playerCount;
        }

        public long Id { get; }

        public string Name { get; }

        public string Description { get; }

        public DateTimeOffset CreatedAtUtc { get; }

        public DateTimeOffset UpdatedAtUtc { get; }

        public bool IsArchived { get; }

        public int PlayerCount { get; }
    }

    public sealed class ShortlistPlayerRecord
    {
        public ShortlistPlayerRecord(
            long id,
            long shortlistId,
            long playerId,
            string statlynPlayerId,
            ShortlistStatus status,
            ShortlistPriority priority,
            ShortlistFollowUpAction followUpAction,
            string roleName,
            string recommendation,
            string addedReason,
            string userNote,
            DateTimeOffset addedAtUtc,
            DateTimeOffset updatedAtUtc)
        {
            Id = id;
            ShortlistId = shortlistId;
            PlayerId = playerId;
            StatlynPlayerId = statlynPlayerId ?? string.Empty;
            Status = status;
            Priority = priority;
            FollowUpAction = followUpAction;
            RoleName = roleName ?? string.Empty;
            Recommendation = recommendation ?? string.Empty;
            AddedReason = addedReason ?? string.Empty;
            UserNote = userNote ?? string.Empty;
            AddedAtUtc = addedAtUtc;
            UpdatedAtUtc = updatedAtUtc;
        }

        public long Id { get; }

        public long ShortlistId { get; }

        public long PlayerId { get; }

        public string StatlynPlayerId { get; }

        public ShortlistStatus Status { get; }

        public ShortlistPriority Priority { get; }

        public ShortlistFollowUpAction FollowUpAction { get; }

        public string RoleName { get; }

        public string Recommendation { get; }

        public string AddedReason { get; }

        public string UserNote { get; }

        public DateTimeOffset AddedAtUtc { get; }

        public DateTimeOffset UpdatedAtUtc { get; }
    }

    public sealed class ShortlistPlayerSummary
    {
        public ShortlistPlayerSummary(
            ShortlistPlayerRecord shortlistPlayer,
            string playerName,
            string age,
            string nationality,
            string position,
            string sourceName,
            string roleFit,
            string confidence,
            IReadOnlyList<string> keyOutputMetrics,
            int missingDataCount,
            int blockedFieldCount,
            bool isFixtureMode,
            bool isLiveFm26Data,
            IReadOnlyList<string> safeWarnings)
        {
            ShortlistPlayer = shortlistPlayer;
            PlayerName = playerName ?? string.Empty;
            Age = age ?? string.Empty;
            Nationality = nationality ?? string.Empty;
            Position = position ?? string.Empty;
            SourceName = sourceName ?? string.Empty;
            RoleFit = roleFit ?? string.Empty;
            Confidence = confidence ?? string.Empty;
            KeyOutputMetrics = keyOutputMetrics ?? new List<string>();
            MissingDataCount = missingDataCount;
            BlockedFieldCount = blockedFieldCount;
            IsFixtureMode = isFixtureMode;
            IsLiveFm26Data = isLiveFm26Data;
            SafeWarnings = safeWarnings ?? new List<string>();
        }

        public ShortlistPlayerRecord ShortlistPlayer { get; }

        public string PlayerName { get; }

        public string Age { get; }

        public string Nationality { get; }

        public string Position { get; }

        public string SourceName { get; }

        public string RoleFit { get; }

        public string Confidence { get; }

        public IReadOnlyList<string> KeyOutputMetrics { get; }

        public int MissingDataCount { get; }

        public int BlockedFieldCount { get; }

        public bool IsFixtureMode { get; }

        public bool IsLiveFm26Data { get; }

        public IReadOnlyList<string> SafeWarnings { get; }
    }

    public sealed class ShortlistCreateRequest
    {
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
    }

    public sealed class ShortlistAddPlayerRequest
    {
        public long? ShortlistId { get; set; }

        public string ShortlistName { get; set; } = string.Empty;

        public bool CreateShortlistIfMissing { get; set; } = true;

        public string StatlynPlayerId { get; set; } = string.Empty;

        public ShortlistStatus? Status { get; set; }

        public ShortlistPriority? Priority { get; set; }

        public ShortlistFollowUpAction? FollowUpAction { get; set; }

        public string RoleName { get; set; } = string.Empty;

        public string Recommendation { get; set; } = string.Empty;

        public string AddedReason { get; set; } = string.Empty;

        public string UserNote { get; set; } = string.Empty;
    }

    public sealed class ShortlistUpdatePlayerRequest
    {
        public long ShortlistPlayerId { get; set; }

        public ShortlistStatus Status { get; set; } = ShortlistStatus.Watchlist;

        public ShortlistPriority Priority { get; set; } = ShortlistPriority.Medium;

        public ShortlistFollowUpAction FollowUpAction { get; set; } = ShortlistFollowUpAction.None;

        public string RoleName { get; set; } = string.Empty;

        public string Recommendation { get; set; } = string.Empty;

        public string UserNote { get; set; } = string.Empty;
    }

    public sealed class ShortlistResult
    {
        public ShortlistResult(bool success, string safeMessage, ShortlistRecord shortlist, ShortlistPlayerRecord player, IReadOnlyList<string> warnings, IReadOnlyList<string> errors)
        {
            Success = success;
            SafeMessage = safeMessage ?? string.Empty;
            Shortlist = shortlist;
            Player = player;
            Warnings = warnings ?? new List<string>();
            Errors = errors ?? new List<string>();
        }

        public bool Success { get; }

        public string SafeMessage { get; }

        public ShortlistRecord Shortlist { get; }

        public ShortlistPlayerRecord Player { get; }

        public IReadOnlyList<string> Warnings { get; }

        public IReadOnlyList<string> Errors { get; }
    }

    public sealed class ShortlistWorkflowResult
    {
        public ShortlistWorkflowResult(
            bool success,
            string safeMessage,
            ShortlistRecord? shortlist,
            ShortlistPlayerSummary? playerSummary,
            IReadOnlyList<string> warnings,
            IReadOnlyList<string> errors)
        {
            Success = success;
            SafeMessage = safeMessage ?? string.Empty;
            Shortlist = shortlist;
            PlayerSummary = playerSummary;
            Warnings = warnings ?? new List<string>();
            Errors = errors ?? new List<string>();
        }

        public bool Success { get; }

        public string SafeMessage { get; }

        public ShortlistRecord? Shortlist { get; }

        public ShortlistPlayerSummary? PlayerSummary { get; }

        public IReadOnlyList<string> Warnings { get; }

        public IReadOnlyList<string> Errors { get; }

        public static ShortlistWorkflowResult Failure(string message, IReadOnlyList<string> errors)
        {
            return new ShortlistWorkflowResult(false, message, null, null, new List<string>(), errors ?? new[] { message });
        }
    }

    public sealed class ShortlistDecisionResult
    {
        public ShortlistDecisionResult(ShortlistStatus suggestedStatus, ShortlistPriority suggestedPriority, ShortlistFollowUpAction suggestedFollowUpAction, string safeReason, IReadOnlyList<string> warnings)
        {
            SuggestedStatus = suggestedStatus;
            SuggestedPriority = suggestedPriority;
            SuggestedFollowUpAction = suggestedFollowUpAction;
            SafeReason = safeReason ?? string.Empty;
            Warnings = warnings ?? new List<string>();
        }

        public ShortlistStatus SuggestedStatus { get; }

        public ShortlistPriority SuggestedPriority { get; }

        public ShortlistFollowUpAction SuggestedFollowUpAction { get; }

        public string SafeReason { get; }

        public IReadOnlyList<string> Warnings { get; }
    }
}
