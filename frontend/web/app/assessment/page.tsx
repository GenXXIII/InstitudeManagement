import { Suspense } from "react";
import { AssessmentWorkspace } from "@/features/assessment/assessment-workspace";

export default function AssessmentPage() { return <Suspense fallback={null}><AssessmentWorkspace/></Suspense>; }
