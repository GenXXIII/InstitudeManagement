import type { ReactNode } from "react";

export function ManagementDataCell({ label, children, className = "" }: { label: string; children: ReactNode; className?: string }) {
  return <div className={`management-data-cell ${className}`.trim()}><span className="management-cell-label">{label}</span>{children}</div>;
}
