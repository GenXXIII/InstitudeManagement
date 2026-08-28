import { redirect } from "next/navigation";
import { HistoryDetail } from "@/features/history/history-detail";

export default async function HistoryDetailPage({ params }: { params: Promise<{ resource: string; id: string }> }) {
  const { resource, id } = await params;
  if (resource === "grades" || resource === "results" || resource === "result-semester") redirect("/records/result-semester");
  return <HistoryDetail resource={resource} id={id}/>;
}
