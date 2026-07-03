using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Statlyn.Data.Shortlists
{
    public sealed class ShortlistsPageViewModel
    {
        public ShortlistsPageViewModel(
            IReadOnlyList<ShortlistCardViewModel> shortlists,
            ShortlistDetailViewModel selectedShortlist,
            ShortlistStatusViewModel statusOptions,
            string safeMessage,
            string databasePath)
        {
            Shortlists = shortlists ?? new List<ShortlistCardViewModel>();
            SelectedShortlist = selectedShortlist;
            StatusOptions = statusOptions;
            SafeMessage = safeMessage ?? string.Empty;
            DatabasePath = databasePath ?? string.Empty;
        }

        public IReadOnlyList<ShortlistCardViewModel> Shortlists { get; }

        public ShortlistDetailViewModel SelectedShortlist { get; }

        public ShortlistStatusViewModel StatusOptions { get; }

        public string SafeMessage { get; }

        public string DatabasePath { get; }
    }

    public sealed class ShortlistCardViewModel
    {
        public ShortlistCardViewModel(ShortlistRecord record)
        {
            ShortlistId = record == null ? 0 : record.Id;
            Name = record == null ? string.Empty : record.Name;
            Description = record == null ? string.Empty : record.Description;
            PlayerCount = record == null ? "0" : record.PlayerCount.ToString(CultureInfo.InvariantCulture);
            Status = record != null && record.IsArchived ? "Archived" : "Active";
            UpdatedAt = record == null ? string.Empty : record.UpdatedAtUtc.ToString("u", CultureInfo.InvariantCulture);
        }

        public long ShortlistId { get; }

        public string Name { get; }

        public string Description { get; }

        public string PlayerCount { get; }

        public string Status { get; }

        public string UpdatedAt { get; }
    }

    public sealed class ShortlistDetailViewModel
    {
        public ShortlistDetailViewModel(ShortlistRecord? record, IReadOnlyList<ShortlistPlayerRowViewModel> players)
        {
            ShortlistId = record == null ? 0 : record.Id;
            Name = record == null ? string.Empty : record.Name;
            Description = record == null ? string.Empty : record.Description;
            IsArchived = record != null && record.IsArchived;
            Players = players ?? new List<ShortlistPlayerRowViewModel>();
        }

        public long ShortlistId { get; }

        public string Name { get; }

        public string Description { get; }

        public bool IsArchived { get; }

        public IReadOnlyList<ShortlistPlayerRowViewModel> Players { get; }
    }

    public sealed class ShortlistPlayerRowViewModel
    {
        public ShortlistPlayerRowViewModel(ShortlistPlayerSummary summary)
        {
            ShortlistPlayerId = summary == null || summary.ShortlistPlayer == null ? 0 : summary.ShortlistPlayer.Id;
            StatlynPlayerId = summary == null || summary.ShortlistPlayer == null ? string.Empty : summary.ShortlistPlayer.StatlynPlayerId;
            PlayerName = summary == null ? string.Empty : summary.PlayerName;
            Age = summary == null ? "Unknown" : summary.Age;
            Nationality = summary == null ? "Unknown" : summary.Nationality;
            Position = summary == null ? "Unknown" : summary.Position;
            SourceName = summary == null ? string.Empty : summary.SourceName;
            RoleName = summary == null || summary.ShortlistPlayer == null ? string.Empty : summary.ShortlistPlayer.RoleName;
            RoleFit = summary == null ? "Not scored" : summary.RoleFit;
            Confidence = summary == null ? "Unknown" : summary.Confidence;
            Recommendation = summary == null || summary.ShortlistPlayer == null ? string.Empty : summary.ShortlistPlayer.Recommendation;
            Status = summary == null || summary.ShortlistPlayer == null ? string.Empty : summary.ShortlistPlayer.Status.ToString();
            Priority = summary == null || summary.ShortlistPlayer == null ? string.Empty : summary.ShortlistPlayer.Priority.ToString();
            FollowUpAction = summary == null || summary.ShortlistPlayer == null ? string.Empty : summary.ShortlistPlayer.FollowUpAction.ToString();
            AddedReason = summary == null || summary.ShortlistPlayer == null ? string.Empty : summary.ShortlistPlayer.AddedReason;
            UserNote = summary == null || summary.ShortlistPlayer == null ? string.Empty : summary.ShortlistPlayer.UserNote;
            KeyOutputMetrics = summary == null ? new List<string>() : summary.KeyOutputMetrics;
            MissingDataCount = summary == null ? 0 : summary.MissingDataCount;
            BlockedFieldCount = summary == null ? 0 : summary.BlockedFieldCount;
            IsFixtureMode = summary != null && summary.IsFixtureMode;
            IsLiveFm26Data = summary != null && summary.IsLiveFm26Data;
            SafeWarnings = summary == null ? new List<string>() : summary.SafeWarnings;
        }

        public long ShortlistPlayerId { get; }

        public string StatlynPlayerId { get; }

        public string PlayerName { get; }

        public string Age { get; }

        public string Nationality { get; }

        public string Position { get; }

        public string SourceName { get; }

        public string RoleName { get; }

        public string RoleFit { get; }

        public string Confidence { get; }

        public string Recommendation { get; }

        public string Status { get; }

        public string Priority { get; }

        public string FollowUpAction { get; }

        public string AddedReason { get; }

        public string UserNote { get; }

        public IReadOnlyList<string> KeyOutputMetrics { get; }

        public int MissingDataCount { get; }

        public int BlockedFieldCount { get; }

        public bool IsFixtureMode { get; }

        public bool IsLiveFm26Data { get; }

        public IReadOnlyList<string> SafeWarnings { get; }
    }

    public sealed class ShortlistActionViewModel
    {
        public ShortlistActionViewModel(string label, string value)
        {
            Label = label ?? string.Empty;
            Value = value ?? string.Empty;
        }

        public string Label { get; }

        public string Value { get; }
    }

    public sealed class ShortlistStatusViewModel
    {
        public ShortlistStatusViewModel()
        {
            Statuses = System.Enum.GetNames(typeof(ShortlistStatus)).ToList();
            Priorities = System.Enum.GetNames(typeof(ShortlistPriority)).ToList();
            FollowUpActions = System.Enum.GetNames(typeof(ShortlistFollowUpAction)).ToList();
        }

        public IReadOnlyList<string> Statuses { get; }

        public IReadOnlyList<string> Priorities { get; }

        public IReadOnlyList<string> FollowUpActions { get; }
    }
}
