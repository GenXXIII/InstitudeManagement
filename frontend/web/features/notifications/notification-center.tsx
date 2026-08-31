"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import { Icon } from "@/components/icon";
import { notificationApi } from "./notification-api";
import type { NotificationItem } from "./notification-types";

export function NotificationCenter({ open, events, onToggle, onClose }: { open: boolean; events: number; onToggle: () => void; onClose: () => void }) {
  const [notifications, setNotifications] = useState<NotificationItem[]>([]);
  const [error, setError] = useState("");

  const load = useCallback(async () => {
    try {
      setNotifications(await notificationApi.notifications());
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

  const unread = notifications.filter(item => !item.isRead).length;
  const preview = [...notifications]
    .sort((left, right) => Number(left.isRead) - Number(right.isRead) || new Date(right.createAt).getTime() - new Date(left.createAt).getTime())
    .slice(0, 3);

  return <div className="topbar-popover-anchor">
    <button className="icon-button notification-button" aria-label={`Open notifications${unread ? `, ${unread} unread` : ""}`} aria-expanded={open} onClick={onToggle}>
      <Icon name="bell"/>
      {unread > 0 && <span>{unread}</span>}
    </button>
    {open && <aside className="topbar-popover notification-popover notification-center">
      <header><div><strong>Notifications</strong><span>{unread ? `${unread} unread notification${unread === 1 ? "" : "s"}` : "No unread notifications"}</span></div><button onClick={onClose}>Close</button></header>
      {error && <div className="notification-error" role="alert">{error}</div>}
      <div className="notification-center-body">{preview.length
        ? preview.map(item => <NotificationPreview item={item} key={item.id}/>)
        : <p>No notifications.</p>}
      </div>
      <Link className="topbar-popover-link notification-see-more" href="/announce/notifications" onClick={onClose}>View all</Link>
    </aside>}
  </div>;
}

function NotificationPreview({ item }: { item: NotificationItem }) {
  return <article className={item.isRead ? "" : "unread"}>
    <i className={`tone-${item.severity.toLowerCase()}`}/>
    <div><small>{formatDate(item.createAt)} | {formatTime(item.createAt)}</small><span>{item.type}</span><strong>{item.title}</strong></div>
  </article>;
}

function formatDate(value: string) { return new Date(value).toLocaleDateString(); }
function formatTime(value: string) { return new Date(value).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" }); }
