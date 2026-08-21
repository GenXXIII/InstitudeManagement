import type { WeeklyTimetableSlot } from "./operations-types";

export function initials(name: string) {
  return name.split(" ").filter(Boolean).slice(0, 2).map(part => part[0]).join("").toUpperCase();
}

export function statusClass(status: string) {
  return status.toLowerCase().replaceAll(" ", "-");
}

export function isCurrentTime(row: WeeklyTimetableSlot, now: Date) {
  const minutes = (time: string) => { const [hour, minute] = time.split(":").map(Number); return hour * 60 + minute; };
  const current = now.getHours() * 60 + now.getMinutes();
  return current >= minutes(row.startsAt) && current < minutes(row.endsAt);
}
