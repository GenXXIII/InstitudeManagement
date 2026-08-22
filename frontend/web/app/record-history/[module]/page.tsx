"use client";

import { useParams } from "next/navigation";
import { OperationalRecordWorkspace } from "@/features/record/operational-record-workspace";
import { ResultWorkspace } from "@/features/results/result-workspace";

export default function RecordHistoryPage() {
  const { module } = useParams<{ module: string }>();
  return module === "results" ? <ResultWorkspace mode="history"/> : <OperationalRecordWorkspace module={module} history/>;
}
