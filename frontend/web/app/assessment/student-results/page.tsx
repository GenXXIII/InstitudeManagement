import { Suspense } from "react";
import { StudentResultsWorkspace } from "@/features/assessment/student-results-workspace";

export default function StudentResultsPage() { return <Suspense fallback={null}><StudentResultsWorkspace/></Suspense>; }
