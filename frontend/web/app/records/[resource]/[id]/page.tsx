import { redirect } from "next/navigation";
import { HistoryDetail } from "@/features/history/history-detail";

export default async function HistoryDetailPage({ params }: { params: Promise<{ resource: string; id: string }> }) {
  const { resource, id } = await params;
  if (resource === "grades") redirect("/record-history/students");
  return <HistoryDetail resource={resource} id={id}/>;
}
