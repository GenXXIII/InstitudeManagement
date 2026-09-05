import Link from "next/link";
import { Icon } from "@/components/icon";
import type { AnnouncementItem } from "../announcements/announcement-types";
import type { NotificationHistoryItem } from "../history/notification-history-types";
import type { NotificationItem } from "../notifications/notification-types";

export function AnnounceOverview({ notifications, alerts, history }: {
  notifications: NotificationItem[];
  alerts: AnnouncementItem[];
  history: NotificationHistoryItem[];
}) {
  const recent = [
    ...notifications.map(item => ({
      code: item.notificationCode,
      kind: "Notification",
      title: item.title,
      detail: item.message,
      date: item.createAt,
      href: `/announce/notifications/${item.id}`,
    })),
    ...alerts.map(item => ({
      code: item.announcementCode,
      kind: "Alert",
      title: item.title,
      detail: item.message,
      date: item.createAt,
      href: "/announce/alerts",
    })),
    ...history.map(item => ({
      code: item.notificationHistoryCode,
      kind: "Notification history",
      title: item.title,
      detail: `${item.sourceCode} · ${item.action}`,
      date: item.createAt,
      href: `/announce/history/${encodeURIComponent(item.notificationHistoryCode)}`,
    })),
  ].toSorted((left, right) => new Date(right.date).getTime() - new Date(left.date).getTime()).slice(0, 8);
  const areas = [
    {
      label: "Notifications",
      count: notifications.length,
      detail: `${notifications.filter(item => !item.isRead).length} unread`,
      href: "/announce/notifications",
      icon: "bell" as const,
    },
    {
      label: "Active alerts",
      count: alerts.length,
      detail: "Institute announcements",
      href: "/announce/alerts",
      icon: "pulse" as const,
    },
    {
      label: "History",
      count: history.length,
      detail: "Permanent lifecycle entries",
      href: "/announce/history",
      icon: "archive" as const,
    },
  ];

  return <>
    <section className="announce-overview-metrics">
      {areas.map(area => <Link className="panel announce-overview-metric" href={area.href} key={area.label}>
        <span><Icon name={area.icon} size={17}/></span>
        <div>
          <small>{area.label}</small>
          <strong>{area.count}</strong>
          <p>{area.detail}</p>
        </div>
        <Icon name="arrow" size={14}/>
      </Link>)}
    </section>
    <section className="panel announce-overview-register">
      <header>
        <span>FeatureCode</span>
        <span>Feature</span>
        <span>Latest title and detail</span>
        <span>Create At</span>
        <span>Open</span>
      </header>
      {recent.map(item => <Link href={item.href} key={`${item.kind}-${item.code}`}>
        <strong className="management-code-value">{item.code}</strong>
        <span className="table-status">{item.kind}</span>
        <div>
          <strong>{item.title}</strong>
          <small>{item.detail}</small>
        </div>
        <time>{new Date(item.date).toLocaleString()}</time>
        <Icon name="arrow" size={14}/>
      </Link>)}
      {!recent.length && <div className="empty-state">
        <strong>No announcement activity</strong>
        <span>Notifications, alerts, and their coded history will appear here.</span>
      </div>}
    </section>
  </>;
}
