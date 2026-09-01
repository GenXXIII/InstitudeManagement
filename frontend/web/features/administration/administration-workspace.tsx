import { AdministrationOverview } from "./components/administration-overview";
import { SettingsSection } from "./components/settings-section";
import type { SettingSection } from "./administration-types";

export default function AdministrationWorkspace({ section }: { section: SettingSection | "overview" }) {
  if (section === "overview") return <AdministrationOverview/>;
  return <SettingsSection section={section}/>;
}
