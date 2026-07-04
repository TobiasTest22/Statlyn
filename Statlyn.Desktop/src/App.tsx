import { useCallback, useEffect, useMemo, useState } from "react";
import { apiBaseUrl, loadWorkspace } from "./api";
import type { ApiState, PlayerListItemDto } from "./types";

type SectionName =
  | "Dashboard"
  | "Recruitment Board"
  | "Player Profile"
  | "Role Lab"
  | "Squad Gaps"
  | "Comparisons"
  | "Scout Reports"
  | "Tactical Lab"
  | "Data Sources"
  | "Diagnostics";

type Tone = "success" | "warning" | "danger" | "info" | "muted";

type DiagnosticLedgerRow = {
  check: string;
  status: string;
  value: string;
  message: string;
  tone: Tone;
};

type MatrixItem = {
  label: string;
  value: string;
  tone: Tone;
};

type DistributionSegment = {
  label: string;
  value: number;
  tone: Tone;
};

const navItems: SectionName[] = [
  "Dashboard",
  "Recruitment Board",
  "Player Profile",
  "Role Lab",
  "Squad Gaps",
  "Comparisons",
  "Scout Reports",
  "Tactical Lab",
  "Data Sources",
  "Diagnostics"
];

const brandAssets = {
  markWhite: "/branding/statlyn-mark-white.png",
  wordmarkWhite: "/branding/Statlyn_Logo_White-text.png"
};

const emptyState: ApiState = {
  health: null,
  dashboard: null,
  board: null,
  roleLab: null,
  dataSources: null,
  diagnostics: null,
  connectorStatus: null,
  memoryMaps: null,
  scoutReports: []
};

export default function App() {
  const [activeSection, setActiveSection] = useState<SectionName>("Recruitment Board");
  const [apiState, setApiState] = useState<ApiState>(emptyState);
  const [selectedPlayerId, setSelectedPlayerId] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState("");
  const [positionFilter, setPositionFilter] = useState("All");
  const [recommendationFilter, setRecommendationFilter] = useState("All");
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const refreshWorkspace = useCallback((shouldApply: () => boolean = () => true) => {
    setIsLoading(true);
    setError(null);

    loadWorkspace()
      .then((state) => {
        if (shouldApply()) {
          setApiState(state);
        }
      })
      .catch((caught: unknown) => {
        if (shouldApply()) {
          setApiState(emptyState);
          setError(caught instanceof Error ? caught.message : "Could not load safely.");
        }
      })
      .finally(() => {
        if (shouldApply()) {
          setIsLoading(false);
        }
      });
  }, []);

  useEffect(() => {
    let isMounted = true;
    refreshWorkspace(() => isMounted);
    return () => {
      isMounted = false;
    };
  }, [refreshWorkspace]);

  const players = apiState.board?.players ?? [];
  const positionOptions = useMemo(
    () => ["All", ...uniqueSorted(players.map((player) => player.primaryPosition || player.positionGroup))],
    [players]
  );
  const recommendationOptions = useMemo(
    () => ["All", ...uniqueSorted(players.map((player) => player.recommendation))],
    [players]
  );
  const visiblePlayers = useMemo(
    () => filterPlayers(players, searchTerm, positionFilter, recommendationFilter),
    [players, positionFilter, recommendationFilter, searchTerm]
  );
  const selectedPlayer = useMemo(
    () => visiblePlayers.find((player) => player.statlynPlayerId === selectedPlayerId) ?? visiblePlayers[0] ?? null,
    [visiblePlayers, selectedPlayerId]
  );
  const hasActiveFilters = searchTerm.trim().length > 0 || positionFilter !== "All" || recommendationFilter !== "All";
  const showsScoutControls =
    activeSection === "Recruitment Board" || activeSection === "Player Profile" || activeSection === "Scout Reports";

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand">
          <img className="brand-mark" src={brandAssets.markWhite} alt="Statlyn" />
          <img className="brand-wordmark" src={brandAssets.wordmarkWhite} alt="" aria-hidden="true" />
        </div>
        <nav aria-label="Primary workspace">
          {navItems.map((item) => (
            <button
              key={item}
              className={item === activeSection ? "active" : ""}
              type="button"
              onClick={() => setActiveSection(item)}
            >
              <span>{navLabel(item)}</span>
            </button>
          ))}
        </nav>
        <div className="sidebar-footer">
          <MiniStatus label="Local API" value={error ? "Offline" : apiState.health ? "Connected" : "Checking"} />
          <MiniStatus label="FM26" value="No live data" />
        </div>
      </aside>

      <main className="workspace">
        <header className="topbar">
          <div>
            <h1>{sectionTitle(activeSection)}</h1>
            <p>{sectionSummary(activeSection, apiState, visiblePlayers.length, players.length, hasActiveFilters)}</p>
          </div>
          <div className="top-actions" aria-label="Workspace actions and status">
            <button className="action-button" type="button" onClick={() => refreshWorkspace()}>
              Refresh
            </button>
            <div className="status-strip" aria-label="Workspace status">
              <StatusPill label="API" tone={error ? "danger" : apiState.health ? "success" : "muted"} value={error ? "Offline" : apiState.health ? "Connected" : "Checking"} />
              <StatusPill label="Data" tone={apiState.dashboard && apiState.dashboard.importedPlayersCount > 0 ? "success" : "muted"} value={apiState.dataSources?.mode ?? "Local CSV"} />
              <StatusPill label="FM26" tone="warning" value={apiState.connectorStatus?.mapSupportStatus ?? "Unsupported"} />
            </div>
          </div>
        </header>

        {showsScoutControls ? (
          <AnalystControls
            searchTerm={searchTerm}
            onSearchTermChange={setSearchTerm}
            positionFilter={positionFilter}
            onPositionFilterChange={setPositionFilter}
            positionOptions={positionOptions}
            recommendationFilter={recommendationFilter}
            onRecommendationFilterChange={setRecommendationFilter}
            recommendationOptions={recommendationOptions}
            totalPlayers={players.length}
            visiblePlayers={visiblePlayers.length}
            dataSourceMode={apiState.dataSources?.mode ?? "Local CSV"}
            hasActiveFilters={hasActiveFilters}
          />
        ) : null}

        {isLoading ? <LoadingState /> : null}
        {!isLoading && error ? <ErrorState message={error} onRetry={refreshWorkspace} /> : null}
        {!isLoading && !error ? (
          <WorkspaceContent
            activeSection={activeSection}
            state={apiState}
            players={visiblePlayers}
            selectedPlayerId={selectedPlayer?.statlynPlayerId ?? null}
            onSelectPlayer={setSelectedPlayerId}
          />
        ) : null}
      </main>
    </div>
  );
}

function AnalystControls({
  searchTerm,
  onSearchTermChange,
  positionFilter,
  onPositionFilterChange,
  positionOptions,
  recommendationFilter,
  onRecommendationFilterChange,
  recommendationOptions,
  totalPlayers,
  visiblePlayers,
  dataSourceMode,
  hasActiveFilters
}: {
  searchTerm: string;
  onSearchTermChange: (value: string) => void;
  positionFilter: string;
  onPositionFilterChange: (value: string) => void;
  positionOptions: string[];
  recommendationFilter: string;
  onRecommendationFilterChange: (value: string) => void;
  recommendationOptions: string[];
  totalPlayers: number;
  visiblePlayers: number;
  dataSourceMode: string;
  hasActiveFilters: boolean;
}) {
  return (
    <section className="analyst-controls" aria-label="Recruitment board search and filters">
      <label className="search-control">
        <span>Search</span>
        <input
          type="search"
          value={searchTerm}
          placeholder="Search player, position, source or recommendation..."
          onChange={(event) => onSearchTermChange(event.target.value)}
        />
      </label>
      <label className="filter-control">
        <span>Position</span>
        <select value={positionFilter} onChange={(event) => onPositionFilterChange(event.target.value)}>
          {positionOptions.map((option) => (
            <option key={option} value={option}>
              {option}
            </option>
          ))}
        </select>
      </label>
      <label className="filter-control">
        <span>Recommendation</span>
        <select value={recommendationFilter} onChange={(event) => onRecommendationFilterChange(event.target.value)}>
          {recommendationOptions.map((option) => (
            <option key={option} value={option}>
              {option}
            </option>
          ))}
        </select>
      </label>
      <div className="filter-status">
        <span>Rows</span>
        <strong>{totalPlayers === 0 ? "No local data" : `${visiblePlayers} / ${totalPlayers}`}</strong>
      </div>
      <div className="filter-status">
        <span>Source</span>
        <strong>{dataSourceMode}</strong>
      </div>
      <div className="filter-status warning">
        <span>FM26</span>
        <strong>Unsupported</strong>
      </div>
      {hasActiveFilters ? (
        <button className="clear-filters-button" type="button" onClick={() => {
          onSearchTermChange("");
          onPositionFilterChange("All");
          onRecommendationFilterChange("All");
        }}>
          Clear filters
        </button>
      ) : null}
    </section>
  );
}

function WorkspaceContent({
  activeSection,
  state,
  players,
  selectedPlayerId,
  onSelectPlayer
}: {
  activeSection: SectionName;
  state: ApiState;
  players: PlayerListItemDto[];
  selectedPlayerId: string | null;
  onSelectPlayer: (id: string) => void;
}) {
  if (activeSection === "Recruitment Board") {
    return (
      <section className="board-workspace">
        <BoardStatsPanel state={state} visiblePlayers={players.length} />
        <section className="content-grid board-grid">
          <RecruitmentPanel players={players} selectedPlayerId={selectedPlayerId} onSelectPlayer={onSelectPlayer} />
          <ScoutReportsPanel state={state} />
          <DiagnosticsPanel state={state} />
        </section>
      </section>
    );
  }

  if (activeSection === "Player Profile") {
    return (
      <section className="content-grid">
        <PlayerProfilePanel player={players.find((player) => player.statlynPlayerId === selectedPlayerId) ?? players[0] ?? null} />
        <RecruitmentPanel players={players} selectedPlayerId={selectedPlayerId} onSelectPlayer={onSelectPlayer} compact />
      </section>
    );
  }

  if (activeSection === "Role Lab") {
    return (
      <section className="content-grid">
        <RoleLabPanel state={state} />
        <EmptyWorkflowPanel title="Role Output" text="Role templates are generic/import-safe until FM26 validation exists." />
        <DiagnosticsPanel state={state} />
      </section>
    );
  }

  if (activeSection === "Data Sources") {
    return (
      <section className="content-grid">
        <DataSourcesPanel state={state} wide />
        <MemoryMapRegistryPanel state={state} wide />
        <ConnectorStatusPanel state={state} />
        <DiagnosticsPanel state={state} />
      </section>
    );
  }

  if (activeSection === "Diagnostics") {
    return (
      <section className="content-grid">
        <DiagnosticsPanel state={state} wide />
        <MemoryMapRegistryPanel state={state} wide />
        <ConnectorStatusPanel state={state} wide />
        <DataSourcesPanel state={state} />
      </section>
    );
  }

  if (activeSection === "Scout Reports") {
    return (
      <section className="content-grid">
        <ScoutReportsPanel state={state} wide />
        <RecruitmentPanel players={players} selectedPlayerId={selectedPlayerId} onSelectPlayer={onSelectPlayer} compact />
      </section>
    );
  }

  if (activeSection === "Squad Gaps" || activeSection === "Comparisons" || activeSection === "Tactical Lab") {
    return (
      <section className="content-grid">
        <EmptyWorkflowPanel title={activeSection} text={placeholderText(activeSection)} wide />
        <DashboardPanel state={state} />
      </section>
    );
  }

  return (
    <section className="content-grid">
      <DashboardPanel state={state} wide />
      <RecruitmentPanel players={players} selectedPlayerId={selectedPlayerId} onSelectPlayer={onSelectPlayer} />
      <ConnectorStatusPanel state={state} />
      <MemoryMapRegistryPanel state={state} />
      <DataSourcesPanel state={state} />
      <DiagnosticsPanel state={state} />
      <ScoutReportsPanel state={state} />
    </section>
  );
}

function DashboardPanel({ state, wide = false }: { state: ApiState; wide?: boolean }) {
  const dashboard = state.dashboard;
  const connector = state.connectorStatus;
  const registry = state.memoryMaps;
  return (
    <section className={`panel cockpit-panel ${wide ? "wide" : ""}`}>
      <SectionHeader
        title="Model Control Panel"
        detail={dashboard ? "Safe local counts, readiness signals and unsupported FM26 state." : "Awaiting local API data."}
      />
      <div className="metric-grid metric-grid-financial">
        <MetricCard
          label="Imported Players"
          value={String(dashboard?.importedPlayersCount ?? 0)}
          note={dashboard && dashboard.importedPlayersCount > 0 ? "Local safe rows" : "Awaiting local data"}
          tone={dashboard && dashboard.importedPlayersCount > 0 ? "success" : "muted"}
        />
        <MetricCard
          label="Shortlists"
          value={String(dashboard?.shortlistCount ?? 0)}
          note="Recruitment workflow"
          tone={dashboard && dashboard.shortlistCount > 0 ? "success" : "muted"}
        />
        <MetricCard
          label="Scout Assignments"
          value={String(dashboard?.scoutAssignmentCount ?? 0)}
          note="Human reports"
          tone={dashboard && dashboard.scoutAssignmentCount > 0 ? "info" : "muted"}
        />
        <MetricCard
          label="Role Templates"
          value={String(dashboard?.roleLabTemplateCount ?? 0)}
          note="Generic/import"
          tone={dashboard && dashboard.roleLabTemplateCount > 0 ? "info" : "muted"}
        />
        <MetricCard
          label="Benchmarks"
          value={String(dashboard?.benchmarkDefinitionCount ?? 0)}
          note="No fake percentiles"
          tone={dashboard && dashboard.benchmarkDefinitionCount > 0 ? "info" : "muted"}
        />
        <MetricCard
          label="FM26 Connector"
          value={connector?.isFm26Supported ? "Supported" : "Unsupported"}
          note={connector?.mapSupportStatus ?? "TemplateOnly"}
          tone="warning"
        />
      </div>

      <div className="control-panel-grid">
        <StatusMatrix
          title="Readiness Matrix"
          items={[
            {
              label: "Local Data",
              value: dashboard && dashboard.importedPlayersCount > 0 ? "Ready" : "Awaiting",
              tone: dashboard && dashboard.importedPlayersCount > 0 ? "success" : "muted"
            },
            {
              label: "Benchmarks",
              value: dashboard && dashboard.benchmarkDefinitionCount > 0 ? "Definitions" : "No benchmark yet",
              tone: dashboard && dashboard.benchmarkDefinitionCount > 0 ? "info" : "muted"
            },
            {
              label: "FM26",
              value: connector?.isFm26Supported ? "Supported" : "Unsupported",
              tone: "warning"
            },
            {
              label: "Maps",
              value: registry?.registryStatus ?? "Not checked",
              tone: toneForMapStatus(registry?.registryStatus)
            }
          ]}
        />
        <DiagnosticLedger
          rows={[
            {
              check: "Local Data",
              status: dashboard?.localReadinessStatus ?? "Not checked",
              value: String(dashboard?.importedPlayersCount ?? 0),
              message: dashboard && dashboard.importedPlayersCount > 0 ? "Safe local rows available." : "No local rows imported.",
              tone: dashboard && dashboard.importedPlayersCount > 0 ? "success" : "muted"
            },
            {
              check: "FM26 Connector",
              status: connector?.mapSupportStatus ?? "Unsupported",
              value: connector?.memoryMapRegistryStatus ?? "Not checked",
              message: connector?.supportStatusMessage ?? "FM26 unsupported until validated maps exist.",
              tone: "warning"
            },
            {
              check: "Registry",
              status: registry?.registryStatus ?? "Not checked",
              value: `${registry?.usableMapsCount ?? 0} validated`,
              message: registry?.nextActionSafeMessage ?? "Validated metadata required before future snapshots.",
              tone: toneForMapStatus(registry?.registryStatus)
            }
          ]}
        />
      </div>

      <RiskSignal
        tone="warning"
        title="Safe Data Guardrails"
        message="No fake rows, fake charts, live FM26 player data, hidden values or frontend scoring are shown."
      />
      <CompactTrendPlaceholder label="Trend analysis" />
    </section>
  );
}

function BoardStatsPanel({ state, visiblePlayers }: { state: ApiState; visiblePlayers: number }) {
  const dashboard = state.dashboard;
  const connector = state.connectorStatus;

  return (
    <section className="board-stat-grid" aria-label="Recruitment board local status">
      <MetricCard
        label="Players in Database"
        value={String(dashboard?.importedPlayersCount ?? 0)}
        note={dashboard && dashboard.importedPlayersCount > 0 ? "Safe local rows" : "Awaiting local data"}
        tone={dashboard && dashboard.importedPlayersCount > 0 ? "success" : "muted"}
      />
      <MetricCard
        label="Visible Rows"
        value={String(visiblePlayers)}
        note="After safe local filters"
        tone={visiblePlayers > 0 ? "success" : "muted"}
      />
      <MetricCard
        label="Shortlists"
        value={String(dashboard?.shortlistCount ?? 0)}
        note="Recruitment workflow"
        tone={dashboard && dashboard.shortlistCount > 0 ? "success" : "muted"}
      />
      <MetricCard
        label="Scout Assignments"
        value={String(dashboard?.scoutAssignmentCount ?? 0)}
        note="Human reports"
        tone={dashboard && dashboard.scoutAssignmentCount > 0 ? "info" : "muted"}
      />
      <MetricCard
        label="Role Templates"
        value={String(dashboard?.roleLabTemplateCount ?? 0)}
        note="Generic/import-safe"
        tone={dashboard && dashboard.roleLabTemplateCount > 0 ? "info" : "muted"}
      />
      <MetricCard
        label="FM26 Maps"
        value={connector?.mapSupportStatus ?? "Unsupported"}
        note={connector?.mapSupportMessage ?? "No validated maps"}
        tone={toneForMapStatus(connector?.mapSupportStatus)}
      />
    </section>
  );
}

function RecruitmentPanel({
  players,
  selectedPlayerId,
  onSelectPlayer,
  compact = false
}: {
  players: PlayerListItemDto[];
  selectedPlayerId: string | null;
  onSelectPlayer: (id: string) => void;
  compact?: boolean;
}) {
  return (
    <section className={`panel recruitment-panel ${compact ? "" : "wide"}`}>
      <div className="board-table-header">
        <div className="board-tabs" aria-label="Recruitment board view">
          <span className="active">All Players</span>
          <span>Shortlisted</span>
          <span>Comparison 0</span>
        </div>
        <div className="table-toolbar">
          <span>{players.length === 0 ? "Awaiting local data" : `${players.length} safe row${players.length === 1 ? "" : "s"}`}</span>
          <StatusChip tone="warning" value="No live FM26 data" />
        </div>
      </div>
      <div className="analyst-table" role="table" aria-label="Recruitment board">
        <div className="table-row table-head" role="row">
          <span>Player</span>
          <span>Age</span>
          <span>Nat</span>
          <span>Position</span>
          <span>Source</span>
          <span>Role</span>
          <span>Fit</span>
          <span>Confidence</span>
          <span>Data</span>
          <span>Benchmark</span>
          <span>Decision</span>
          <span>Missing</span>
          <span>Blocked</span>
          <span>Risk</span>
        </div>
        {players.length === 0 ? (
          <EmptyVisualState
            title="No local player data imported."
            message="Use Data Sources to import a permitted local CSV. No demo rows are generated."
          />
        ) : (
          players.slice(0, compact ? 5 : 10).map((player) => {
            const isSelected = player.statlynPlayerId === selectedPlayerId;
            const riskCount = player.safeWarnings.length + player.missingDataCount + player.blockedFieldCount;
            return (
              <button
                className={`table-row table-button ${isSelected ? "selected" : ""}`}
                key={player.statlynPlayerId}
                type="button"
                onClick={() => onSelectPlayer(player.statlynPlayerId)}
              >
                <span className="player-cell">
                  <strong>{player.displayName}</strong>
                  <small>{player.primaryPosition} / {player.sourceName || "Local source"}</small>
                </span>
                <span>{player.age || "Unknown"}</span>
                <span>{player.nationality || "Unknown"}</span>
                <span>{player.primaryPosition || player.positionGroup}</span>
                <VisualTableCell label={player.sourceName || "Local"} value={player.sourceConfidence} tone={toneForScore(player.sourceConfidence)} />
                <span>{player.roleName}</span>
                <VisualTableCell value={player.roleFit} tone={toneForScore(player.roleFit)} />
                <VisualTableCell value={player.confidence} tone={toneForScore(player.confidence)} />
                <DataQualityBar value={player.dataCompleteness} compact />
                <SignalBadge tone={toneForBenchmark(player.benchmarkStatus)} value={player.benchmarkStatus} />
                <SignalBadge tone={toneForRecommendation(player.recommendation)} value={player.recommendation} />
                <RiskCell label="Missing" value={player.missingDataCount} />
                <RiskCell label="Blocked" value={player.blockedFieldCount} />
                <RiskCell label="Risk" value={riskCount} />
              </button>
            );
          })
        )}
      </div>
    </section>
  );
}

function PlayerProfilePanel({ player }: { player: PlayerListItemDto | null }) {
  return (
    <section className="panel wide">
      <SectionHeader title="Player Profile" detail={player ? "Selected safe player row from API data." : "No imported players."} />
      {player ? (
        <div className="profile-stack">
          <div>
            <h2>{player.displayName}</h2>
            <p>{player.primaryPosition} / {player.nationality}</p>
          </div>
          <div className="visual-bars">
            <ModelConfidenceBar label="Role Fit" value={player.roleFit} />
            <ModelConfidenceBar label="Confidence" value={player.confidence} />
            <DataQualityBar value={player.dataCompleteness} />
          </div>
          <div className="chip-row">
            <SignalBadge label="Role" tone="info" value={player.roleName} />
            <SignalBadge label="Benchmark" tone={toneForBenchmark(player.benchmarkStatus)} value={player.benchmarkStatus} />
            <SignalBadge label="Blocked" tone={player.blockedFieldCount > 0 ? "warning" : "muted"} value={String(player.blockedFieldCount)} />
          </div>
          <WarningList warnings={player.safeWarnings} />
        </div>
      ) : (
        <EmptyVisualState title="No selected profile" message="Import CSV data before opening a player profile." />
      )}
    </section>
  );
}

function ConnectorStatusPanel({ state, wide = false }: { state: ApiState; wide?: boolean }) {
  const connector = state.connectorStatus;
  return (
    <section className={`panel connector-panel ${wide ? "wide" : ""}`}>
      <SectionHeader title="FM26 Diagnostics" detail={connector?.safeMessage ?? "Connector diagnostics unavailable."} />
      <div className="chip-row">
        <SignalBadge label="Native" tone={connector?.isNativeConnectorAvailable ? "success" : "muted"} value={connector?.availability ?? "Unknown"} />
        <SignalBadge label="Platform" tone={connector?.isWindows ? "info" : "warning"} value={connector?.isWindows ? "Windows" : "Unsupported"} />
        <SignalBadge label="FM Process" tone={connector?.isFmProcessDetected ? "info" : "muted"} value={connector?.detectionStatus ?? "NotDetected"} />
        <SignalBadge label="Read-only" tone={toneForDiagnosticStatus(connector?.readOnlyAccessStatus)} value={connector?.readOnlyAccessStatus ?? "Unavailable"} />
        <SignalBadge label="Map" tone={toneForMapStatus(connector?.mapSupportStatus)} value={connector?.mapSupportStatus ?? "MapMissing"} />
        <SignalBadge label="Support" tone="warning" value={connector?.isFm26Supported ? "Supported" : "Unsupported"} />
      </div>
      <DiagnosticLedger
        rows={[
          {
            check: "Native Connector",
            status: connector?.availability ?? "Unknown",
            value: connector?.connectorVersion || "Not reported",
            message: connector?.isNativeConnectorAvailable ? "Read-only diagnostics boundary available." : "Native connector library unavailable.",
            tone: connector?.isNativeConnectorAvailable ? "success" : "muted"
          },
          {
            check: "FM Process Detection",
            status: connector?.detectionStatus ?? "NotDetected",
            value: connector?.isFmProcessDetected ? connector.processName : "Not detected",
            message: connector?.detectionStatusMessage ?? "FM process detection has not returned a supported state.",
            tone: connector?.isFmProcessDetected ? "info" : "muted"
          },
          {
            check: "Read-only Access",
            status: connector?.readOnlyAccessStatus ?? "Unavailable",
            value: connector?.readOnlyAccessAttempted ? "Attempted" : "Not attempted",
            message: connector?.requiredAccessLevel || "Read-only diagnostics unavailable.",
            tone: toneForDiagnosticStatus(connector?.readOnlyAccessStatus)
          },
          {
            check: "Build Support",
            status: connector?.buildSupportStatus ?? "ConnectorUnavailable",
            value: connector?.productVersion || connector?.fileVersion || "Not reported",
            message: connector?.buildSupportMessage ?? "FM26 diagnostics unavailable.",
            tone: toneForDiagnosticStatus(connector?.buildSupportStatus)
          },
          {
            check: "Memory-map Registry",
            status: connector?.memoryMapRegistryStatus ?? "Not checked",
            value: `${connector?.usableMemoryMapCount ?? 0} validated / ${connector?.templateMemoryMapCount ?? 0} template`,
            message: connector?.mapSupportMessage ?? "No validated FM26 maps are loaded.",
            tone: toneForMapStatus(connector?.memoryMapRegistryStatus)
          },
          {
            check: "Next Action",
            status: connector?.isFm26Supported ? "Ready" : "Unsupported",
            value: connector?.selectedMemoryMapId || "No selected map",
            message: connector?.nextActionSafeMessage ?? "Validated FM26 maps are required before future player snapshots.",
            tone: "warning"
          }
        ]}
      />
      <WarningList warnings={connector?.warnings ?? ["FM26 unsupported until validated maps exist."]} />
    </section>
  );
}

function RoleLabPanel({ state }: { state: ApiState }) {
  return (
    <section className="panel wide">
      <SectionHeader title="Role Lab" detail={state.roleLab?.safeMessage ?? "No Role Lab data loaded."} />
      <div className="metric-grid compact">
        <MetricCard label="Roles" value={String(state.roleLab?.roleCount ?? 0)} note="C# templates" tone={state.roleLab && state.roleLab.roleCount > 0 ? "info" : "muted"} />
        <MetricCard label="Pairs" value={String(state.roleLab?.rolePairCount ?? 0)} note="IP/OOP links" tone={state.roleLab && state.roleLab.rolePairCount > 0 ? "info" : "muted"} />
      </div>
      <ComparisonMatrix
        title="Template Readiness"
        rows={[
          { label: "Template Source", value: state.roleLab && state.roleLab.roleCount > 0 ? "Generic/import" : "Awaiting seed", tone: state.roleLab && state.roleLab.roleCount > 0 ? "info" : "muted" },
          { label: "Official FM26 Mapping", value: "Not verified", tone: "warning" },
          { label: "Frontend Scoring", value: "Not present", tone: "success" }
        ]}
      />
    </section>
  );
}

function DataSourcesPanel({ state, wide = false }: { state: ApiState; wide?: boolean }) {
  return (
    <section className={`panel ${wide ? "wide" : ""}`}>
      <SectionHeader title="Data Sources" detail={state.dataSources?.safeMessage ?? "Awaiting local data."} />
      <StatusMatrix
        title="Source Status"
        items={[
          { label: "Mode", value: state.dataSources?.mode ?? "Local CSV", tone: "info" },
          { label: "Sources", value: String(state.dataSources?.dataSourceCount ?? 0), tone: state.dataSources && state.dataSources.dataSourceCount > 0 ? "success" : "muted" },
          { label: "Fixture", value: state.dataSources?.fixtureStatus ?? "Not checked", tone: "muted" },
          { label: "Import", value: state.dataSources?.importStatus ?? "No import status yet", tone: state.dataSources && state.dataSources.dataSourceCount > 0 ? "success" : "muted" }
        ]}
      />
      <WarningList warnings={state.dataSources?.warnings ?? []} />
    </section>
  );
}

function MemoryMapRegistryPanel({ state, wide = false }: { state: ApiState; wide?: boolean }) {
  const registry = state.memoryMaps;
  const maps = registry?.maps ?? [];
  return (
    <section className={`panel map-registry-panel ${wide ? "wide" : ""}`}>
      <SectionHeader title="Memory-map Registry" detail={registry?.safeMessage ?? "Map registry status is awaiting the local API."} />
      <div className="metric-grid compact-four">
        <MetricCard label="Found" tone={registry && registry.mapsFoundCount > 0 ? "info" : "muted"} value={String(registry?.mapsFoundCount ?? 0)} note="Metadata files" />
        <MetricCard label="Validated" tone={registry && registry.usableMapsCount > 0 ? "success" : "muted"} value={String(registry?.usableMapsCount ?? 0)} note="Usable metadata" />
        <MetricCard label="Templates" tone={registry && registry.templateMapsCount > 0 ? "warning" : "muted"} value={String(registry?.templateMapsCount ?? 0)} note="Not usable" />
        <MetricCard label="Invalid" tone={registry && registry.invalidMapsCount > 0 ? "danger" : "muted"} value={String(registry?.invalidMapsCount ?? 0)} note="Rejected" />
      </div>
      <DistributionStrip
        label="Registry Distribution"
        segments={[
          { label: "Validated", value: registry?.usableMapsCount ?? 0, tone: "success" },
          { label: "Templates", value: registry?.templateMapsCount ?? 0, tone: "warning" },
          { label: "Invalid", value: registry?.invalidMapsCount ?? 0, tone: "danger" }
        ]}
      />
      <DiagnosticLedger
        rows={[
          {
            check: "Registry Status",
            status: registry?.registryStatus ?? "Not checked",
            value: registry?.mapSupportStatus ?? "MapMissing",
            message: registry?.mapSupportMessage ?? "No validated FM26 maps are loaded.",
            tone: toneForMapStatus(registry?.registryStatus)
          },
          {
            check: "Selected Metadata",
            status: registry?.selectedMapStatus ?? "None",
            value: registry?.selectedMapDisplayName || registry?.selectedMapId || "None",
            message: registry?.selectedMapId ? "Metadata only; player reading remains unavailable." : "No usable validated metadata selected.",
            tone: toneForMapStatus(registry?.selectedMapStatus)
          },
          {
            check: "Player Reading",
            status: "Not implemented",
            value: "Unavailable",
            message: registry?.nextActionSafeMessage ?? "Validate maps before any future player snapshot milestone.",
            tone: "warning"
          }
        ]}
      />
      {maps.length === 0 ? (
        <EmptyVisualState title="No map metadata files found" message="Player reading is not implemented." />
      ) : (
        <div className="map-list">
          {maps.slice(0, 6).map((map) => (
            <div className="map-row" key={`${map.mapId}-${map.displayName}`}>
              <div>
                <strong>{map.displayName || map.mapId || "Unnamed map"}</strong>
                <span>{map.isTemplate ? "Template" : map.isValidated ? "Validated metadata" : "Unvalidated metadata"} / {map.platform || "Platform unknown"} / {map.architecture || "Architecture unknown"}</span>
              </div>
              <div className="map-row-stats">
                <SignalBadge tone={toneForMapStatus(map.supportStatus)} value={map.supportStatus} />
                <SignalBadge label="Fields" tone="info" value={String(map.fieldCount)} />
                <SignalBadge label="Visible" tone="info" value={String(map.visibleFieldCount)} />
                <SignalBadge label="Blocked" tone={map.hiddenFieldCountBlocked > 0 ? "warning" : "muted"} value={String(map.hiddenFieldCountBlocked)} />
              </div>
            </div>
          ))}
        </div>
      )}
    </section>
  );
}

function DiagnosticsPanel({ state, wide = false }: { state: ApiState; wide?: boolean }) {
  const connector = state.connectorStatus;
  const registry = state.memoryMaps;
  return (
    <section className={`panel ${wide ? "wide" : ""}`}>
      <SectionHeader title="Diagnostics" detail={state.diagnostics?.safeSummary ?? "Diagnostics unavailable."} />
      <DiagnosticLedger
        rows={[
          {
            check: "API Boundary",
            status: state.health?.status ?? "Not checked",
            value: apiBaseUrl(),
            message: state.health?.safeMessage ?? "Local API health is not available.",
            tone: state.health ? "info" : "muted"
          },
          {
            check: "Schema",
            status: String(state.diagnostics?.schemaVersion ?? 0),
            value: `${state.diagnostics?.importedPlayerCount ?? 0} players`,
            message: state.diagnostics?.success ? "Diagnostics returned safe local counts." : "Diagnostics are not checked.",
            tone: state.diagnostics?.success ? "success" : "muted"
          },
          {
            check: "FM26 Support",
            status: connector?.isFm26Supported ? "Supported" : "Unsupported",
            value: connector?.mapSupportStatus ?? state.diagnostics?.fm26Status ?? "Unsupported",
            message: connector?.supportStatusMessage ?? "FM26 unsupported until validated maps exist.",
            tone: "warning"
          },
          {
            check: "Read-only Access",
            status: connector?.readOnlyAccessStatus ?? "Unavailable",
            value: connector?.readOnlyAccessAttempted ? "Attempted" : "Not attempted",
            message: connector?.lastErrorSafeMessage || "No unsafe stack trace is shown.",
            tone: toneForDiagnosticStatus(connector?.readOnlyAccessStatus)
          },
          {
            check: "Memory-map Registry",
            status: registry?.registryStatus ?? "Not checked",
            value: `${registry?.mapsFoundCount ?? 0} found`,
            message: registry?.nextActionSafeMessage ?? "Player reading not implemented.",
            tone: toneForMapStatus(registry?.registryStatus)
          }
        ]}
      />
      <RiskSignal
        tone="warning"
        title="Future Snapshot Gate"
        message={connector?.nextActionSafeMessage ?? "Validated FM26 maps are required before future player snapshots. Player reading not implemented."}
      />
    </section>
  );
}

function ScoutReportsPanel({ state, wide = false }: { state: ApiState; wide?: boolean }) {
  return (
    <section className={`panel ${wide ? "wide" : ""}`}>
      <SectionHeader title="Scout Reports" detail={state.scoutReports.length === 0 ? "No scout assignments." : "Qualitative reports only."} />
      {state.scoutReports.slice(0, 4).map((report) => (
        <div className="mini-row" key={`${report.statlynPlayerId}-${report.latestRecommendation}`}>
          <strong>{report.playerName}</strong>
          <span>{report.latestRecommendation}</span>
        </div>
      ))}
      {state.scoutReports.length === 0 ? <EmptyVisualState title="No scout reports yet" message="Qualitative reports appear here after local scout assignments exist." /> : null}
    </section>
  );
}

function EmptyWorkflowPanel({ title, text, wide = false }: { title: string; text: string; wide?: boolean }) {
  return (
    <section className={`panel muted-panel ${wide ? "wide" : ""}`}>
      <SectionHeader title={title} detail={text} />
      <EmptyVisualState title="Awaiting real local data" message="No fake data is shown." />
    </section>
  );
}

function ErrorState({ message, onRetry }: { message: string; onRetry: () => void }) {
  return (
    <div className="state-panel danger">
      <img className="state-mark" src={brandAssets.markWhite} alt="" aria-hidden="true" />
      <strong>API Offline</strong>
      <span>{message}</span>
      <small>Start Statlyn.Api, then refresh the desktop workspace. No fallback rows are generated.</small>
      <button className="retry-button" type="button" onClick={onRetry}>
        Retry API connection
      </button>
    </div>
  );
}

function LoadingState() {
  return (
    <div className="state-panel">
      <img className="state-mark" src={brandAssets.markWhite} alt="" aria-hidden="true" />
      <strong>Loading local API workspace</strong>
      <span>Preparing safe recruitment DTOs...</span>
    </div>
  );
}

function SectionHeader({ title, detail }: { title: string; detail: string }) {
  return (
    <div className="panel-header">
      <h2>{title}</h2>
      <p>{detail}</p>
    </div>
  );
}

function MetricCard({ label, value, note, tone }: { label: string; value: string; note: string; tone: Tone }) {
  return (
    <div className={`metric-card ${tone} ${value.length > 10 ? "long-value" : ""}`}>
      <span>{label}</span>
      <strong>{value}</strong>
      <small>{note}</small>
    </div>
  );
}

function StatusMatrix({ title, items }: { title: string; items: MatrixItem[] }) {
  return (
    <div className="status-matrix">
      <h3>{title}</h3>
      <div>
        {items.map((item) => (
          <div className="status-matrix-cell" key={`${item.label}-${item.value}`}>
            <span>{item.label}</span>
            <SignalBadge tone={item.tone} value={item.value} />
          </div>
        ))}
      </div>
    </div>
  );
}

function StatusPill({ label, value, tone }: { label: string; value: string; tone: Tone }) {
  return (
    <div className={`status-pill ${tone}`}>
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}

function SignalBadge({ label, value, tone }: { label?: string; value: string; tone: Tone }) {
  return (
    <span className={`status-chip signal-badge ${tone}`}>
      {label ? <small>{label}</small> : null}
      <strong>{value}</strong>
    </span>
  );
}

function StatusChip({ label, value, tone }: { label?: string; value: string; tone: Tone }) {
  return <SignalBadge label={label} value={value} tone={tone} />;
}

function DiagnosticLedger({ rows }: { rows: DiagnosticLedgerRow[] }) {
  return (
    <div className="diagnostic-ledger" role="table" aria-label="Diagnostic risk ledger">
      <div className="diagnostic-ledger-row diagnostic-ledger-head" role="row">
        <span>Check</span>
        <span>Status</span>
        <span>Value</span>
        <span>Message</span>
      </div>
      {rows.map((row) => (
        <div className="diagnostic-ledger-row" role="row" key={`${row.check}-${row.status}`}>
          <strong>{row.check}</strong>
          <SignalBadge tone={row.tone} value={row.status} />
          <span>{row.value}</span>
          <p>{row.message}</p>
        </div>
      ))}
    </div>
  );
}

function RiskSignal({ title, message, tone }: { title: string; message: string; tone: Tone }) {
  return (
    <div className={`risk-signal ${tone}`}>
      <strong>{title}</strong>
      <span>{message}</span>
    </div>
  );
}

function DataQualityBar({ value, compact = false }: { value: number; compact?: boolean }) {
  return <ValueBar label="Data Quality" value={value} tone={toneForScore(value)} compact={compact} />;
}

function ModelConfidenceBar({ label, value }: { label: string; value: number | null }) {
  return <ValueBar label={label} value={value} tone={toneForScore(value)} />;
}

function ValueBar({
  label,
  value,
  tone,
  compact = false
}: {
  label: string;
  value: number | null;
  tone: Tone;
  compact?: boolean;
}) {
  const percent = normalizePercent(value);
  return (
    <div className={`value-bar ${tone} ${compact ? "compact" : ""} ${value === null ? "empty" : ""}`}>
      <div>
        <span>{label}</span>
        <strong>{formatNullable(value)}</strong>
      </div>
      <i aria-hidden="true">
        <b style={{ width: value === null ? "0%" : `${percent}%` }} />
      </i>
    </div>
  );
}

function DistributionStrip({ label, segments }: { label: string; segments: DistributionSegment[] }) {
  const total = segments.reduce((sum, segment) => sum + segment.value, 0);
  return (
    <div className="distribution-strip">
      <div>
        <span>{label}</span>
        <strong>{total === 0 ? "No metadata distribution" : `${total} total`}</strong>
      </div>
      <div className="distribution-bars" aria-hidden="true">
        {total === 0 ? (
          <i className="muted" style={{ width: "100%" }} />
        ) : (
          segments.map((segment) => (
            <i
              className={segment.tone}
              key={`${segment.label}-${segment.value}`}
              style={{ width: `${Math.max(4, (segment.value / total) * 100)}%` }}
            />
          ))
        )}
      </div>
      <div className="distribution-labels">
        {segments.map((segment) => (
          <SignalBadge key={segment.label} label={segment.label} tone={segment.tone} value={String(segment.value)} />
        ))}
      </div>
    </div>
  );
}

function CompactTrendPlaceholder({ label }: { label: string }) {
  return (
    <div className="trend-placeholder">
      <span>{label}</span>
      <strong>Trend unavailable</strong>
    </div>
  );
}

function ComparisonMatrix({ title, rows }: { title: string; rows: MatrixItem[] }) {
  return (
    <div className="comparison-matrix">
      <h3>{title}</h3>
      {rows.map((row) => (
        <div className="comparison-row" key={`${row.label}-${row.value}`}>
          <span>{row.label}</span>
          <SignalBadge tone={row.tone} value={row.value} />
        </div>
      ))}
    </div>
  );
}

function VisualTableCell({ label, value, tone }: { label?: string; value: number | null; tone: Tone }) {
  return (
    <span className="visual-table-cell">
      {label ? <small>{label}</small> : null}
      <ValueBar label={label ? "Confidence" : "Score"} value={value} tone={tone} compact />
    </span>
  );
}

function RiskCell({ label, value }: { label: string; value: number }) {
  const tone: Tone = value > 0 ? "warning" : "muted";
  return (
    <span className={`risk-cell ${tone}`} aria-label={`${label}: ${value}`}>
      <strong>{value}</strong>
      <small>{label}</small>
    </span>
  );
}

function EmptyVisualState({ title, message }: { title: string; message: string }) {
  return (
    <div className="empty-table-state empty-visual-state">
      <strong>{title}</strong>
      <span>{message}</span>
    </div>
  );
}

function MiniStatus({ label, value }: { label: string; value: string }) {
  return (
    <div className="mini-status">
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}

function WarningList({ warnings }: { warnings: string[] }) {
  if (warnings.length === 0) {
    return <div className="empty-line">No warnings returned by the API.</div>;
  }

  return (
    <div className="warning-list">
      <span>Warnings</span>
      {warnings.slice(0, 4).map((warning) => (
        <p key={warning}>{warning}</p>
      ))}
    </div>
  );
}

function sectionTitle(section: SectionName): string {
  if (section === "Recruitment Board") {
    return "Scout Room";
  }

  return section;
}

function navLabel(section: SectionName): string {
  if (section === "Recruitment Board") {
    return "Scout Room";
  }

  return section;
}

function sectionSummary(
  section: SectionName,
  state: ApiState,
  visiblePlayerCount: number,
  totalPlayerCount: number,
  hasActiveFilters: boolean
): string {
  if (section === "Recruitment Board") {
    if (totalPlayerCount === 0) {
      return "Local data analysis and intelligent scouting. No local players imported yet.";
    }

    return hasActiveFilters
      ? `${visiblePlayerCount} of ${totalPlayerCount} safe local rows match the current filters.`
      : "Local data analysis and intelligent scouting from Statlyn.Api.";
  }

  if (section === "Dashboard") {
    return state.dashboard?.localReadinessStatus ?? "Local readiness is not checked.";
  }

  if (section === "Diagnostics") {
    return state.diagnostics?.success ? "Diagnostics are available from the local API." : "Diagnostics are awaiting local API data.";
  }

  if (section === "Data Sources") {
    return state.dataSources?.safeMessage ?? "Local CSV source status.";
  }

  return "Safe local workspace state. No live FM26 data is exposed.";
}

function placeholderText(section: SectionName): string {
  if (section === "Squad Gaps") {
    return "Define squad targets before claiming a recruitment gap.";
  }

  if (section === "Comparisons") {
    return "Import at least two safe players before comparing recruitment evidence.";
  }

  return "Prepared for future tactical models; no simulation or fake tactical analysis.";
}

function toneForScore(value: number | null): Tone {
  if (value === null) {
    return "muted";
  }

  if (value >= 75) {
    return "success";
  }

  if (value >= 50) {
    return "info";
  }

  return "warning";
}

function toneForBenchmark(value: string): Tone {
  if (value.toLowerCase().includes("no benchmark")) {
    return "muted";
  }

  if (value.toLowerCase().includes("insufficient")) {
    return "warning";
  }

  return "info";
}

function toneForRecommendation(value: string): Tone {
  if (value.toLowerCase().includes("reject")) {
    return "danger";
  }

  if (value.toLowerCase().includes("sign")) {
    return "success";
  }

  if (value.toLowerCase().includes("further") || value.toLowerCase().includes("monitor")) {
    return "warning";
  }

  return "info";
}

function toneForDiagnosticStatus(value: string | undefined): Tone {
  const normalized = (value ?? "").toLowerCase();

  if (normalized.includes("denied") || normalized.includes("error") || normalized.includes("failed")) {
    return "danger";
  }

  if (
    normalized.includes("missing") ||
    normalized.includes("unvalidated") ||
    normalized.includes("unsupported") ||
    normalized.includes("notdetected") ||
    normalized.includes("not detected")
  ) {
    return "warning";
  }

  if (normalized.includes("available") || normalized.includes("diagnostic") || normalized.includes("ready")) {
    return "info";
  }

  return "muted";
}

function toneForMapStatus(value: string | undefined): Tone {
  const normalized = (value ?? "").toLowerCase();

  if (normalized.includes("invalid") || normalized.includes("denied") || normalized.includes("failed")) {
    return "danger";
  }

  if (
    normalized.includes("missing") ||
    normalized.includes("template") ||
    normalized.includes("unvalidated") ||
    normalized.includes("mismatch") ||
    normalized.includes("unsupported")
  ) {
    return "warning";
  }

  if (normalized.includes("available") || normalized.includes("notimplemented")) {
    return "info";
  }

  return "muted";
}

function formatNullable(value: number | null): string {
  return value === null ? "Unknown" : String(value);
}

function normalizePercent(value: number | null): number {
  if (value === null || Number.isNaN(value)) {
    return 0;
  }

  return Math.max(0, Math.min(100, value));
}

function filterPlayers(
  players: PlayerListItemDto[],
  searchTerm: string,
  positionFilter: string,
  recommendationFilter: string
): PlayerListItemDto[] {
  const normalizedSearch = searchTerm.trim().toLowerCase();

  return players.filter((player) => {
    const matchesSearch =
      normalizedSearch.length === 0 ||
      [
        player.displayName,
        player.age,
        player.nationality,
        player.positionGroup,
        player.primaryPosition,
        player.sourceName,
        player.roleName,
        player.recommendation,
        player.benchmarkStatus
      ]
        .join(" ")
        .toLowerCase()
        .includes(normalizedSearch);
    const matchesPosition =
      positionFilter === "All" || player.primaryPosition === positionFilter || player.positionGroup === positionFilter;
    const matchesRecommendation = recommendationFilter === "All" || player.recommendation === recommendationFilter;

    return matchesSearch && matchesPosition && matchesRecommendation;
  });
}

function uniqueSorted(values: string[]): string[] {
  return Array.from(new Set(values.map((value) => value.trim()).filter(Boolean))).sort((left, right) =>
    left.localeCompare(right)
  );
}
