using System.Collections.Generic;

namespace Statlyn.Data
{
    public static class StatlynDatabaseSchema
    {
        public static IReadOnlyList<string> CreateStatements
        {
            get
            {
                return new[]
                {
                    @"CREATE TABLE IF NOT EXISTS DataSource (
                        Id INTEGER PRIMARY KEY,
                        SourceName TEXT NOT NULL,
                        ProviderType TEXT NOT NULL,
                        LicenceStatus TEXT NOT NULL,
                        ImportedAtUtc TEXT NOT NULL,
                        Confidence INTEGER NOT NULL,
                        AllowedUsage TEXT NOT NULL,
                        IsLive INTEGER NOT NULL DEFAULT 0,
                        PermitsImages INTEGER NOT NULL DEFAULT 0,
                        PermitsFlags INTEGER NOT NULL DEFAULT 0,
                        DataCompleteness INTEGER NOT NULL DEFAULT 0
                    );",
                    @"CREATE TABLE IF NOT EXISTS FieldPolicyAudit (
                        Id INTEGER PRIMARY KEY,
                        FieldKey TEXT NOT NULL,
                        VisibilityCategory TEXT NOT NULL,
                        CanDisplay INTEGER NOT NULL,
                        CanScore INTEGER NOT NULL,
                        CanStore INTEGER NOT NULL,
                        RequiresScoutReport INTEGER NOT NULL,
                        MinimumScoutKnowledge INTEGER NOT NULL,
                        RequiresLicensedSource INTEGER NOT NULL,
                        IsRestricted INTEGER NOT NULL,
                        CreatedAtUtc TEXT NOT NULL
                    );",
                    @"CREATE TABLE IF NOT EXISTS Player (
                        Id INTEGER PRIMARY KEY,
                        StatlynPlayerId TEXT NOT NULL UNIQUE,
                        DisplayName TEXT NOT NULL,
                        Age INTEGER NULL,
                        Nationality TEXT NULL,
                        Club TEXT NULL,
                        PositionGroup TEXT NULL,
                        PrimaryPosition TEXT NULL,
                        PreferredFoot TEXT NULL,
                        Height TEXT NULL,
                        ContractEnd TEXT NULL,
                        WageDisplay TEXT NULL,
                        MarketValueDisplay TEXT NULL,
                        SourceName TEXT NOT NULL,
                        SourceConfidence INTEGER NOT NULL,
                        DataCompleteness INTEGER NOT NULL,
                        LastUpdatedUtc TEXT NOT NULL
                    );",
                    @"CREATE TABLE IF NOT EXISTS Team (
                        Id INTEGER PRIMARY KEY,
                        SourceTeamId TEXT NOT NULL,
                        Name TEXT NOT NULL,
                        Nation TEXT NULL,
                        League TEXT NULL,
                        SourceName TEXT NOT NULL,
                        LastUpdatedUtc TEXT NOT NULL
                    );",
                    @"CREATE TABLE IF NOT EXISTS Match (
                        Id INTEGER PRIMARY KEY,
                        SourceMatchId TEXT NOT NULL,
                        Competition TEXT NOT NULL,
                        MatchDateUtc TEXT NOT NULL,
                        HomeTeamId INTEGER NULL,
                        AwayTeamId INTEGER NULL,
                        SourceName TEXT NOT NULL,
                        FOREIGN KEY(HomeTeamId) REFERENCES Team(Id),
                        FOREIGN KEY(AwayTeamId) REFERENCES Team(Id)
                    );",
                    @"CREATE TABLE IF NOT EXISTS VisibleAttribute (
                        Id INTEGER PRIMARY KEY,
                        PlayerId INTEGER NOT NULL,
                        AttributeName TEXT NOT NULL,
                        AttributeValue INTEGER NULL,
                        IsKnown INTEGER NOT NULL,
                        Confidence INTEGER NOT NULL,
                        SourceName TEXT NOT NULL,
                        FOREIGN KEY(PlayerId) REFERENCES Player(Id)
                    );",
                    @"CREATE TABLE IF NOT EXISTS PlayerStat (
                        Id INTEGER PRIMARY KEY,
                        PlayerId INTEGER NOT NULL,
                        StatName TEXT NOT NULL,
                        StatValue REAL NOT NULL,
                        Minutes INTEGER NOT NULL,
                        SourceName TEXT NOT NULL,
                        Confidence INTEGER NOT NULL,
                        FOREIGN KEY(PlayerId) REFERENCES Player(Id)
                    );",
                    @"CREATE TABLE IF NOT EXISTS PlayerImageReference (
                        Id INTEGER PRIMARY KEY,
                        PlayerId INTEGER NOT NULL,
                        ImageReference TEXT NOT NULL,
                        LicenceStatus TEXT NOT NULL,
                        CanDisplay INTEGER NOT NULL,
                        SourceName TEXT NOT NULL,
                        FOREIGN KEY(PlayerId) REFERENCES Player(Id)
                    );",
                    @"CREATE TABLE IF NOT EXISTS NationalityFlagReference (
                        Id INTEGER PRIMARY KEY,
                        Nationality TEXT NOT NULL,
                        AssetKey TEXT NOT NULL,
                        CanDisplay INTEGER NOT NULL,
                        IsBundledSafeAsset INTEGER NOT NULL,
                        SourceName TEXT NOT NULL
                    );",
                    @"CREATE TABLE IF NOT EXISTS ScoutKnowledge (
                        Id INTEGER PRIMARY KEY,
                        PlayerId INTEGER NOT NULL,
                        KnowledgePercentage INTEGER NOT NULL,
                        HasScoutReport INTEGER NOT NULL,
                        LastUpdatedUtc TEXT NOT NULL,
                        FOREIGN KEY(PlayerId) REFERENCES Player(Id)
                    );",
                    @"CREATE TABLE IF NOT EXISTS ScoutReport (
                        Id INTEGER PRIMARY KEY,
                        PlayerId INTEGER NOT NULL,
                        ReportDateUtc TEXT NOT NULL,
                        RoleAssessed TEXT NOT NULL,
                        Strengths TEXT NULL,
                        Weaknesses TEXT NULL,
                        Risk TEXT NULL,
                        Recommendation TEXT NOT NULL,
                        Confidence INTEGER NOT NULL,
                        FollowUpAction TEXT NULL,
                        FOREIGN KEY(PlayerId) REFERENCES Player(Id)
                    );",
                    @"CREATE TABLE IF NOT EXISTS RoleModel (
                        Id INTEGER PRIMARY KEY,
                        RoleName TEXT NOT NULL,
                        PositionGroup TEXT NULL,
                        WeightingProfile TEXT NOT NULL,
                        ConfidenceRules TEXT NOT NULL,
                        EvidenceTemplates TEXT NOT NULL,
                        CreatedAtUtc TEXT NOT NULL
                    );",
                    @"CREATE TABLE IF NOT EXISTS RoleScore (
                        Id INTEGER PRIMARY KEY,
                        PlayerId INTEGER NOT NULL,
                        RoleModelId INTEGER NULL,
                        RoleFit INTEGER NOT NULL,
                        TechnicalFit INTEGER NOT NULL,
                        StatisticalFit INTEGER NOT NULL,
                        PhysicalFit INTEGER NOT NULL,
                        TacticalFit INTEGER NOT NULL,
                        RiskScore INTEGER NOT NULL,
                        Confidence INTEGER NOT NULL,
                        PositiveEvidence TEXT NOT NULL,
                        NegativeEvidence TEXT NOT NULL,
                        MissingData TEXT NOT NULL,
                        BlockedDataNotice TEXT NULL,
                        CreatedAtUtc TEXT NOT NULL,
                        FOREIGN KEY(PlayerId) REFERENCES Player(Id),
                        FOREIGN KEY(RoleModelId) REFERENCES RoleModel(Id)
                    );",
                    @"CREATE TABLE IF NOT EXISTS RecruitmentVerdict (
                        Id INTEGER PRIMARY KEY,
                        PlayerId INTEGER NOT NULL,
                        Recommendation TEXT NOT NULL,
                        RoleFit INTEGER NOT NULL,
                        TacticalFit INTEGER NULL,
                        SquadUpgrade INTEGER NULL,
                        TransferRealism INTEGER NULL,
                        ValueScore INTEGER NULL,
                        RiskScore INTEGER NULL,
                        Confidence INTEGER NOT NULL,
                        Evidence TEXT NOT NULL,
                        CreatedAtUtc TEXT NOT NULL,
                        FOREIGN KEY(PlayerId) REFERENCES Player(Id)
                    );",
                    @"CREATE TABLE IF NOT EXISTS UserNote (
                        Id INTEGER PRIMARY KEY,
                        PlayerId INTEGER NOT NULL,
                        NoteType TEXT NOT NULL,
                        Body TEXT NOT NULL,
                        CreatedAtUtc TEXT NOT NULL,
                        UpdatedAtUtc TEXT NOT NULL,
                        FOREIGN KEY(PlayerId) REFERENCES Player(Id)
                    );",
                    @"CREATE TABLE IF NOT EXISTS Shortlist (
                        Id INTEGER PRIMARY KEY,
                        Name TEXT NOT NULL,
                        Description TEXT NULL,
                        CreatedAtUtc TEXT NOT NULL
                    );",
                    @"CREATE TABLE IF NOT EXISTS ShortlistPlayer (
                        Id INTEGER PRIMARY KEY,
                        ShortlistId INTEGER NOT NULL,
                        PlayerId INTEGER NOT NULL,
                        Status TEXT NOT NULL,
                        AddedAtUtc TEXT NOT NULL,
                        FOREIGN KEY(ShortlistId) REFERENCES Shortlist(Id),
                        FOREIGN KEY(PlayerId) REFERENCES Player(Id)
                    );",
                    @"CREATE TABLE IF NOT EXISTS ImportAudit (
                        Id INTEGER PRIMARY KEY,
                        SourceName TEXT NOT NULL,
                        ProviderType TEXT NOT NULL,
                        ImportedAtUtc TEXT NOT NULL,
                        RowsRead INTEGER NOT NULL,
                        RowsAccepted INTEGER NOT NULL,
                        RowsRejected INTEGER NOT NULL,
                        Diagnostics TEXT NOT NULL
                    );",
                    @"CREATE TABLE IF NOT EXISTS BlockedFieldAudit (
                        Id INTEGER PRIMARY KEY,
                        SourceName TEXT NOT NULL,
                        SourceEntityId TEXT NOT NULL,
                        FieldKey TEXT NOT NULL,
                        FieldName TEXT NOT NULL,
                        Reason TEXT NOT NULL,
                        CreatedAtUtc TEXT NOT NULL
                    );",
                    @"CREATE TABLE IF NOT EXISTS DiagnosticsLog (
                        Id INTEGER PRIMARY KEY,
                        Key TEXT NOT NULL,
                        Status TEXT NOT NULL,
                        Message TEXT NOT NULL,
                        TechnicalDetail TEXT NULL,
                        CreatedAtUtc TEXT NOT NULL
                    );"
                };
            }
        }
    }
}
