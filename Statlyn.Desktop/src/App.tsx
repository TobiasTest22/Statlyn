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
        <ConnectorStatusPanel state={state} />
        <DiagnosticsPanel state={state} />
      </section>
    );
  }

  if (activeSection === "Diagnostics") {
    return (
      <section className="content-grid">
        <DiagnosticsPanel state={state} wide />
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
      <DataSourcesPanel state={state} />
      <DiagnosticsPanel state={state} />
      <ScoutReportsPanel state={state} />
    </section>
  );
}

function DashboardPanel({ state, wide = false }: { state: ApiState; wide?: boolean }) {
  const dashboard = state.dashboard;
  return (
    <section className={`panel cockpit-panel ${wide ? "wide" : ""}`}>
      <PanelHeader title="Cockpit Overview" detail={dashboard ? "Local workspace readiness, safe counts and unsupported FM26 state." : "Awaiting local API data."} />
      <div className="metric-grid">
        <Metric label="Imported Players" value={dashboard?.importedPlayersCount ?? 0} note={dashboard && dashboard.importedPlayersCount > 0 ? "Local safe rows" : "Awaiting local data"} tone={dashboard && dashboard.importedPlayersCount > 0 ? "success" : "muted"} />
        <Metric label="Shortlists" value={dashboard?.shortlistCount ?? 0} note="Recruitment workflow" tone={dashboard && dashboard.shortlistCount > 0 ? "success" : "muted"} />
        <Metric label="Scout Assignments" value={dashboard?.scoutAssignmentCount ?? 0} note="Human reports" tone={dashboard && dashboard.scoutAssignmentCount > 0 ? "info" : "muted"} />
        <Metric label="Role Templates" value={dashboard?.roleLabTemplateCount ?? 0} note="Generic/import" tone={dashboard && dashboard.roleLabTemplateCount > 0 ? "info" : "muted"} />
        <Metric label="Benchmarks" value={dashboard?.benchmarkDefinitionCount ?? 0} note="No fake percentiles" tone={dashboard && dashboard.benchmarkDefinitionCount > 0 ? "info" : "muted"} />
      </div>
      <div className="cockpit-status-row">
        <StatusChip tone="warning" value={state.connectorStatus?.supportStatusMessage ?? "FM26 unsupported until validated maps exist."} />
        <StatusChip tone={state.diagnostics?.success ? "success" : "muted"} value={state.diagnostics?.success ? "Diagnostics available" : "Diagnostics not checked"} />
        <StatusChip tone="info" value={state.dataSources?.importStatus ?? "No import status yet."} />
      </div>
    </section>
  );
}

function BoardStatsPanel({ state, visiblePlayers }: { state: ApiState; visiblePlayers: number }) {
  const dashboard = state.dashboard;
  const connector = state.connectorStatus;

  return (
    <section className="board-stat-grid" aria-label="Recruitment board local status">
      <BoardStatCard
        label="Players in Database"
        value={String(dashboard?.importedPlayersCount ?? 0)}
        note={dashboard && dashboard.importedPlayersCount > 0 ? "Safe local rows" : "Awaiting local data"}
        tone={dashboard && dashboard.importedPlayersCount > 0 ? "success" : "muted"}
      />
      <BoardStatCard
        label="Visible Rows"
        value={String(visiblePlayers)}
        note="After safe local filters"
        tone={visiblePlayers > 0 ? "success" : "muted"}
      />
      <BoardStatCard
        label="Shortlists"
        value={String(dashboard?.shortlistCount ?? 0)}
        note="Recruitment workflow"
        tone={dashboard && dashboard.shortlistCount > 0 ? "success" : "muted"}
      />
      <BoardStatCard
        label="Scout Assignments"
        value={String(dashboard?.scoutAssignmentCount ?? 0)}
        note="Human reports"
        tone={dashboard && dashboard.scoutAssignmentCount > 0 ? "info" : "muted"}
      />
      <BoardStatCard
        label="Role Templates"
        value={String(dashboard?.roleLabTemplateCount ?? 0)}
        note="Generic/import-safe"
        tone={dashboard && dashboard.roleLabTemplateCount > 0 ? "info" : "muted"}
      />
      <BoardStatCard
        label="FM26 Status"
        value={connector?.mapSupportStatus ?? "Unsupported"}
        note={connector?.supportStatusMessage ?? "No validated maps"}
        tone="warning"
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
          <span>Source</span>
          <span>Position</span>
          <span>Role</span>
          <span>Fit</span>
          <span>Confidence</span>
          <span>Benchmark</span>
          <span>Decision</span>
          <span>Warnings</span>
        </div>
        {players.length === 0 ? (
          <div className="empty-table-state">
            <strong>No local player data imported.</strong>
            <span>Use Data Sources to import a permitted local CSV. No demo rows are generated.</span>
          </div>
        ) : (
          players.slice(0, compact ? 5 : 10).map((player) => {
            const isSelected = player.statlynPlayerId === selectedPlayerId;
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
                <span>{player.sourceName || "Local"}</span>
                <span>{player.primaryPosition || player.positionGroup}</span>
                <span>{player.roleName}</span>
                <StatusChip tone={toneForScore(player.roleFit)} value={formatNullable(player.roleFit)} />
                <StatusChip tone={toneForScore(player.confidence)} value={formatNullable(player.confidence)} />
                <StatusChip tone={toneForBenchmark(player.benchmarkStatus)} value={player.benchmarkStatus} />
                <StatusChip tone={toneForRecommendation(player.recommendation)} value={player.recommendation} />
                <StatusChip tone={player.safeWarnings.length > 0 || player.missingDataCount > 0 ? "warning" : "muted"} value={String(player.safeWarnings.length + player.missingDataCount)} />
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
      <PanelHeader title="Player Profile" detail={player ? "Selected safe player row from API data." : "No imported players."} />
      {player ? (
        <div className="profile-stack">
          <div>
            <h2>{player.displayName}</h2>
            <p>{player.primaryPosition} / {player.nationality}</p>
          </div>
          <div className="chip-row">
            <StatusChip label="Role" tone="info" value={player.roleName} />
            <StatusChip label="Benchmark" tone={toneForBenchmark(player.benchmarkStatus)} value={player.benchmarkStatus} />
            <StatusChip label="Blocked" tone={player.blockedFieldCount > 0 ? "warning" : "muted"} value={String(player.blockedFieldCount)} />
          </div>
          <WarningList warnings={player.safeWarnings} />
        </div>
      ) : (
        <div className="empty-line">Import CSV data before opening a player profile.</div>
      )}
    </section>
  );
}

function ConnectorStatusPanel({ state, wide = false }: { state: ApiState; wide?: boolean }) {
  const connector = state.connectorStatus;
  return (
    <section className={`panel connector-panel ${wide ? "wide" : ""}`}>
      <PanelHeader title="FM26 Diagnostics" detail={connector?.safeMessage ?? "Connector diagnostics unavailable."} />
      <div className="chip-row">
        <StatusChip label="Native" tone={connector?.isNativeConnectorAvailable ? "success" : "muted"} value={connector?.availability ?? "Unknown"} />
        <StatusChip label="Platform" tone={connector?.isWindows ? "info" : "warning"} value={connector?.isWindows ? "Windows" : "Unsupported"} />
        <StatusChip label="FM Process" tone={connector?.isFmProcessDetected ? "info" : "muted"} value={connector?.detectionStatus ?? "NotDetected"} />
        <StatusChip label="Read-only" tone={toneForDiagnosticStatus(connector?.readOnlyAccessStatus)} value={connector?.readOnlyAccessStatus ?? "Unavailable"} />
        <StatusChip label="Map" tone="warning" value={connector?.mapSupportStatus ?? "MapMissing"} />
        <StatusChip label="Support" tone="warning" value={connector?.isFm26Supported ? "Supported" : "Unsupported"} />
      </div>
      <div className="status-list dense">
        <MiniStatus label="Process" value={connector?.isFmProcessDetected ? connector.processName : "Not detected"} />
        <MiniStatus label="Process ID" value={connector?.processId === null || connector?.processId === undefined ? "Unavailable" : String(connector.processId)} />
        <MiniStatus label="Executable" value={connector?.executableFileName || "Not reported"} />
        <MiniStatus label="Folder Label" value={connector?.executableDirectorySafeLabel || "Not reported"} />
        <MiniStatus label="Product Version" value={connector?.productVersion || "Not reported"} />
        <MiniStatus label="File Version" value={connector?.fileVersion || "Not reported"} />
        <MiniStatus label="Architecture" value={connector?.architecture || "Not reported"} />
        <MiniStatus label="64-bit" value={connector?.is64BitProcess === null || connector?.is64BitProcess === undefined ? "Not reported" : connector.is64BitProcess ? "Yes" : "No"} />
        <MiniStatus label="Read-only Tried" value={connector?.readOnlyAccessAttempted ? "Yes" : "No"} />
        <MiniStatus label="Access Level" value={connector?.requiredAccessLevel || "Read-only diagnostics unavailable"} />
        <MiniStatus label="Build Status" value={connector?.buildSupportStatus ?? "ConnectorUnavailable"} />
        <MiniStatus label="Build Message" value={connector?.buildSupportMessage ?? "FM26 diagnostics unavailable."} />
        <MiniStatus label="Map Message" value={connector?.mapSupportMessage ?? "No validated FM26 maps are loaded."} />
        <MiniStatus label="Next Action" value={connector?.nextActionSafeMessage ?? "Validated FM26 maps are required before live FM player data can be read."} />
        <MiniStatus label="Generated" value={connector?.generatedAtUtc ?? "Not checked"} />
        {connector?.connectorVersion ? <MiniStatus label="Connector" value={connector.connectorVersion} /> : null}
      </div>
      <WarningList warnings={connector?.warnings ?? ["FM26 unsupported until validated maps exist."]} />
    </section>
  );
}

function RoleLabPanel({ state }: { state: ApiState }) {
  return (
    <section className="panel wide">
      <PanelHeader title="Role Lab" detail={state.roleLab?.safeMessage ?? "No Role Lab data loaded."} />
      <div className="metric-grid compact">
        <Metric label="Roles" value={state.roleLab?.roleCount ?? 0} note="C# templates" tone={state.roleLab && state.roleLab.roleCount > 0 ? "info" : "muted"} />
        <Metric label="Pairs" value={state.roleLab?.rolePairCount ?? 0} note="IP/OOP links" tone={state.roleLab && state.roleLab.rolePairCount > 0 ? "info" : "muted"} />
      </div>
    </section>
  );
}

function DataSourcesPanel({ state, wide = false }: { state: ApiState; wide?: boolean }) {
  return (
    <section className={`panel ${wide ? "wide" : ""}`}>
      <PanelHeader title="Data Sources" detail={state.dataSources?.safeMessage ?? "Awaiting local data."} />
      <div className="status-list">
        <MiniStatus label="Mode" value={state.dataSources?.mode ?? "Local CSV"} />
        <MiniStatus label="Fixture" value={state.dataSources?.fixtureStatus ?? "Synthetic fixture status unavailable."} />
        <MiniStatus label="Import" value={state.dataSources?.importStatus ?? "No import status yet."} />
      </div>
    </section>
  );
}

function DiagnosticsPanel({ state, wide = false }: { state: ApiState; wide?: boolean }) {
  const connector = state.connectorStatus;
  return (
    <section className={`panel ${wide ? "wide" : ""}`}>
      <PanelHeader title="Diagnostics" detail={state.diagnostics?.safeSummary ?? "Diagnostics unavailable."} />
      <div className="chip-row">
        <StatusChip label="Schema" tone="info" value={String(state.diagnostics?.schemaVersion ?? 0)} />
        <StatusChip label="FM26" tone="warning" value={connector?.supportStatusMessage ?? state.diagnostics?.fm26Status ?? "Unsupported"} />
        <StatusChip label="Map" tone="warning" value={connector?.mapSupportStatus ?? "MapMissing"} />
        <StatusChip label="Access" tone={toneForDiagnosticStatus(connector?.readOnlyAccessStatus)} value={connector?.readOnlyAccessStatus ?? "Unavailable"} />
        <StatusChip label="API" tone="info" value={apiBaseUrl()} />
      </div>
      <div className="diagnostic-note">
        {connector?.nextActionSafeMessage ?? "Validated FM26 maps are required before live FM player data can be read."}
      </div>
    </section>
  );
}

function ScoutReportsPanel({ state, wide = false }: { state: ApiState; wide?: boolean }) {
  return (
    <section className={`panel ${wide ? "wide" : ""}`}>
      <PanelHeader title="Scout Reports" detail={state.scoutReports.length === 0 ? "No scout assignments." : "Qualitative reports only."} />
      {state.scoutReports.slice(0, 4).map((report) => (
        <div className="mini-row" key={`${report.statlynPlayerId}-${report.latestRecommendation}`}>
          <strong>{report.playerName}</strong>
          <span>{report.latestRecommendation}</span>
        </div>
      ))}
      {state.scoutReports.length === 0 ? <div className="empty-line">No scout reports yet.</div> : null}
    </section>
  );
}

function EmptyWorkflowPanel({ title, text, wide = false }: { title: string; text: string; wide?: boolean }) {
  return (
    <section className={`panel muted-panel ${wide ? "wide" : ""}`}>
      <PanelHeader title={title} detail={text} />
      <div className="empty-line">No fake data is shown.</div>
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

function PanelHeader({ title, detail }: { title: string; detail: string }) {
  return (
    <div className="panel-header">
      <h2>{title}</h2>
      <p>{detail}</p>
    </div>
  );
}

function Metric({ label, value, note, tone }: { label: string; value: number; note: string; tone: Tone }) {
  return (
    <div className={`metric ${tone}`}>
      <span>{label}</span>
      <strong>{value}</strong>
      <small>{note}</small>
    </div>
  );
}

function BoardStatCard({ label, value, note, tone }: { label: string; value: string; note: string; tone: Tone }) {
  return (
    <div className={`board-stat-card ${tone} ${value.length > 8 ? "long-value" : ""}`}>
      <span>{label}</span>
      <strong>{value}</strong>
      <small>{note}</small>
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

function StatusChip({ label, value, tone }: { label?: string; value: string; tone: Tone }) {
  return (
    <span className={`status-chip ${tone}`}>
      {label ? <small>{label}</small> : null}
      <strong>{value}</strong>
    </span>
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
    return <div className="empty-line">No player warnings returned by the API.</div>;
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

function formatNullable(value: number | null): string {
  return value === null ? "Unknown" : String(value);
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
