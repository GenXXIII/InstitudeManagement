import { OperationalRecordWorkspace } from "@/features/record/operational-record-workspace";
import { RecordOverview } from "@/features/record/record-overview";

export default async function RecordPage({ params }: { params: Promise<{ module: string }> }) {
  const { module } = await params;
  if (module === "overview") return <RecordOverview/>;
  if (module === "class-sessions") return <OperationalRecordWorkspace module="sessions"/>;
  if (["students", "teachers", "courses", "classrooms", "departments", "timetable"].includes(module)) return <OperationalRecordWorkspace module={module}/>;
  return <RecordOverview/>;
}
