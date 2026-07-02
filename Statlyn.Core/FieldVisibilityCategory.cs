namespace Statlyn.Core
{
    public enum FieldVisibilityCategory
    {
        AlwaysVisible = 0,
        VisibleIfManagedClubPlayer = 1,
        VisibleIfScouted = 2,
        VisibleIfScoutKnowledgeThresholdMet = 3,
        VisibleAsEstimateOnly = 4,
        UserEnteredNote = 5,
        LicensedExternalData = 6,
        NeverVisible = 7,
        NeverScore = 8
    }
}
