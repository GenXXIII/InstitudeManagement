import { Icon } from "./icon";
import type { Activity, Metric } from "@/lib/types";

export function PageHeading({ eyebrow, title, description, actions }: { eyebrow: string; title: string; description: string; actions?: React.ReactNode }) {
  return <div className="page-heading"><div><div className="eyebrow">{eyebrow}</div><h1>{title}</h1><p>{description}</p></div>{actions && <div className="heading-actions">{actions}</div>}</div>;
}

export function MetricCards({ metrics }: { metrics: Metric[] }) {
  return <section className="metrics-grid">{metrics.map((item, index) => <article className={`metric-card tone-${item.tone || "blue"}`} key={item.label}>
    <div className="metric-top"><span className="metric-icon"><Icon name={(["users", "teacher", "room", "book", "check"] as const)[index % 5]}/></span><span className="trend-pill">{index % 2 === 0 ? "↗" : "•"}</span></div>
    <span className="metric-label">{item.label}</span><strong>{item.value}</strong><small>{item.detail}</small>
  </article>)}</section>;
}

export function ActivityList({ items, empty = "No recent activity" }: { items: Activity[]; empty?: string }) {
  if (!items.length) return <div className="empty-small">{empty}</div>;
  return <div className="activity-list">{items.map((item, index) => <div className="activity-row" key={`${item.time}-${item.title}-${index}`}><span className={`activity-mark ${item.tone}`}/><div><strong>{item.title}</strong><span>{item.detail}</span></div><time>{item.time}</time></div>)}</div>;
}

export function DataTable({ rows }: { rows: Record<string, string>[] }) {
  if (!rows.length) return <div className="empty-state"><div className="empty-icon"><Icon name="archive" size={28}/></div><strong>No records yet</strong><span>Data added to this section will appear here.</span></div>;
  const columns = Object.keys(rows[0]);
  return <div className="table-wrap"><table><thead><tr>{columns.map(column => <th key={column}>{column}</th>)}</tr></thead><tbody>{rows.map((row, i) => <tr key={i}>{columns.map(column => <td key={column}>{column.toLowerCase() === "status" || column.toLowerCase() === "device" ? <span className={`table-status ${row[column].toLowerCase().replace(" ", "-")}`}>{row[column]}</span> : row[column]}</td>)}</tr>)}</tbody></table></div>;
}

export function LoadingPage() { return <div className="loading-page"><div className="loading-title"/><div className="loading-copy"/><div className="loading-grid">{[1,2,3,4].map(x => <div key={x}/>)}</div></div>; }
export function ErrorPage({ retry }: { retry: () => void }) { return <div className="error-panel"><span>Connection unavailable</span><h2>We couldn’t reach the institute API.</h2><p>Start the backend on port 5080, then try again.</p><button className="button primary" onClick={retry}>Try again</button></div>; }
