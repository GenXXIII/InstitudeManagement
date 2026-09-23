import { Suspense } from "react";
import { CourseSubmissionStatusWorkspace } from "@/features/assessment/course-submission-status-workspace";

export default function CourseSubmissionStatusPage() { return <Suspense fallback={null}><CourseSubmissionStatusWorkspace/></Suspense>; }
