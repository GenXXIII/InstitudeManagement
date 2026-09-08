import { notFound, redirect } from "next/navigation";
import AdministrationWorkspace from "@/features/administration/administration-workspace";
import { isSettingSection } from "@/features/administration/administration-config";

export default async function AdministrationSectionPage({ params }: { params: Promise<{ section: string }> }) {
  const { section } = await params;
  if (section === "overview") redirect("/settings");
  if (section !== "maintenance" && !isSettingSection(section)) notFound();
  return <AdministrationWorkspace section={section}/>;
}
