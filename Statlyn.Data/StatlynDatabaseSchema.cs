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
                    @"CREATE TABLE IF NOT EXISTS SchemaVersion (
                        SchemaVersion INTEGER NOT NULL,
                        AppliedAtUtc TEXT NOT NULL
                    );",
                    @"CREATE TABLE IF NOT EXISTS DataSource (
                        Id INTEGER PRIMARY KEY,
                        SourceName TEXT NOT NULL,
                        ProviderType TEXT NOT NULL,
                        LicenceStatus TEXT NOT NULL,
                        ImportedAtUtc TEXT NOT NULL,
                        Confidence INTEGER NOT NULL,
                        AllowedUsage TEXT NOT NULL,
                        IsLive INTEGER NOT NULL DEFAULT 0,
                        PermitsPlayerImages INTEGER NOT NULL DEFAULT 0,
                        PermitsProviderFlags INTEGER NOT NULL DEFAULT 0,
                        UsesBundledSafeFlagAssets INTEGER NOT NULL DEFAULT 0,
                        PermitsClubBadges INTEGER NOT NULL DEFAULT 0,
                        AllowsExport INTEGER NOT NULL DEFAULT 0,
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
                    @"CREATE TABLE IF NOT EXISTS VisibleField (
                        Id INTEGER PRIMARY KEY,
                        PlayerId INTEGER NOT NULL,
                        FieldInstanceKey TEXT NOT NULL,
                        FieldKey TEXT NOT NULL,
                        FieldName TEXT NOT NULL,
                        SourceFieldName TEXT NOT NULL,
                        DisplayValue TEXT NULL,
                        NumericValue REAL NULL,
                        CanDisplay INTEGER NOT NULL,
                        CanScore INTEGER NOT NULL,
                        CanStore INTEGER NOT NULL,
                        Confidence INTEGER NOT NULL,
                        SourceName TEXT NOT NULL,
                        LastUpdatedUtc TEXT NOT NULL,
                        FOREIGN KEY(PlayerId) REFERENCES Player(Id)
                    );",
                    @"CREATE TABLE IF NOT EXISTS PlayerStat (
                        Id INTEGER PRIMARY KEY,
                        PlayerId INTEGER NOT NULL,
                        FieldInstanceKey TEXT NOT NULL,
                        StatName TEXT NOT NULL,
                        StatValue REAL NOT NULL,
                        Minutes INTEGER NOT NULL,
                        SampleMinutesMissing INTEGER NOT NULL DEFAULT 1,
                        MinutesSource TEXT NOT NULL DEFAULT 'missing',
                        SourceName TEXT NOT NULL,
                        Confidence INTEGER NOT NULL,
                        FOREIGN KEY(PlayerId) REFERENCES Player(Id)
                    );",
                    @"CREATE TABLE IF NOT EXISTS PhysicalMetric (
                        Id INTEGER PRIMARY KEY,
                        PlayerId INTEGER NOT NULL,
                        FieldInstanceKey TEXT NOT NULL,
                        MetricName TEXT NOT NULL,
                        MetricValue REAL NOT NULL,
                        Unit TEXT NULL,
                        SourceName TEXT NOT NULL,
                        Confidence INTEGER NOT NULL,
                        FOREIGN KEY(PlayerId) REFERENCES Player(Id)
                    );",
                    @"CREATE TABLE IF NOT EXISTS PlayerProfileSnapshot (
                        Id INTEGER PRIMARY KEY,
                        PlayerId INTEGER NOT NULL,
                        SourceName TEXT NOT NULL,
                        IsFixtureMode INTEGER NOT NULL,
                        IsLiveFm26Data INTEGER NOT NULL,
                        Confidence INTEGER NOT NULL,
                        DataCompleteness INTEGER NOT NULL,
                        CreatedAtUtc TEXT NOT NULL,
                        FOREIGN KEY(PlayerId) REFERENCES Player(Id)
                    );",
                    @"CREATE TABLE IF NOT EXISTS VisualProfileSnapshot (
                        Id INTEGER PRIMARY KEY,
                        PlayerProfileSnapshotId INTEGER NOT NULL,
                        VisualType TEXT NOT NULL,
                        VisualJson TEXT NOT NULL,
                        CreatedAtUtc TEXT NOT NULL,
                        FOREIGN KEY(PlayerProfileSnapshotId) REFERENCES PlayerProfileSnapshot(Id)
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
                    @"CREATE TABLE IF NOT EXISTS ScoutAssignment (
                        Id INTEGER PRIMARY KEY,
                        StatlynPlayerId TEXT NOT NULL,
                        ShortlistPlayerId INTEGER NULL,
                        ShortlistId INTEGER NULL,
                        PlayerId INTEGER NOT NULL,
                        AssignmentTitle TEXT NOT NULL,
                        RoleName TEXT NOT NULL,
                        PositionGroup TEXT NOT NULL,
                        Priority TEXT NOT NULL,
                        Status TEXT NOT NULL,
                        AssignedTo TEXT NOT NULL,
                        CreatedAtUtc TEXT NOT NULL,
                        DueAtUtc TEXT NULL,
                        UpdatedAtUtc TEXT NOT NULL,
                        ClosedAtUtc TEXT NULL,
                        SourceName TEXT NOT NULL,
                        IsArchived INTEGER NOT NULL DEFAULT 0,
                        FOREIGN KEY(PlayerId) REFERENCES Player(Id),
                        FOREIGN KEY(ShortlistPlayerId) REFERENCES ShortlistPlayer(Id),
                        FOREIGN KEY(ShortlistId) REFERENCES Shortlist(Id)
                    );",
                    @"CREATE TABLE IF NOT EXISTS ScoutReport (
                        Id INTEGER PRIMARY KEY,
                        AssignmentId INTEGER NULL,
                        PlayerId INTEGER NOT NULL,
                        StatlynPlayerId TEXT NOT NULL,
                        ReportDateUtc TEXT NOT NULL,
                        RoleAssessed TEXT NOT NULL,
                        TechnicalRating TEXT NOT NULL DEFAULT 'Unknown',
                        TacticalRating TEXT NOT NULL DEFAULT 'Unknown',
                        PhysicalRating TEXT NOT NULL DEFAULT 'Unknown',
                        MentalRating TEXT NOT NULL DEFAULT 'Unknown',
                        OverallRecommendation TEXT NOT NULL DEFAULT 'ScoutFurther',
                        Confidence INTEGER NOT NULL,
                        Strengths TEXT NOT NULL DEFAULT '',
                        Weaknesses TEXT NOT NULL DEFAULT '',
                        Risks TEXT NOT NULL DEFAULT '',
                        Risk TEXT NULL,
                        Recommendation TEXT NOT NULL DEFAULT 'ScoutFurther',
                        FollowUpAction TEXT NOT NULL DEFAULT 'None',
                        FinalSummary TEXT NOT NULL DEFAULT '',
                        CreatedAtUtc TEXT NOT NULL DEFAULT '',
                        UpdatedAtUtc TEXT NOT NULL DEFAULT '',
                        FOREIGN KEY(PlayerId) REFERENCES Player(Id),
                        FOREIGN KEY(AssignmentId) REFERENCES ScoutAssignment(Id)
                    );",
                    @"CREATE TABLE IF NOT EXISTS ScoutReportQuestion (
                        Id INTEGER PRIMARY KEY,
                        ReportId INTEGER NOT NULL,
                        Question TEXT NOT NULL,
                        Answer TEXT NOT NULL,
                        Category TEXT NOT NULL,
                        CreatedAtUtc TEXT NOT NULL,
                        FOREIGN KEY(ReportId) REFERENCES ScoutReport(Id)
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
                        RoleName TEXT NOT NULL,
                        RoleFit INTEGER NOT NULL,
                        TechnicalFit INTEGER NOT NULL,
                        StatisticalFit INTEGER NOT NULL,
                        PhysicalFit INTEGER NOT NULL,
                        TacticalFit INTEGER NULL,
                        RiskScore INTEGER NOT NULL,
                        Confidence INTEGER NOT NULL,
                        Recommendation TEXT NOT NULL DEFAULT 'ScoutFurther',
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
                        CreatedAtUtc TEXT NOT NULL,
                        UpdatedAtUtc TEXT NOT NULL,
                        IsArchived INTEGER NOT NULL DEFAULT 0
                    );",
                    @"CREATE TABLE IF NOT EXISTS ShortlistPlayer (
                        Id INTEGER PRIMARY KEY,
                        ShortlistId INTEGER NOT NULL,
                        PlayerId INTEGER NOT NULL,
                        StatlynPlayerId TEXT NOT NULL,
                        Status TEXT NOT NULL,
                        Priority TEXT NOT NULL,
                        FollowUpAction TEXT NOT NULL,
                        RoleName TEXT NOT NULL,
                        Recommendation TEXT NOT NULL,
                        AddedReason TEXT NOT NULL,
                        UserNote TEXT NOT NULL DEFAULT '',
                        AddedAtUtc TEXT NOT NULL,
                        UpdatedAtUtc TEXT NOT NULL,
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
                        FieldsStored INTEGER NOT NULL DEFAULT 0,
                        PlayerStatsStored INTEGER NOT NULL DEFAULT 0,
                        PhysicalMetricsStored INTEGER NOT NULL DEFAULT 0,
                        BlockedFields INTEGER NOT NULL DEFAULT 0,
                        UnknownFields INTEGER NOT NULL DEFAULT 0,
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
                    );",
                    @"CREATE TABLE IF NOT EXISTS fm26_snapshot_runs (
                        snapshot_id TEXT PRIMARY KEY,
                        generated_at_utc TEXT NOT NULL,
                        snapshot_status TEXT NOT NULL,
                        safe_message TEXT NOT NULL,
                        connector_availability TEXT NOT NULL,
                        platform_status TEXT NOT NULL,
                        process_detected INTEGER NOT NULL,
                        process_status TEXT NOT NULL,
                        process_name TEXT NULL,
                        process_id INTEGER NULL,
                        product_version TEXT NULL,
                        file_version TEXT NULL,
                        architecture TEXT NULL,
                        read_only_access_status TEXT NOT NULL,
                        memory_map_registry_status TEXT NOT NULL,
                        maps_found INTEGER NOT NULL,
                        validated_maps INTEGER NOT NULL,
                        template_maps INTEGER NOT NULL,
                        invalid_maps INTEGER NOT NULL,
                        selected_map_id TEXT NULL,
                        selected_map_display_name TEXT NULL,
                        selected_map_build TEXT NULL,
                        all_gates_passed INTEGER NOT NULL,
                        blocking_gate TEXT NULL,
                        live_reading_allowed INTEGER NOT NULL,
                        next_action_safe_message TEXT NOT NULL,
                        warning_count INTEGER NOT NULL,
                        error_count INTEGER NOT NULL
                    );",
                    @"CREATE TABLE IF NOT EXISTS fm26_snapshot_gate_results (
                        snapshot_id TEXT NOT NULL,
                        gate_key TEXT NOT NULL,
                        gate_name TEXT NOT NULL,
                        status TEXT NOT NULL,
                        safe_message TEXT NOT NULL,
                        sort_order INTEGER NOT NULL,
                        PRIMARY KEY(snapshot_id, gate_key),
                        FOREIGN KEY(snapshot_id) REFERENCES fm26_snapshot_runs(snapshot_id) ON DELETE CASCADE
                    );",
                    @"CREATE TABLE IF NOT EXISTS PerformanceMetricDefinition (
                        Id INTEGER PRIMARY KEY,
                        MetricKey TEXT NOT NULL UNIQUE,
                        DisplayName TEXT NOT NULL,
                        Description TEXT NOT NULL,
                        FieldKey TEXT NOT NULL,
                        FieldName TEXT NOT NULL,
                        ProviderType TEXT NOT NULL,
                        IsGenericFootballMetric INTEGER NOT NULL,
                        IsVerifiedFm26Metric INTEGER NOT NULL,
                        IsPer90Capable INTEGER NOT NULL,
                        DefaultUnit TEXT NOT NULL,
                        HigherIsBetter INTEGER NOT NULL,
                        LowerIsBetter INTEGER NOT NULL,
                        RequiresMinutes INTEGER NOT NULL,
                        MinimumMinutesRecommended INTEGER NOT NULL,
                        PositionGroups TEXT NOT NULL,
                        RoleFamilies TEXT NOT NULL,
                        SourceConfidenceRequired INTEGER NOT NULL,
                        CanScore INTEGER NOT NULL,
                        CanStore INTEGER NOT NULL,
                        Notes TEXT NOT NULL
                    );",
                    @"CREATE TABLE IF NOT EXISTS PerformanceMetricAlias (
                        Id INTEGER PRIMARY KEY,
                        MetricKey TEXT NOT NULL,
                        ProviderType TEXT NOT NULL,
                        AliasName TEXT NOT NULL,
                        IsVerifiedFm26Alias INTEGER NOT NULL DEFAULT 0,
                        Notes TEXT NOT NULL,
                        FOREIGN KEY(MetricKey) REFERENCES PerformanceMetricDefinition(MetricKey)
                    );",
                    @"CREATE TABLE IF NOT EXISTS ProviderMetricMapping (
                        Id INTEGER PRIMARY KEY,
                        MetricKey TEXT NOT NULL,
                        ProviderType TEXT NOT NULL,
                        ProviderFieldName TEXT NOT NULL,
                        IsVerifiedFm26Mapping INTEGER NOT NULL DEFAULT 0,
                        Notes TEXT NOT NULL,
                        FOREIGN KEY(MetricKey) REFERENCES PerformanceMetricDefinition(MetricKey)
                    );",
                    @"CREATE TABLE IF NOT EXISTS RoleOutputExpectationProfile (
                        Id INTEGER PRIMARY KEY,
                        ProfileName TEXT NOT NULL UNIQUE,
                        PositionGroup TEXT NOT NULL,
                        RoleFamily TEXT NOT NULL,
                        TacticalPhase TEXT NULL,
                        IsFm26Specific INTEGER NOT NULL,
                        IsGenericTemplate INTEGER NOT NULL,
                        AttributeSupportWeights TEXT NOT NULL,
                        ScoutQuestionPrompts TEXT NOT NULL,
                        RedFlagRules TEXT NOT NULL,
                        MinimumSampleRules TEXT NOT NULL,
                        Notes TEXT NOT NULL
                    );",
                    @"CREATE TABLE IF NOT EXISTS RoleOutputMetricExpectation (
                        Id INTEGER PRIMARY KEY,
                        ProfileName TEXT NOT NULL,
                        MetricKey TEXT NOT NULL,
                        FieldName TEXT NOT NULL,
                        Weight REAL NOT NULL,
                        Importance TEXT NOT NULL,
                        Direction TEXT NOT NULL,
                        MinimumSampleMinutes INTEGER NOT NULL,
                        Per90Required INTEGER NOT NULL,
                        NormalizationHint TEXT NOT NULL,
                        EvidenceTemplate TEXT NOT NULL,
                        MissingDataImpact TEXT NOT NULL,
                        FOREIGN KEY(ProfileName) REFERENCES RoleOutputExpectationProfile(ProfileName)
                    );",
                    @"CREATE TABLE IF NOT EXISTS TacticalRole (
                        Id INTEGER PRIMARY KEY,
                        RoleName TEXT NOT NULL,
                        TacticalPhase TEXT NOT NULL,
                        RoleFamily TEXT NOT NULL,
                        Source TEXT NOT NULL,
                        IsOfficialFm26Role INTEGER NOT NULL DEFAULT 0,
                        Fm26RoleId TEXT NULL,
                        PositionGroup TEXT NOT NULL,
                        ValidSlots TEXT NOT NULL,
                        MovementBehaviour TEXT NOT NULL,
                        BuildUpBehaviour TEXT NOT NULL,
                        FinalThirdBehaviour TEXT NOT NULL,
                        PressingBehaviour TEXT NOT NULL,
                        DefensiveBlockBehaviour TEXT NOT NULL,
                        TransitionBehaviour TEXT NOT NULL,
                        CreatedAtUtc TEXT NOT NULL,
                        UpdatedAtUtc TEXT NOT NULL,
                        IsArchived INTEGER NOT NULL DEFAULT 0
                    );",
                    @"CREATE TABLE IF NOT EXISTS TacticalRolePair (
                        Id INTEGER PRIMARY KEY,
                        PairName TEXT NOT NULL,
                        InPossessionRoleId INTEGER NOT NULL,
                        OutOfPossessionRoleId INTEGER NOT NULL,
                        InPossessionSlot TEXT NOT NULL,
                        OutOfPossessionSlot TEXT NOT NULL,
                        InPossessionFormation TEXT NOT NULL,
                        OutOfPossessionFormation TEXT NOT NULL,
                        TransitionComplexityScore INTEGER NOT NULL,
                        TacticalRiskScore INTEGER NOT NULL,
                        PositionalFamiliarityNeed TEXT NOT NULL,
                        CreatedAtUtc TEXT NOT NULL,
                        UpdatedAtUtc TEXT NOT NULL,
                        IsArchived INTEGER NOT NULL DEFAULT 0,
                        FOREIGN KEY(InPossessionRoleId) REFERENCES TacticalRole(Id),
                        FOREIGN KEY(OutOfPossessionRoleId) REFERENCES TacticalRole(Id)
                    );",
                    @"CREATE TABLE IF NOT EXISTS RoleOutputMetricRequirement (
                        Id INTEGER PRIMARY KEY,
                        TacticalRoleId INTEGER NULL,
                        RolePairId INTEGER NULL,
                        MetricKey TEXT NOT NULL,
                        FieldName TEXT NOT NULL,
                        Weight REAL NOT NULL,
                        Importance TEXT NOT NULL,
                        Direction TEXT NOT NULL,
                        MinimumSampleMinutes INTEGER NOT NULL,
                        Per90Required INTEGER NOT NULL,
                        NormalizationHint TEXT NOT NULL,
                        EvidenceTemplate TEXT NOT NULL,
                        MissingDataImpact TEXT NOT NULL,
                        FOREIGN KEY(TacticalRoleId) REFERENCES TacticalRole(Id),
                        FOREIGN KEY(RolePairId) REFERENCES TacticalRolePair(Id)
                    );",
                    @"CREATE TABLE IF NOT EXISTS RoleScoutQuestion (
                        Id INTEGER PRIMARY KEY,
                        TacticalRoleId INTEGER NULL,
                        RolePairId INTEGER NULL,
                        Category TEXT NOT NULL,
                        Question TEXT NOT NULL,
                        WhyItMatters TEXT NOT NULL,
                        SuggestedObservationType TEXT NOT NULL,
                        FOREIGN KEY(TacticalRoleId) REFERENCES TacticalRole(Id),
                        FOREIGN KEY(RolePairId) REFERENCES TacticalRolePair(Id)
                    );",
                    @"CREATE TABLE IF NOT EXISTS RoleRedFlag (
                        Id INTEGER PRIMARY KEY,
                        TacticalRoleId INTEGER NULL,
                        RolePairId INTEGER NULL,
                        FieldName TEXT NOT NULL,
                        Operator TEXT NOT NULL,
                        Threshold TEXT NOT NULL,
                        Message TEXT NOT NULL,
                        AppliesToPhase TEXT NOT NULL,
                        FOREIGN KEY(TacticalRoleId) REFERENCES TacticalRole(Id),
                        FOREIGN KEY(RolePairId) REFERENCES TacticalRolePair(Id)
                    );",
                    @"CREATE TABLE IF NOT EXISTS BenchmarkDefinition (
                        Id INTEGER PRIMARY KEY,
                        BenchmarkName TEXT NOT NULL,
                        Scope TEXT NOT NULL,
                        SourceName TEXT NOT NULL,
                        PositionGroup TEXT NOT NULL,
                        RoleProfileName TEXT NOT NULL,
                        TacticalRoleName TEXT NOT NULL,
                        TacticalRolePairName TEXT NOT NULL,
                        MetricKeys TEXT NOT NULL,
                        MinimumSampleSize INTEGER NOT NULL,
                        MinimumMinutes INTEGER NOT NULL,
                        IncludeFixtureData INTEGER NOT NULL,
                        CreatedAtUtc TEXT NOT NULL,
                        UpdatedAtUtc TEXT NOT NULL,
                        IsArchived INTEGER NOT NULL DEFAULT 0
                    );",
                    @"CREATE TABLE IF NOT EXISTS BenchmarkRun (
                        Id INTEGER PRIMARY KEY,
                        BenchmarkDefinitionId INTEGER NOT NULL,
                        RanAtUtc TEXT NOT NULL,
                        PlayerCount INTEGER NOT NULL,
                        MetricCount INTEGER NOT NULL,
                        SafeMessage TEXT NOT NULL,
                        FOREIGN KEY(BenchmarkDefinitionId) REFERENCES BenchmarkDefinition(Id)
                    );",
                    @"CREATE TABLE IF NOT EXISTS BenchmarkMetricSnapshot (
                        Id INTEGER PRIMARY KEY,
                        BenchmarkRunId INTEGER NOT NULL,
                        MetricKey TEXT NOT NULL,
                        FieldName TEXT NOT NULL,
                        MetricType TEXT NOT NULL,
                        SampleSize INTEGER NOT NULL,
                        MedianValue REAL NULL,
                        AverageValue REAL NULL,
                        MinimumValue REAL NULL,
                        MaximumValue REAL NULL,
                        SourceName TEXT NOT NULL,
                        ComparisonGroup TEXT NOT NULL,
                        IsGenericImportMetric INTEGER NOT NULL,
                        IsVerifiedFm26Metric INTEGER NOT NULL DEFAULT 0,
                        FOREIGN KEY(BenchmarkRunId) REFERENCES BenchmarkRun(Id)
                    );",
                    @"CREATE UNIQUE INDEX IF NOT EXISTS UX_VisibleField_Player_FieldInstance
                        ON VisibleField (PlayerId, FieldInstanceKey);",
                    @"CREATE UNIQUE INDEX IF NOT EXISTS UX_PlayerStat_Player_FieldInstance
                        ON PlayerStat (PlayerId, FieldInstanceKey);",
                    @"CREATE UNIQUE INDEX IF NOT EXISTS UX_PhysicalMetric_Player_FieldInstance
                        ON PhysicalMetric (PlayerId, FieldInstanceKey);",
                    @"CREATE INDEX IF NOT EXISTS IX_DataSource_SourceName_ImportedAtUtc
                        ON DataSource (SourceName, ImportedAtUtc);",
                    @"CREATE INDEX IF NOT EXISTS IX_RoleScore_Player_RoleModel_CreatedAt
                        ON RoleScore (PlayerId, RoleModelId, CreatedAtUtc);",
                    @"CREATE UNIQUE INDEX IF NOT EXISTS UX_BlockedFieldAudit_Entity_Field
                        ON BlockedFieldAudit (SourceEntityId, FieldKey, FieldName);",
                    @"CREATE INDEX IF NOT EXISTS IX_ScoutAssignment_StatlynPlayerId
                        ON ScoutAssignment (StatlynPlayerId);",
                    @"CREATE INDEX IF NOT EXISTS IX_ScoutAssignment_Status
                        ON ScoutAssignment (Status);",
                    @"CREATE INDEX IF NOT EXISTS IX_ScoutAssignment_ShortlistId
                        ON ScoutAssignment (ShortlistId);",
                    @"CREATE INDEX IF NOT EXISTS IX_ScoutReport_StatlynPlayerId
                        ON ScoutReport (StatlynPlayerId);",
                    @"CREATE INDEX IF NOT EXISTS IX_ScoutReport_AssignmentId
                        ON ScoutReport (AssignmentId);",
                    @"CREATE INDEX IF NOT EXISTS IX_ScoutReport_ReportDateUtc
                        ON ScoutReport (ReportDateUtc);",
                    @"CREATE INDEX IF NOT EXISTS IX_TacticalRole_RoleName
                        ON TacticalRole (RoleName);",
                    @"CREATE INDEX IF NOT EXISTS IX_TacticalRole_TacticalPhase
                        ON TacticalRole (TacticalPhase);",
                    @"CREATE INDEX IF NOT EXISTS IX_TacticalRole_RoleFamily
                        ON TacticalRole (RoleFamily);",
                    @"CREATE INDEX IF NOT EXISTS IX_TacticalRole_Source
                        ON TacticalRole (Source);",
                    @"CREATE INDEX IF NOT EXISTS IX_TacticalRolePair_PairName
                        ON TacticalRolePair (PairName);",
                    @"CREATE INDEX IF NOT EXISTS IX_RoleOutputMetricRequirement_TacticalRoleId
                        ON RoleOutputMetricRequirement (TacticalRoleId);",
                    @"CREATE INDEX IF NOT EXISTS IX_RoleOutputMetricRequirement_RolePairId
                        ON RoleOutputMetricRequirement (RolePairId);",
                    @"CREATE INDEX IF NOT EXISTS IX_BenchmarkDefinition_BenchmarkName
                        ON BenchmarkDefinition (BenchmarkName);",
                    @"CREATE INDEX IF NOT EXISTS IX_BenchmarkDefinition_Scope
                        ON BenchmarkDefinition (Scope);",
                    @"CREATE INDEX IF NOT EXISTS IX_BenchmarkDefinition_PositionGroup
                        ON BenchmarkDefinition (PositionGroup);",
                    @"CREATE INDEX IF NOT EXISTS IX_BenchmarkRun_BenchmarkDefinitionId
                        ON BenchmarkRun (BenchmarkDefinitionId);",
                    @"CREATE INDEX IF NOT EXISTS IX_BenchmarkMetricSnapshot_BenchmarkRunId
                        ON BenchmarkMetricSnapshot (BenchmarkRunId);",
                    @"CREATE INDEX IF NOT EXISTS IX_BenchmarkMetricSnapshot_MetricKey
                        ON BenchmarkMetricSnapshot (MetricKey);",
                    @"CREATE INDEX IF NOT EXISTS IX_fm26_snapshot_runs_generated_at_utc
                        ON fm26_snapshot_runs (generated_at_utc);",
                    @"CREATE INDEX IF NOT EXISTS IX_fm26_snapshot_gate_results_snapshot_id
                        ON fm26_snapshot_gate_results (snapshot_id, sort_order);",
                    @"CREATE INDEX IF NOT EXISTS IX_Shortlist_Name
                        ON Shortlist (Name);",
                    @"CREATE INDEX IF NOT EXISTS IX_ShortlistPlayer_ShortlistId
                        ON ShortlistPlayer (ShortlistId);",
                    @"CREATE INDEX IF NOT EXISTS IX_ShortlistPlayer_StatlynPlayerId
                        ON ShortlistPlayer (StatlynPlayerId);",
                    @"CREATE UNIQUE INDEX IF NOT EXISTS UX_ShortlistPlayer_Shortlist_StatlynPlayer
                        ON ShortlistPlayer (ShortlistId, StatlynPlayerId);"
                };
            }
        }
    }
}
