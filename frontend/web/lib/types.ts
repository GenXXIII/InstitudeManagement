export type Metric = { label: string; value: string; detail: string; tone: string };
export type Activity = { time: string; title: string; detail: string; tone: string };
export type StatusItem = { label: string; value: string; detail: string; status: string };
export type ChartPoint = { label: string; value: number };

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
  gradeDistribution: ChartPoint[];
};

export type Operation = {
  module: string;
  title: string;
  description: string;
  metrics: Metric[];
  rows: Record<string, string>[];
  activity: Activity[];
  attention: Activity[];
};

export type CatalogItem = { id: string; values: Record<string, string> };
export type RecordItem = { id: string; date: string; type: string; subject: string; action: string; details: string };
export type Settings = { section: string; values: Record<string, string> };
