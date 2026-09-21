import type { ChartPoint, Metric, StatusItem } from "@/lib/types/presentation-types";

export type Dashboard = {
  range: DashboardRange;
  rangeLabel: string;
  periodStart: string;
  periodEnd: string;
  generatedAt: string;
  metrics: Metric[];
  attendanceRate: number;
  attendanceChange: number;
  liveStatus: StatusItem[];
  attendanceTrend: ChartPoint[];
  departmentStatus: StatusItem[];
  averageGrade: number;
  gradeDistribution: ChartPoint[];
  finance: FinanceSummary;
};

export type FinanceSummary = {
  currency: string;
  periodLabel: string;
  totalDue: number;
  collected: number;
  outstanding: number;
  collectionRate: number;
  paidAccounts: number;
  openAccounts: number;
};

export type DashboardRange = "daily" | "weekly" | "monthly" | "yearly" | "all";
