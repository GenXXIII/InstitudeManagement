"use client";

import { useParams } from "next/navigation";
import OperationsWorkspace from "@/features/operations/operations-workspace";
import { ResultWorkspace } from "@/features/results/result-workspace";

export default function OperationPage() {
  const { module } = useParams<{ module: string }>();
  return module === "results" ? <ResultWorkspace mode="operation"/> : <OperationsWorkspace/>;
}
