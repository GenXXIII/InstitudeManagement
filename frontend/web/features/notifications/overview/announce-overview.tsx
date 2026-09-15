import Link from "next/link";
import { DataTable } from "@/components/data-table";
import { Icon } from "@/components/icon";
import type { AnnouncementItem } from "../announcements/announcement-types";
import type { NotificationItem } from "../notifications/notification-types";

export function AnnounceOverview({ notifications, alerts }: {
  notifications: NotificationItem[];
  alerts: AnnouncementItem[];
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
      href: `/announce/alerts/${item.id}`,
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
    <DataTable as="section" className="panel horizontal-management-table announce-overview-register" headerClassName="horizontal-management-head" rowSelector=":scope > .announce-overview-row" columns={[{ key: "feature-code", label: "FeatureCode", align: "center" }, "Feature", { key: "latest-detail", label: "Latest detail", align: "left" }, "Create At", { key: "open", label: "Open", align: "center" }]}>
      {recent.map(item => <Link className="horizontal-management-row announce-overview-row" href={item.href} key={`${item.kind}-${item.code}`}>
        <strong className="management-code-value">{item.code}</strong>
        <span className="table-status">{item.kind}</span>
        <div><span>{item.detail}</span></div>
        <time>{new Date(item.date).toLocaleString()}</time>
        <span className="announce-overview-open">View <Icon name="arrow" size={14}/></span>
      </Link>)}
      {!recent.length && <div className="empty-state">
        <strong>No announcement activity</strong>
        <span>Current notifications and alerts will appear here.</span>
      </div>}
    </DataTable>
  </>;
}
