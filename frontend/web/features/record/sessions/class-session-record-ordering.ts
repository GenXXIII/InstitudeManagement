import type { OperationalRecord } from "../record-types";

export type ClassSessionRecordGroup = {
  key: string;
  title: string;
  label: string;
  rows: OperationalRecord[];
};

export function sortClassSessionRecords(rows: OperationalRecord[]) {
  return rows.toSorted((left, right) =>
    sessionSortKey(left).localeCompare(sessionSortKey(right))
    || (left.code || left.subject).localeCompare(right.code || right.subject, undefined, { numeric: true }),
  );
}

export function groupClassSessionRecords(rows: OperationalRecord[]): ClassSessionRecordGroup[] {
  const groups = new Map<string, OperationalRecord[]>();
  for (const row of rows) {
    const date = sessionDate(row);
    groups.set(date, [...(groups.get(date) ?? []), row]);
  }

  return [...groups].map(([date, dateRows]) => ({
    key: date,
    title: displaySessionDate(date),
    label: sessionTimeRange(dateRows),
    rows: dateRows,
  }));
}

function sessionSortKey(row: OperationalRecord) {
  const date = sessionDate(row);
  const time = sessionTimes(row)[0] ?? "23:59";
  return /^\d{4}-\d{2}-\d{2}$/.test(date) ? `${date}T${time}` : "9999-12-31T23:59";
}

function sessionDate(row: OperationalRecord) {
  return sessionSummary(row)?.Date || "Session date unavailable";
}

function sessionTimes(row: OperationalRecord) {
  return sessionSummary(row)?.Time?.match(/\d{1,2}:\d{2}/g) ?? [];
}

function sessionSummary(row: OperationalRecord) {
  return row.activities.find(activity => activity.Activity === "Completed class")
    ?? row.activities.find(activity => activity.Date && activity.Time);
}

function sessionTimeRange(rows: OperationalRecord[]) {
  const times = rows.map(sessionTimes).filter(times => times.length);
  if (!times.length) return "Time unavailable";
  const starts = times.map(time => time[0]).toSorted();
  const ends = times.map(time => time.at(-1) ?? time[0]).toSorted();
  return `${starts[0]} – ${ends.at(-1)}`;
}

function displaySessionDate(value: string) {
  const parsed = new Date(`${value}T00:00:00`);
  return Number.isNaN(parsed.valueOf())
    ? value
    : parsed.toLocaleDateString(undefined, { weekday: "long", day: "2-digit", month: "short", year: "numeric" });
}
