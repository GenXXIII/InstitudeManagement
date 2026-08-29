import { redirect } from "next/navigation";
import { HistoryDetail } from "@/features/history/history-detail";
import { OperationalRecordDetail } from "@/features/record/operational-record-detail";

export default async function HistoryDetailPage({ params }: { params: Promise<{ resource: string; id: string }> }) {
  const { resource, id } = await params;
  if (resource === "grades" || resource === "results" || resource === "result-semester") redirect("/records/result-semester");
  if (resource === "attendance") redirect("/records/students");
  if (resource === "students" || resource === "teachers" || resource === "courses" || resource === "classrooms") return <OperationalRecordDetail module={resource} id={id} history/>;
  if (resource === "class-sessions") return <OperationalRecordDetail module="sessions" id={id} history/>;
  return <HistoryDetail resource={resource} id={id}/>;
}
