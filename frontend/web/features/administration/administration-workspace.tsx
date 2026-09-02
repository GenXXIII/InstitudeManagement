import { AdministrationOverview } from "./components/administration-overview";
import { MaintenanceModePage } from "./components/maintenance-mode-card";
import { SettingsSection } from "./components/settings-section";
import type { SettingSection } from "./administration-types";

export default function AdministrationWorkspace({ section }: { section: SettingSection | "overview" | "maintenance" }) {
  if (section === "overview") return <AdministrationOverview/>;
  if (section === "maintenance") return <MaintenanceModePage/>;
  return <SettingsSection section={section}/>;
}
