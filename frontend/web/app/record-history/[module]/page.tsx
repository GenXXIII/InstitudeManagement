import { redirect } from "next/navigation";
import { OperationalRecordWorkspace } from "@/features/record/operational-record-workspace";

export default async function RecordHistoryPage({ params }: { params: Promise<{ module: string }> }) {
  const { module } = await params;
  if (module === "results" || module === "grades") redirect("/record-history/students");
  return <OperationalRecordWorkspace module={module} history/>;
}
