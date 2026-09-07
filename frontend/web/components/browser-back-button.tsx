"use client";

export function BrowserBackButton({ children, className = "button secondary" }: { children: React.ReactNode; className?: string }) {
  return <button className={className} type="button" onClick={() => window.history.back()}>{children}</button>;
}
