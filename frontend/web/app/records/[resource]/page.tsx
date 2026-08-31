import { redirect } from "next/navigation";
import HistoryWorkspace from "@/features/history/history-workspace";
import { HistoryOverview } from "@/features/history/history-overview";
import { OperationalRecordWorkspace } from "@/features/record/operational-record-workspace";
import { ResultWorkspace } from "@/features/results/result-workspace";

export default async function HistoryPage({ params }: { params: Promise<{ resource: string }> }) {
  const { resource } = await params;
  if (resource === "grades" || resource === "results") redirect("/records/result-semester");
  if (resource === "attendance") redirect("/records/students");
  if (resource === "overview") return <HistoryOverview/>;
  if (resource === "students" || resource === "teachers" || resource === "courses" || resource === "classrooms" || resource === "departments" || resource === "timetable") return <OperationalRecordWorkspace module={resource} history/>;
  if (resource === "class-sessions") return <OperationalRecordWorkspace module="sessions" history/>;
  if (resource === "result-semester") return <ResultWorkspace mode="history"/>;
  return <HistoryWorkspace/>;
}
