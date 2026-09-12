"use client";

import Link from "next/link";
import { useCallback, useEffect, useRef, useState } from "react";
import { Icon } from "@/components/icon";
import { announcementsApi } from "../announcements/announcement-api";
import type { AnnouncementItem } from "../announcements/announcement-types";
import { notificationsApi } from "./notification-api";
import type { NotificationItem } from "./notification-types";

export function NotificationCenter({ open, events, onToggle, onClose }: { open: boolean; events: number; onToggle: () => void; onClose: () => void }) {
  const container = useRef<HTMLDivElement>(null);
  const [notifications, setNotifications] = useState<NotificationItem[]>([]);
  const [alerts, setAlerts] = useState<AnnouncementItem[]>([]);
  const [tab, setTab] = useState<"all" | "system" | "alerts">("all");
  const [error, setError] = useState("");

  const load = useCallback(async () => {
    try {
      const [nextNotifications, nextAlerts] = await Promise.all([notificationsApi.get(), announcementsApi.get()]);
      setNotifications(nextNotifications);
      setAlerts(nextAlerts);
      setError("");
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Could not load notifications.");
    }
  }, []);

  useEffect(() => {
    const timer = window.setTimeout(() => void load(), 0);
    return () => window.clearTimeout(timer);
  }, [load, open, events]);
  useEffect(() => {
    const refresh = () => void load();
    window.addEventListener("ink:notifications-changed", refresh);
    return () => window.removeEventListener("ink:notifications-changed", refresh);
  }, [load]);
  useEffect(() => {
    if (!open) return;
    const closeOutside = (event: PointerEvent) => {
      if (container.current && !container.current.contains(event.target as Node)) onClose();
    };
    document.addEventListener("pointerdown", closeOutside);
    return () => document.removeEventListener("pointerdown", closeOutside);
  }, [onClose, open]);

  const items: CenterItem[] = [
    ...notifications.map(item => ({ id: item.id, kind: "System" as const, type: item.severity, title: item.title, isRead: item.isRead, createAt: item.createAt, href: `/announce/notifications/${item.id}` })),
    ...alerts.map(item => ({ id: item.id, kind: "Alert" as const, type: item.type, title: item.title, isRead: item.isRead, createAt: item.createAt, href: `/announce/alerts/${item.id}` })),
  ];
  const unread = items.filter(item => !item.isRead).length;
  const preview = items
    .filter(item => tab === "all" || item.kind === (tab === "system" ? "System" : "Alert"))
    .sort((left, right) => Number(left.isRead) - Number(right.isRead) || new Date(right.createAt).getTime() - new Date(left.createAt).getTime())
    .slice(0, 3);

  return <div className="topbar-popover-anchor" ref={container}>
    <button className="icon-button notification-button" aria-label={open ? "Close notifications" : `Open notifications${unread ? `, ${unread} unread` : ""}`} aria-expanded={open} onClick={onToggle}>
      <Icon name="bell"/>
      {unread > 0 && <span>{unread}</span>}
    </button>
    {open && <aside className="topbar-popover notification-popover notification-center">
      <header><div><strong>Notifications</strong><span>{unread ? `${unread} unread notification${unread === 1 ? "" : "s"}` : "No unread notifications"}</span></div></header>
      {error && <div className="notification-error" role="alert">{error}</div>}
      <div className="notification-tabs" role="tablist" aria-label="Notification type">
        <button className={tab === "all" ? "active" : ""} role="tab" aria-selected={tab === "all"} onClick={() => setTab("all")}>All</button>
        <button className={tab === "system" ? "active" : ""} role="tab" aria-selected={tab === "system"} onClick={() => setTab("system")}>System</button>
        <button className={tab === "alerts" ? "active" : ""} role="tab" aria-selected={tab === "alerts"} onClick={() => setTab("alerts")}>Alerts</button>
      </div>
      <div className="notification-center-body">{preview.length
        ? preview.map(item => <NotificationPreview item={item} onOpen={onClose} key={`${item.kind}-${item.id}`}/>)
        : <p>No notifications.</p>}
      </div>
      <Link className="topbar-popover-link notification-see-more" href={tab === "system" ? "/announce/notifications" : tab === "alerts" ? "/announce/alerts" : "/announce"} onClick={onClose}>View all</Link>
    </aside>}
  </div>;
}

type CenterItem = {
  id: string;
  kind: "System" | "Alert";
  type: string;
  title: string;
  isRead: boolean;
  createAt: string;
  href: string;
};

function NotificationPreview({ item, onOpen }: { item: CenterItem; onOpen: () => void }) {
  return <article className={item.isRead ? "" : "unread"}>
    <i className={item.kind === "Alert" ? `alert-${item.type.toLowerCase()}` : `tone-${item.type.toLowerCase()}`}/>
    <Link className="notification-center-copy" href={item.href} onClick={onOpen}><small>{formatDate(item.createAt)} | {formatTime(item.createAt)}</small><span>{item.kind} · {item.type}</span><strong>{item.title}</strong></Link>
  </article>;
}

function formatDate(value: string) { return new Date(value).toLocaleDateString(); }
function formatTime(value: string) { return new Date(value).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" }); }
