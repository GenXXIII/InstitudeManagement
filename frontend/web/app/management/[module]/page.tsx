import { ManagementWorkspace } from "@/features/management/management-workspace";

export default async function ManagementPage({ params }: { params: Promise<{ module: string }> }) {
  const { module } = await params;
  return <ManagementWorkspace module={module}/>;
}
