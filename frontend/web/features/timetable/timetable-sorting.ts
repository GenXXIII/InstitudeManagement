import type { TimetableItem } from "./timetable-types";

const days = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];
const shifts = ["Morning", "Afternoon", "Evening", "Weekend"];

function configuredOrder(values: string[], value: string) {
  const position = values.indexOf(value);
  return position === -1 ? values.length : position;
}

export function compareTimetableItems(left: TimetableItem, right: TimetableItem) {
  return configuredOrder(days, left.values.dayOfWeek) - configuredOrder(days, right.values.dayOfWeek)
    || configuredOrder(shifts, left.values.shift) - configuredOrder(shifts, right.values.shift)
    || left.values.startsAt.localeCompare(right.values.startsAt)
    || left.values.timetableCode.localeCompare(right.values.timetableCode, undefined, { numeric: true });
}

export { days as timetableDays };
