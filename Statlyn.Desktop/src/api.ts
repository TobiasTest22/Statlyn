import type {
  ApiState,
  AppHealthDto,
  DashboardOverviewDto,
  DataSourceStatusDto,
  DiagnosticsDto,
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
  const controller = new AbortController();
  const timeout = window.setTimeout(() => controller.abort(), REQUEST_TIMEOUT_MS);

  try {
    const response = await fetch(`${API_BASE}${path}`, {
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
  const [health, dashboard, board, roleLab, dataSources, diagnostics, scoutReports] = await Promise.all([
    getJson<AppHealthDto>("/health"),
    getJson<DashboardOverviewDto>("/dashboard"),
    getJson<RecruitmentBoardDto>("/recruitment-board"),
    getJson<RoleLabSummaryDto>("/role-lab"),
    getJson<DataSourceStatusDto>("/data-sources"),
    getJson<DiagnosticsDto>("/diagnostics"),
    getJson<ScoutReportSummaryDto[]>("/scout-reports")
  ]);

  return { health, dashboard, board, roleLab, dataSources, diagnostics, scoutReports };
}

export function apiBaseUrl(): string {
  return API_BASE;
}
