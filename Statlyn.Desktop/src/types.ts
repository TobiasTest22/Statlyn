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
  scoutReports: ScoutReportSummaryDto[];
};
