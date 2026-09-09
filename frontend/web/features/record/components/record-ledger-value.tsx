import type { ReactNode } from "react";

export function RecordLedgerValue({ value, className = "", title }: { value: ReactNode; className?: string; title?: string }) {
  return <span className={`record-ledger-value ${className}`.trim()} title={title}>{value}</span>;
}
