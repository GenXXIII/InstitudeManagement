import { OperationalRecordDetail } from "@/features/record/operational-record-detail";

export default async function RecordDetailPage({ params }: { params: Promise<{ module: string; id: string }> }) {
  const { module, id } = await params;
  return <OperationalRecordDetail module={module === "class-sessions" ? "sessions" : module} id={id}/>;
}
