export function timetableEnrollmentDefaults(year: string): Record<string, string> {
  return { yearLevel: year || "1", dayOfWeek: "Monday", startsAt: "07:30", endsAt: "09:00", status: "Upcoming" };
}
