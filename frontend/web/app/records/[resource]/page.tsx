import { redirect } from "next/navigation";
import HistoryWorkspace from "@/features/history/history-workspace";
import { ResultWorkspace } from "@/features/results/result-workspace";

export default async function HistoryPage({ params }: { params: Promise<{ resource: string }> }) {
  const { resource } = await params;
  if (resource === "grades" || resource === "results") redirect("/records/result-semester");
  if (resource === "result-semester") return <ResultWorkspace mode="history"/>;
  return <HistoryWorkspace/>;
}
