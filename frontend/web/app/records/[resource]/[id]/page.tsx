import { redirect } from "next/navigation";
import { HistoryDetail } from "@/features/history/history-detail";
import { OperationalRecordDetail } from "@/features/record/operational-record-detail";
import { NotificationHistoryDetail } from "@/features/notifications/history/notification-history-detail";
import { AssessmentHistoryDetail } from "@/features/history/assessment-history-detail";

export default async function HistoryDetailPage({ params }: { params: Promise<{ resource: string; id: string }> }) {
  const { resource, id } = await params;
  if (resource === "grades" || resource === "results" || resource === "result-semester") redirect("/records/result-semester");
  if (resource === "attendance") redirect("/records/students");
  if (resource === "notifications") return <NotificationHistoryDetail id={id}/>;
  if (resource === "assessment") return <AssessmentHistoryDetail id={id}/>;
  if (resource === "students" || resource === "teachers" || resource === "courses" || resource === "classrooms" || resource === "departments" || resource === "timetable") return <OperationalRecordDetail module={resource} id={id} history/>;
  if (resource === "class-sessions") return <OperationalRecordDetail module="sessions" id={id} history/>;
  return <HistoryDetail resource={resource} id={id}/>;
}
