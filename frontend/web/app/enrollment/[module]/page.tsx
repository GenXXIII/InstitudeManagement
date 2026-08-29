"use client";

import { useParams } from "next/navigation";
import { EnrollmentWorkspace } from "@/features/enrollment/enrollment-workspace";
import { EnrollmentOverview } from "@/features/enrollment/enrollment-overview";
import type { EnrollmentResource } from "@/features/enrollment/enrollment-api";

export default function EnrollmentPage() {
  const { module } = useParams<{ module: string }>();
  if (module === "overview") return <EnrollmentOverview/>;
  if (["students", "student-assignments", "teachers", "courses", "classrooms", "timetable", "departments"].includes(module)) return <EnrollmentWorkspace resource={module as EnrollmentResource}/>;
  return <EnrollmentOverview/>;
}
