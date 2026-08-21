import type { RecordItem } from "../history-types";
import { displayValue, parseDetails, pretty, slug } from "../history-utils";

export function HistoryEntry({ entry }: { entry: RecordItem }) {
  return <div className="record-history-entry"><div className="record-history-meta"><time>{new Date(entry.date).toLocaleString()}</time><span className={`history-action action-${slug(entry.action)}`}>{entry.action}</span></div><div className="record-history-snapshot">{parseDetails(entry.details).map(([key, value]) => <div key={key}><span>{pretty(key)}</span><strong>{displayValue(key, value)}</strong></div>)}</div></div>;
}
