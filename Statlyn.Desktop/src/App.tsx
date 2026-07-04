import { useCallback, useEffect, useMemo, useState } from "react";
import { apiBaseUrl, loadWorkspace } from "./api";
import type { ApiState, PlayerListItemDto } from "./types";

const navItems = [
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
  wordmarkWhite: "/branding/statlyn-wordmark-white.png"
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
  const [activeSection, setActiveSection] = useState("Dashboard");
  const [apiState, setApiState] = useState<ApiState>(emptyState);
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
  const selectedPlayer = useMemo(() => players[0] ?? null, [players]);

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand">
          <img className="brand-mark" src={brandAssets.markWhite} alt="" aria-hidden="true" />
          <div className="brand-copy">
            <img className="brand-wordmark" src={brandAssets.wordmarkWhite} alt="Statlyn" />
            <span>Recruitment Intelligence</span>
          </div>
        </div>
        <nav>
          {navItems.map((item) => (
            <button
              key={item}
              className={item === activeSection ? "active" : ""}
              type="button"
              onClick={() => setActiveSection(item)}
            >
              {item}
            </button>
          ))}
        </nav>
      </aside>

      <main className="workspace">
        <header className="topbar">
          <div>
            <span className="eyeline">C# API bridge</span>
            <h1>{activeSection}</h1>
          </div>
          <div className="status-strip">
            <StatusPill label="API" tone={error ? "danger" : apiState.health ? "success" : "muted"} value={error ? "Offline" : apiState.health ? "Connected" : "Checking"} />
            <StatusPill label="Data" tone="info" value={apiState.dataSources?.mode ?? "Local CSV"} />
            <StatusPill label="Connector" tone={apiState.connectorStatus?.isNativeConnectorAvailable ? "info" : "muted"} value={apiState.connectorStatus ? apiState.connectorStatus.availability : "Checking"} />
            <StatusPill label="FM26" tone={apiState.connectorStatus?.isFm26Supported ? "success" : "warning"} value={apiState.connectorStatus?.isFm26Supported ? "Supported" : "Unsupported"} />
          </div>
        </header>

        {isLoading ? <LoadingState /> : null}
        {!isLoading && error ? <ErrorState message={error} onRetry={refreshWorkspace} /> : null}
        {!isLoading && !error ? (
          <section className="content-grid">
            <DashboardPanel state={apiState} />
            <RecruitmentPanel players={players} />
            <PlayerProfilePanel player={selectedPlayer} />
            <ConnectorStatusPanel state={apiState} />
            <RoleLabPanel state={apiState} />
            <DataSourcesPanel state={apiState} />
            <DiagnosticsPanel state={apiState} />
            <ScoutReportsPanel state={apiState} />
            <EmptyWorkflowPanel title="Squad Gaps" text="Define squad targets before claiming a recruitment gap." />
            <EmptyWorkflowPanel title="Comparisons" text="Import at least two safe players before comparing recruitment evidence." />
            <EmptyWorkflowPanel title="Tactical Lab" text="Prepared for future tactical models; no simulation or fake tactical analysis." />
          </section>
        ) : null}
      </main>
    </div>
  );
}

function ConnectorStatusPanel({ state }: { state: ApiState }) {
  const connector = state.connectorStatus;
  return (
    <section className="panel">
      <PanelHeader title="Connector Status" detail={connector?.safeMessage ?? "Connector diagnostics unavailable."} />
      <div className="status-list">
        <MiniStatus label="Binding" value={connector?.availability ?? "Unknown"} />
        <MiniStatus label="FM Process" value={connector?.isFmProcessDetected ? "Detected" : "Not detected"} />
        <MiniStatus label="Read-only" value={connector?.readOnlyAccessStatus ?? "Unavailable"} />
        <MiniStatus label="Support" value={connector?.supportStatusMessage ?? "Unsupported until validated maps exist."} />
        {connector?.connectorVersion ? <MiniStatus label="Version" value={connector.connectorVersion} /> : null}
      </div>
    </section>
  );
}

function DashboardPanel({ state }: { state: ApiState }) {
  const dashboard = state.dashboard;
  return (
    <section className="panel wide">
      <PanelHeader title="Dashboard" detail={dashboard?.safeMessage ?? "Awaiting local API data."} />
      <div className="metric-grid">
        <Metric label="Imported Players" value={dashboard?.importedPlayersCount ?? 0} note="Safe DTO rows" />
        <Metric label="Shortlists" value={dashboard?.shortlistCount ?? 0} note="Local workflow" />
        <Metric label="Scout Assignments" value={dashboard?.scoutAssignmentCount ?? 0} note="Qualitative reports" />
        <Metric label="Role Templates" value={dashboard?.roleLabTemplateCount ?? 0} note="Generic/import" />
        <Metric label="Benchmarks" value={dashboard?.benchmarkDefinitionCount ?? 0} note="No fake percentiles" />
      </div>
    </section>
  );
}

function RecruitmentPanel({ players }: { players: PlayerListItemDto[] }) {
  return (
    <section className="panel wide">
      <PanelHeader title="Recruitment Board" detail={players.length === 0 ? "No players imported." : "Safe local players from the C# API."} />
      <div className="table">
        <div className="table-row table-head">
          <span>Player</span>
          <span>Role</span>
          <span>Fit</span>
          <span>Confidence</span>
          <span>Decision</span>
        </div>
        {players.length === 0 ? (
          <div className="empty-line">Awaiting local data from Data Sources.</div>
        ) : (
          players.slice(0, 8).map((player) => (
            <div className="table-row" key={player.statlynPlayerId}>
              <span>
                <strong>{player.displayName}</strong>
                <small>{player.primaryPosition} - {player.sourceName}</small>
              </span>
              <span>{player.roleName}</span>
              <span>{formatNullable(player.roleFit)}</span>
              <span>{formatNullable(player.confidence)}</span>
              <span>{player.recommendation}</span>
            </div>
          ))
        )}
      </div>
    </section>
  );
}

function PlayerProfilePanel({ player }: { player: PlayerListItemDto | null }) {
  return (
    <section className="panel">
      <PanelHeader title="Player Profile" detail={player ? "First safe player selected from API data." : "No imported players."} />
      {player ? (
        <div className="profile-stack">
          <h2>{player.displayName}</h2>
          <p>{player.primaryPosition} - {player.nationality}</p>
          <StatusPill label="Benchmark" tone="info" value={player.benchmarkStatus} />
          <StatusPill label="Blocked Fields" tone={player.blockedFieldCount > 0 ? "warning" : "muted"} value={String(player.blockedFieldCount)} />
        </div>
      ) : (
        <div className="empty-line">Import CSV data before opening a player profile.</div>
      )}
    </section>
  );
}

function RoleLabPanel({ state }: { state: ApiState }) {
  return (
    <section className="panel">
      <PanelHeader title="Role Lab" detail={state.roleLab?.safeMessage ?? "No Role Lab data loaded."} />
      <div className="metric-grid compact">
        <Metric label="Roles" value={state.roleLab?.roleCount ?? 0} note="C# templates" />
        <Metric label="Pairs" value={state.roleLab?.rolePairCount ?? 0} note="IP/OOP links" />
      </div>
    </section>
  );
}

function DataSourcesPanel({ state }: { state: ApiState }) {
  return (
    <section className="panel">
      <PanelHeader title="Data Sources" detail={state.dataSources?.safeMessage ?? "Awaiting local data."} />
      <p>{state.dataSources?.fixtureStatus ?? "Synthetic fixture status unavailable."}</p>
      <p>{state.dataSources?.importStatus ?? "No import status yet."}</p>
    </section>
  );
}

function DiagnosticsPanel({ state }: { state: ApiState }) {
  return (
    <section className="panel">
      <PanelHeader title="Diagnostics" detail={state.diagnostics?.safeSummary ?? "Diagnostics unavailable."} />
      <StatusPill label="Schema" tone="info" value={String(state.diagnostics?.schemaVersion ?? 0)} />
      <StatusPill label="FM26" tone="warning" value={state.diagnostics?.fm26Status ?? "Unsupported"} />
      <small>{apiBaseUrl()}</small>
    </section>
  );
}

function ScoutReportsPanel({ state }: { state: ApiState }) {
  return (
    <section className="panel">
      <PanelHeader title="Scout Reports" detail={state.scoutReports.length === 0 ? "No scout assignments." : "Qualitative reports only."} />
      {state.scoutReports.slice(0, 3).map((report) => (
        <div className="mini-row" key={`${report.statlynPlayerId}-${report.latestRecommendation}`}>
          <strong>{report.playerName}</strong>
          <span>{report.latestRecommendation}</span>
        </div>
      ))}
      {state.scoutReports.length === 0 ? <div className="empty-line">No scout reports yet.</div> : null}
    </section>
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

function EmptyWorkflowPanel({ title, text }: { title: string; text: string }) {
  return (
    <section className="panel muted-panel">
      <PanelHeader title={title} detail={text} />
      <div className="empty-line">No fake data is shown.</div>
    </section>
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

function Metric({ label, value, note }: { label: string; value: number; note: string }) {
  return (
    <div className="metric">
      <span>{label}</span>
      <strong>{value}</strong>
      <small>{note}</small>
    </div>
  );
}

function StatusPill({ label, value, tone }: { label: string; value: string; tone: "success" | "warning" | "danger" | "info" | "muted" }) {
  return (
    <div className={`status-pill ${tone}`}>
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}

function LoadingState() {
  return (
    <div className="state-panel">
      <img className="state-mark" src={brandAssets.markWhite} alt="" aria-hidden="true" />
      <span>Loading safe C# API DTOs...</span>
    </div>
  );
}

function ErrorState({ message, onRetry }: { message: string; onRetry: () => void }) {
  return (
    <div className="state-panel danger">
      <strong>Could not load safely.</strong>
      <span>{message}</span>
      <small>Start Statlyn.Api, then refresh the desktop workspace.</small>
      <button className="retry-button" type="button" onClick={onRetry}>
        Retry API connection
      </button>
    </div>
  );
}

function formatNullable(value: number | null): string {
  return value === null ? "Unknown" : String(value);
}
