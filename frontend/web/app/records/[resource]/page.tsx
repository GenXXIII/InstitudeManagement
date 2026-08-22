"use client";

import { useParams } from "next/navigation";
import HistoryWorkspace from "@/features/history/history-workspace";
import { ResultWorkspace } from "@/features/results/result-workspace";

export default function HistoryPage() {
  const { resource } = useParams<{ resource: string }>();
  return resource === "results" ? <ResultWorkspace mode="history"/> : <HistoryWorkspace/>;
}
