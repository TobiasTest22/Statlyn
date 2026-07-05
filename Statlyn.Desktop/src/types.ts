export type AppHealthDto = {
  status: string;
  productMode: string;
  databasePath: string;
  schemaVersion: number;
  isFm26Supported: boolean;
  connectorStatus: string;
  validatedMapStatus: string;
  safeMessage: string;
};

export type Fm26ConnectorStatusDto = {
  isNativeConnectorAvailable: boolean;
  availability: string;
  connectorVersion: string;
  connectorBuildInfo: string;
  isWindows: boolean;
  isFmProcessDetected: boolean;
  detectionStatus: string;
  detectionStatusMessage: string;
  processDetectedAtUtc: string;
  processName: string;
  processId: number | null;
  executableFileName: string;
  executableDirectorySafeLabel: string;
  processPath: string;
  productName: string;
  productVersion: string;
  fileVersion: string;
  architecture: string;
  is64BitProcess: boolean | null;
  readOnlyAccessAttempted: boolean;
  hasReadOnlyAccess: boolean;
  readOnlyAccessStatus: string;
  requiredAccessLevel: string;
  isFm26Supported: boolean;
  buildSupportStatus: string;
  buildSupportMessage: string;
  mapSupportStatus: string;
  mapSupportMessage: string;
  supportStatusMessage: string;
  nextActionSafeMessage: string;
  lastErrorSafeMessage: string;
  generatedAtUtc: string;
  safeMessage: string;
  memoryMapRegistryStatus: string;
  selectedMemoryMapId: string;
  hasValidatedMap: boolean;
  memoryMapCount: number;
  usableMemoryMapCount: number;
  templateMemoryMapCount: number;
  invalidMemoryMapCount: number;
  warnings: string[];
  errors: string[];
};

export type MemoryMapDiagnosticDto = {
  mapId: string;
  displayName: string;
  gameVersion: string;
  buildNumber: string;
  platform: string;
  architecture: string;
  isTemplate: boolean;
  isValidated: boolean;
  isUsable: boolean;
  supportStatus: string;
  fieldCount: number;
  visibleFieldCount: number;
  hiddenFieldCountBlocked: number;
  safeMessage: string;
  validationWarnings: string[];
  validationErrors: string[];
};

export type MemoryMapRegistryDto = {
  registryStatus: string;
  mapsFoundCount: number;
  usableMapsCount: number;
  templateMapsCount: number;
  invalidMapsCount: number;
  hasValidatedMap: boolean;
  selectedMapId: string;
  selectedMapDisplayName: string;
  selectedMapStatus: string;
  mapSupportStatus: string;
  mapSupportMessage: string;
  nextActionSafeMessage: string;
  safeMessage: string;
  maps: MemoryMapDiagnosticDto[];
};

export type Fm26SelectedMapSummaryDto = {
  mapId: string;
  displayName: string;
  build: string;
  status: string;
};

export type Fm26SnapshotGateDto = {
  gateKey: string;
  label: string;
  gateStatus: string;
  snapshotStatus: string;
  safeMessage: string;
  nextAction: string;
};

export type Fm26SnapshotBlockReasonDto = {
  gateKey: string;
  reason: string;
  safeMessage: string;
  nextAction: string;
};

export type Fm26SnapshotDto = {
  snapshotId: string;
  generatedAtUtc: string;
  snapshotStatus: string;
  safeMessage: string;
  connectorStatus: string;
  isNativeConnectorAvailable: boolean;
  platformStatus: string;
  isWindows: boolean;
  fmProcessDetected: boolean;
  fmProcessStatus: string;
  processName: string;
  processId: number | null;
  productVersion: string;
  fileVersion: string;
  architecture: string;
  readOnlyStatus: string;
  mapRegistryStatus: string;
  mapsFound: number;
  validatedMaps: number;
  templateMaps: number;
  invalidMaps: number;
  selectedMapSummary: Fm26SelectedMapSummaryDto;
  allGatesPassed: boolean;
  blockingGate: string;
  isFm26Supported: boolean;
  isLiveReadingAvailable: boolean;
  readerStatus: string;
  fieldPolicyStatus: string;
  gates: Fm26SnapshotGateDto[];
  blockReasons: Fm26SnapshotBlockReasonDto[];
  nextAction: string;
  warnings: string[];
  errors: string[];
};

export type Fm26SnapshotSummaryDto = {
  snapshotId: string;
  generatedAtUtc: string;
  snapshotStatus: string;
  connectorStatus: string;
  processStatus: string;
  readOnlyStatus: string;
  mapRegistryStatus: string;
  blockingGate: string;
  liveReadingAllowed: boolean;
  nextAction: string;
  warningsCount: number;
  errorsCount: number;
};

export type Fm26PersistedSnapshotDto = {
  snapshotId: string;
  generatedAtUtc: string;
  snapshotStatus: string;
  safeMessage: string;
  connectorStatus: string;
  platformStatus: string;
  processDetected: boolean;
  processStatus: string;
  readOnlyStatus: string;
  mapRegistryStatus: string;
  mapsFound: number;
  validatedMaps: number;
  templateMaps: number;
  invalidMaps: number;
  selectedMapSummary: Fm26SelectedMapSummaryDto;
  allGatesPassed: boolean;
  blockingGate: string;
  liveReadingAllowed: boolean;
  nextAction: string;
  warningsCount: number;
  errorsCount: number;
  gates: Fm26SnapshotGateDto[];
};

export type Fm26SnapshotHistoryDto = {
  success: boolean;
  safeMessage: string;
  totalCount: number;
  latestSnapshot: Fm26PersistedSnapshotDto | null;
  snapshots: Fm26SnapshotSummaryDto[];
  warnings: string[];
  errors: string[];
};

export type Fm26SnapshotCreateResultDto = {
  success: boolean;
  safeMessage: string;
  snapshot: Fm26PersistedSnapshotDto | null;
  totalCount: number;
  warnings: string[];
  errors: string[];
};

export type DashboardOverviewDto = {
  safeMessage: string;
  databasePath: string;
  dataSourceCount: number;
  importedPlayersCount: number;
  shortlistCount: number;
  scoutAssignmentCount: number;
  roleLabTemplateCount: number;
  benchmarkDefinitionCount: number;
  localReadinessStatus: string;
  fm26Status: string;
};

export type PlayerListItemDto = {
  statlynPlayerId: string;
  displayName: string;
  age: string;
  nationality: string;
  positionGroup: string;
  primaryPosition: string;
  sourceName: string;
  sourceConfidence: number;
  dataCompleteness: number;
  roleName: string;
  roleFit: number | null;
  confidence: number | null;
  recommendation: string;
  missingDataCount: number;
  blockedFieldCount: number;
  benchmarkStatus: string;
  safeWarnings: string[];
};

export type PlayerIntelligenceReadinessDto = {
  available: boolean;
  safeMessage: string;
  importedPlayers: number;
  eventLocationRows: number;
  marketContextRows: number;
  teamStyleRows: number;
  leagueAverageRows: number;
  styleVectorRows: number;
  warnings: string[];
};

export type PlayerIntelligenceProfileDto = {
  available: boolean;
  safeMessage: string;
  statlynPlayerId: string;
  displayName: string;
  position: string;
  role: string;
  source: string;
  age: number | null;
  nationality: string;
  dataQuality: string;
  confidence: number;
  roleFit: number | null;
  warnings: string[];
  missingFields: string[];
};

export type PlayerRadarAxisDto = {
  axisKey: string;
  label: string;
  value: number | null;
  benchmarkValue: number | null;
  sourceMetric: string;
  dataQuality: string;
  confidence: number;
};

export type PlayerSkillRadarDto = {
  available: boolean;
  safeMessage: string;
  profileType: string;
  dataQuality: string;
  confidence: number;
  missingFields: string[];
  warnings: string[];
  axes: PlayerRadarAxisDto[];
};

export type PlayerPer90MetricDto = {
  metricKey: string;
  label: string;
  value: number;
  unit: string;
  minutes: number;
  dataQuality: string;
  confidence: number;
};

export type PlayerPer90SummaryDto = {
  available: boolean;
  safeMessage: string;
  dataQuality: string;
  confidence: number;
  missingFields: string[];
  warnings: string[];
  metrics: PlayerPer90MetricDto[];
};

export type PlayerHeatmapPointDto = {
  matchId: string;
  minute: number;
  x: number;
  y: number;
  actionType: string;
  confidence: number;
};

export type PlayerHeatmapDto = {
  available: boolean;
  safeMessage: string;
  dataQuality: string;
  confidence: number;
  missingFields: string[];
  warnings: string[];
  points: PlayerHeatmapPointDto[];
};

export type PlayerValueEstimateDto = {
  available: boolean;
  safeMessage: string;
  fairValueLow: number | null;
  fairValueMid: number | null;
  fairValueHigh: number | null;
  currency: string;
  valueIndex: number | null;
  confidence: number;
  dataQuality: string;
  keyValueDrivers: string[];
  keyDiscountDrivers: string[];
  missingInputs: string[];
  modelVersion: string;
};

export type PlayerFitProjectionDto = {
  available: boolean;
  safeMessage: string;
  dataQuality: string;
  confidence: number;
  roleFitSummary: string;
  teamStyleSummary: string;
  missingFields: string[];
  warnings: string[];
};

export type PlayerArchetypeDto = {
  available: boolean;
  safeMessage: string;
  archetype: string;
  dataQuality: string;
  confidence: number;
  evidenceMetrics: string[];
  missingFields: string[];
  warnings: string[];
};

export type SimilarPlayerCandidateDto = {
  statlynPlayerId: string;
  displayName: string;
  role: string;
  similarityScore: number;
  confidence: number;
  dataQuality: string;
};

export type PlayerSimilarityDto = {
  available: boolean;
  safeMessage: string;
  dataQuality: string;
  confidence: number;
  missingFields: string[];
  warnings: string[];
  candidates: SimilarPlayerCandidateDto[];
};

export type LeagueAverageComparisonDto = {
  available: boolean;
  safeMessage: string;
  leagueKey: string;
  comparisonGroup: string;
  sampleSize: number;
  dataQuality: string;
  confidence: number;
  missingFields: string[];
  warnings: string[];
  comparisons: PlayerRadarAxisDto[];
};

export type RoleParameterMetricDto = {
  metricKey: string;
  label: string;
  category: string;
  required: boolean;
  minimumMinutes: number;
};

export type RoleParameterDefinitionDto = {
  roleName: string;
  roleFamily: string;
  primaryMetrics: RoleParameterMetricDto[];
  secondaryMetrics: RoleParameterMetricDto[];
  riskMetrics: RoleParameterMetricDto[];
  styleTraits: string[];
  minimumMinutes: number;
  unavailableConditions: string[];
};

export type RoleSpecificAssessmentDto = {
  available: boolean;
  safeMessage: string;
  roleName: string;
  dataQuality: string;
  confidence: number;
  missingFields: string[];
  warnings: string[];
  definition: RoleParameterDefinitionDto | null;
};

export type PlayerIntelligenceDto = {
  profile: PlayerIntelligenceProfileDto;
  radar: PlayerSkillRadarDto;
  per90: PlayerPer90SummaryDto;
  heatmap: PlayerHeatmapDto;
  valueEstimate: PlayerValueEstimateDto;
  fitProjection: PlayerFitProjectionDto;
  archetype: PlayerArchetypeDto;
  similarPlayers: PlayerSimilarityDto;
  leagueComparison: LeagueAverageComparisonDto;
  roleAssessment: RoleSpecificAssessmentDto;
};

export type RecruitmentBoardDto = {
  safeMessage: string;
  totalPlayers: number;
  players: PlayerListItemDto[];
  recommendations: Array<{
    statlynPlayerId: string;
    playerName: string;
    recommendation: string;
    reason: string;
    roleFit: number | null;
    confidence: number | null;
  }>;
};

export type RoleLabSummaryDto = {
  safeMessage: string;
  roleCount: number;
  rolePairCount: number;
  phaseOptions: string[];
  roleNames: string[];
};

export type DataSourceStatusDto = {
  safeMessage: string;
  mode: string;
  dataSourceCount: number;
  fixtureStatus: string;
  importStatus: string;
  warnings: string[];
};

export type DiagnosticsDto = {
  safeSummary: string;
  success: boolean;
  databasePath: string;
  fixturePath: string;
  schemaVersion: number;
  importedPlayerCount: number;
  shortlistCount: number;
  scoutReportCount: number;
  roleLabTemplateCount: number;
  benchmarkDefinitionCount: number;
  fm26Status: string;
  warnings: string[];
  errors: string[];
};

export type ScoutReportSummaryDto = {
  statlynPlayerId: string;
  playerName: string;
  assignmentStatus: string;
  latestRecommendation: string;
  scoutConfidence: string;
  safeSummary: string;
  safeNotice: string;
};

export type ApiState = {
  health: AppHealthDto | null;
  dashboard: DashboardOverviewDto | null;
  board: RecruitmentBoardDto | null;
  roleLab: RoleLabSummaryDto | null;
  dataSources: DataSourceStatusDto | null;
  diagnostics: DiagnosticsDto | null;
  connectorStatus: Fm26ConnectorStatusDto | null;
  memoryMaps: MemoryMapRegistryDto | null;
  fm26Snapshot: Fm26SnapshotDto | null;
  fm26SnapshotHistory: Fm26SnapshotHistoryDto | null;
  playerIntelligenceReadiness: PlayerIntelligenceReadinessDto | null;
  scoutReports: ScoutReportSummaryDto[];
};
