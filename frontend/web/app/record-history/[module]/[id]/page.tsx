import { redirect } from "next/navigation";
import { historyHref, type HistorySearchParams } from "@/features/history/history-route";

export default async function RecordHistoryDetailPage({ params, searchParams }: { params: Promise<{ module: string; id: string }>; searchParams: Promise<HistorySearchParams> }) {
  const [{ module, id }, query] = await Promise.all([params, searchParams]);
  redirect(historyHref(module, query, id));
}
