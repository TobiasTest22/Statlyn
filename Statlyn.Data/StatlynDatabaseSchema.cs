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
                        AllowedUsage TEXT NOT NULL
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
