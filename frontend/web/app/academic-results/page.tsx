import { Suspense } from "react";
import { ResultWorkspace } from "@/features/results/result-workspace";

export default function AcademicResultsPage() { return <Suspense fallback={null}><ResultWorkspace mode="current"/></Suspense>; }
