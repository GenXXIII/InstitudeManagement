import type { Activity, ChartPoint, Metric, StatusItem } from "@/lib/types/presentation-types";

export type Dashboard = {
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
