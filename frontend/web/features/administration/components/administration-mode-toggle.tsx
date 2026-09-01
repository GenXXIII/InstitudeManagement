"use client";

import { Icon } from "@/components/icon";
import { useAdministrationMode } from "../administration-mode-context";

export function AdministrationModeToggle({ compact = false }: { compact?: boolean }) {
  const { advanced, setAdvanced } = useAdministrationMode();

  if (compact) return <label className={`administration-mode-compact administration-mode-switch ${advanced ? "is-advanced" : "is-simple"}`}>
    <Icon name="settings" size={15}/>
    <span>Advanced</span>
    <input type="checkbox" checked={advanced} onChange={event => setAdvanced(event.target.checked)} aria-label="Show advanced Settings options"/>
    <i aria-hidden/>
  </label>;

  return <section className={`administration-mode-panel panel ${advanced ? "is-advanced" : "is-simple"}`}>
    <span className="administration-mode-icon"><Icon name="settings" size={19}/></span>
    <div>
      <strong>{advanced ? "Advanced configuration" : "Simple configuration"}</strong>
      <small>{advanced ? "Every available setting is visible." : "Only the essential settings are shown. Saved advanced values stay unchanged."}</small>
    </div>
    <label className="administration-mode-switch">
      <span>Advanced options</span>
      <input type="checkbox" checked={advanced} onChange={event => setAdvanced(event.target.checked)} aria-label="Show advanced Settings options"/>
      <i aria-hidden/>
    </label>
  </section>;
}
