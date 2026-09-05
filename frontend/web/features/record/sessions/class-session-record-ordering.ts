import type { OperationalRecord } from "../record-types";

export type ClassSessionRecordGroup = {
  key: string;
  title: string;
  label: string;
  rows: OperationalRecord[];
};

export function sortClassSessionRecords(rows: OperationalRecord[]) {
  return rows.toSorted((left, right) =>
    sessionSortKey(right).localeCompare(sessionSortKey(left))
    || (left.code || left.subject).localeCompare(right.code || right.subject, undefined, { numeric: true }),
  );
}

export function groupClassSessionRecords(rows: OperationalRecord[]): ClassSessionRecordGroup[] {
  const groups = new Map<string, OperationalRecord[]>();
  for (const row of sortClassSessionRecords(rows)) {
    const key = `${row.academicYear || "Academic year unavailable"}|${row.term || "Semester unavailable"}`;
    groups.set(key, [...(groups.get(key) ?? []), row]);
  }

  return [...groups.entries()]
    .map(([key, semesterRows]) => {
      const [academicYear, term] = key.split("|");
      return {
        key,
        title: academicYear,
        label: `${term} · ${sessionDateRange(semesterRows)}`,
        rows: semesterRows,
      };
    })
    .toSorted((left, right) => right.key.localeCompare(left.key, undefined, { numeric: true, sensitivity: "base" }));
}

function sessionSortKey(row: OperationalRecord) {
  const summary = sessionSummary(row);
  const date = summary?.Date ?? "0000-00-00";
  const time = summary?.Time?.match(/\d{1,2}:\d{2}/)?.[0] ?? "00:00";
  return /^\d{4}-\d{2}-\d{2}$/.test(date) ? `${date}T${time}` : "0000-00-00T00:00";
}

function sessionSummary(row: OperationalRecord) {
  return row.activities.find(activity => activity.Activity === "Completed class")
    ?? row.activities.find(activity => activity.Date && activity.Time);
}

function sessionDateRange(rows: OperationalRecord[]) {
  const dates = rows.map(row => sessionSummary(row)?.Date).filter((date): date is string => Boolean(date)).toSorted();
  if (!dates.length) return "No class dates";
  const latest = displayDate(dates.at(-1)!);
  const earliest = displayDate(dates[0]);
  return dates[0] === dates.at(-1) ? latest : `${earliest} – ${latest}`;
}

function displayDate(value: string) {
  const parsed = new Date(`${value}T00:00:00`);
  return Number.isNaN(parsed.valueOf())
    ? value
    : parsed.toLocaleDateString(undefined, { day: "2-digit", month: "short", year: "numeric" });
}
