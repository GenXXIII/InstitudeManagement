import { StudentResultDetail } from "@/features/assessment/student-result-detail";

export default async function StudentResultDetailPage({ params }: { params: Promise<{ studentId: string }> }) {
  const { studentId } = await params;
  return <StudentResultDetail studentId={decodeURIComponent(studentId)}/>;
}
