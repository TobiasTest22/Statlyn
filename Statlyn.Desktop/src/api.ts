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

const API_BASE = import.meta.env.VITE_STATLYN_API_URL ?? "http://localhost:5118";

async function getJson<T>(path: string): Promise<T> {
  const response = await fetch(`${API_BASE}${path}`);
  if (!response.ok) {
    throw new Error(`API request failed: ${path}`);
  }

  return (await response.json()) as T;
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
