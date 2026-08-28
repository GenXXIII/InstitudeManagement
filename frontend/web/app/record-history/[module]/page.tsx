import { redirect } from "next/navigation";
import { historyHref, type HistorySearchParams } from "@/features/history/history-route";

export default async function RecordHistoryPage({ params, searchParams }: { params: Promise<{ module: string }>; searchParams: Promise<HistorySearchParams> }) {
  const [{ module }, query] = await Promise.all([params, searchParams]);
  redirect(historyHref(module, query));
}
