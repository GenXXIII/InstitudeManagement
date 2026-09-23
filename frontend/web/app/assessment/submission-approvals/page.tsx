import { Suspense } from "react";
import { SubmissionApprovalsWorkspace } from "@/features/assessment/submission-approvals-workspace";

export default function SubmissionApprovalsPage() { return <Suspense fallback={null}><SubmissionApprovalsWorkspace/></Suspense>; }
