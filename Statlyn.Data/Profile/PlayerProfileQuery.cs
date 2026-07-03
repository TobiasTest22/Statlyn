namespace Statlyn.Data.Profile
{
    public sealed class PlayerProfileQuery
    {
        public string StatlynPlayerId { get; set; } = string.Empty;

        public string OptionalRoleName { get; set; } = string.Empty;

        public string OptionalRoleOutputProfileName { get; set; } = string.Empty;

        public bool IncludeAttributes { get; set; } = true;

        public bool IncludeBlockedAudit { get; set; } = true;

        public bool IncludeVisualSections { get; set; } = true;
    }
}
