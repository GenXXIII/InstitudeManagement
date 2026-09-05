import type { Activity, ChartPoint, Metric, StatusItem } from "@/lib/types/presentation-types";

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
  todaySchedule: StatusItem[];
  attendanceTrend: ChartPoint[];
  attention: Activity[];
  activity: Activity[];
  departmentStatus: StatusItem[];
  averageGrade: number;
  gradeDistribution: ChartPoint[];
};

export type DashboardRange = "daily" | "weekly" | "monthly" | "yearly" | "all";
