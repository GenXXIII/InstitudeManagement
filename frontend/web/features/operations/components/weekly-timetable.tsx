"use client";

import { useEffect, useState } from "react";
import type { WeeklyTimetableSlot } from "../operations-types";
import { DaySchedule } from "./day-schedule";

const days = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];

export function WeeklyTimetable({ rows }: { rows: WeeklyTimetableSlot[] }) {
  const [now, setNow] = useState(() => new Date());
  useEffect(() => { const timer = window.setInterval(() => setNow(new Date()), 30_000); return () => window.clearInterval(timer); }, []);
  const today = days[(now.getDay() + 6) % 7];
  return <div className="weekly-timetable">{days.map(day => <DaySchedule day={day} today={today} now={now} rows={rows.filter(row => row.day === day)} key={day}/>)}</div>;
}
