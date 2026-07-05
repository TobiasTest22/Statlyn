import type {
  ApiState,
  AppHealthDto,
  DashboardOverviewDto,
  DataSourceStatusDto,
  DiagnosticsDto,
  Fm26ConnectorStatusDto,
  Fm26SnapshotCreateResultDto,
  Fm26SnapshotDto,
  Fm26SnapshotHistoryDto,
  MemoryMapRegistryDto,
  RecruitmentBoardDto,
  RoleLabSummaryDto,
  ScoutReportSummaryDto
} from "./types";

const REQUEST_TIMEOUT_MS = 8000;
const API_BASE = normalizeBaseUrl(import.meta.env.VITE_STATLYN_API_URL ?? "http://localhost:5118");

function normalizeBaseUrl(value: string): string {
  return value.replace(/\/+$/, "");
}

async function getJson<T>(path: string): Promise<T> {
  return requestJson<T>(path, { method: "GET" });
}

async function postJson<T>(path: string): Promise<T> {
  return requestJson<T>(path, { method: "POST" });
}

async function requestJson<T>(path: string, init: RequestInit): Promise<T> {
  const controller = new AbortController();
  const timeout = window.setTimeout(() => controller.abort(), REQUEST_TIMEOUT_MS);

  try {
    const response = await fetch(`${API_BASE}${path}`, {
      ...init,
      headers: { Accept: "application/json" },
      signal: controller.signal
    });

    if (!response.ok) {
      throw new Error(`Statlyn.Api returned ${response.status} for ${path}.`);
    }

    return (await response.json()) as T;
  } catch (caught: unknown) {
    if (caught instanceof DOMException && caught.name === "AbortError") {
      throw new Error(`Statlyn.Api did not respond within ${REQUEST_TIMEOUT_MS / 1000} seconds.`);
    }

    if (caught instanceof TypeError) {
      throw new Error(`Cannot reach Statlyn.Api at ${API_BASE}.`);
    }

    throw caught instanceof Error ? new Error(caught.message) : new Error("Could not load safe Statlyn.Api data.");
  } finally {
    window.clearTimeout(timeout);
  }
}

export async function loadWorkspace(): Promise<ApiState> {
  const [health, dashboard, board, roleLab, dataSources, diagnostics, connectorStatus, memoryMaps, fm26Snapshot, fm26SnapshotHistory, scoutReports] = await Promise.all([
    getJson<AppHealthDto>("/health"),
    getJson<DashboardOverviewDto>("/dashboard"),
    getJson<RecruitmentBoardDto>("/recruitment-board"),
    getJson<RoleLabSummaryDto>("/role-lab"),
    getJson<DataSourceStatusDto>("/data-sources"),
    getJson<DiagnosticsDto>("/diagnostics"),
    getJson<Fm26ConnectorStatusDto>("/connector/status"),
    getJson<MemoryMapRegistryDto>("/diagnostics/memory-maps"),
    getJson<Fm26SnapshotDto>("/diagnostics/fm26/snapshot"),
    getJson<Fm26SnapshotHistoryDto>("/diagnostics/fm26/snapshots"),
    getJson<ScoutReportSummaryDto[]>("/scout-reports")
  ]);

  return { health, dashboard, board, roleLab, dataSources, diagnostics, connectorStatus, memoryMaps, fm26Snapshot, fm26SnapshotHistory, scoutReports };
}

export async function createPersistedFm26Snapshot(): Promise<Fm26SnapshotCreateResultDto> {
  return postJson<Fm26SnapshotCreateResultDto>("/diagnostics/fm26/snapshots");
}

export function apiBaseUrl(): string {
  return API_BASE;
}
