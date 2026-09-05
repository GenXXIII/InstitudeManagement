"use client";

import { useEffect, useState } from "react";
import { usePathname } from "next/navigation";
import { useInstituteSettings } from "@/features/administration/institute-settings-context";
import { AppTopbar } from "./app-topbar";
import { MaintenanceScreen } from "./maintenance-screen";
import { Sidebar } from "./sidebar";
import { useLiveUpdates } from "./use-live-updates";
import { useRecordEntryNavigation } from "./use-record-entry-navigation";
import { useShellScopes } from "./use-shell-scopes";

export function AppShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const { settings, ready } = useInstituteSettings();
  const [navigationOpen, setNavigationOpen] = useState(false);
  const institute = settings.institute;
  const system = settings.system;
  const settingsRoute = pathname.startsWith("/settings");
  const classSessionRoute = pathname.startsWith("/record/class-sessions") || pathname.startsWith("/records/class-sessions");
  const maintenanceActive = ready && system.maintenanceEnabled === "true";
  const maintenanceSettingsRoute = pathname === "/settings/maintenance";
  const { live, events } = useLiveUpdates(ready && !maintenanceActive);
  const scopes = useShellScopes(pathname, ready && !maintenanceActive);

  useRecordEntryNavigation(pathname);

  useEffect(() => {
    document.documentElement.lang = system.language?.toLowerCase().startsWith("kh") ? "km" : "en";
    document.title = institute.name || "Institude of New Khmer";
  }, [institute.name, system.language]);

  const instituteName = institute.name || "Institude of New Khmer";
  const logoUrl = institute.logoUrl || "/branding/ink-logo.png";
  if (maintenanceActive && !maintenanceSettingsRoute) {
    return <MaintenanceScreen
      instituteName={instituteName}
      logoUrl={logoUrl}
      message={system.maintenanceMessage || "System is currently under maintenance. Please try again later."}
    />;
  }

  return <div className="app-frame">
    <div className="ambient ambient-one"/><div className="ambient ambient-two"/>
    <Sidebar
      open={navigationOpen}
      live={live}
      instituteName={instituteName}
      shortName={institute.shortName || "INK"}
      logoUrl={logoUrl}
      departmentScope={scopes.departmentScope}
      yearScope={classSessionRoute ? "" : scopes.yearScope}
      onClose={() => setNavigationOpen(false)}
    />
    {navigationOpen && <button className="backdrop" onClick={() => setNavigationOpen(false)} aria-label="Close navigation"/>}
    <div className="workspace">
      <AppTopbar
        academicYear={settings["academic-year"]}
        avatar={(institute.shortName || "INK").slice(0, 2).toUpperCase()}
        departmentOptions={scopes.departmentOptions}
        departmentScope={scopes.departmentScope}
        events={events}
        institute={institute}
        onOpenMenu={() => setNavigationOpen(true)}
        onScopeChange={scopes.changeScope}
        semester={settings.semester}
        settingsRoute={settingsRoute}
        showYearScope={!classSessionRoute}
        system={system}
        yearScope={classSessionRoute ? "" : scopes.yearScope}
      />
      <main className="content">{children}</main>
    </div>
  </div>;
}
