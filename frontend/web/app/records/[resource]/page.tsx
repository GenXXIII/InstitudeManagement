import { redirect } from "next/navigation";
import HistoryWorkspace from "@/features/history/history-workspace";

export default async function HistoryPage({ params }: { params: Promise<{ resource: string }> }) {
  const { resource } = await params;
  if (resource === "grades") redirect("/record-history/students");
  return <HistoryWorkspace/>;
}
