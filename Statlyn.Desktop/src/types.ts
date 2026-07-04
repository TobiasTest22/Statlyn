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
  scoutReports: ScoutReportSummaryDto[];
};
