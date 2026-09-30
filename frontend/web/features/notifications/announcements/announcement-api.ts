import { request } from "@/lib/http";
import { appendPage, requestAllPages, type PageOptions, type PagedResult } from "@/lib/pagination";
import type { AnnouncementDraft, AnnouncementItem } from "./announcement-types";

const route = "/api/notification-center/alerts";

export const announcementsApi = {
  get: () => requestAllPages<AnnouncementItem>(route),
  getPage(options: PageOptions = {}, unreadOnly = false, prioritizeUnread = false) {
    const params = new URLSearchParams();
    if (unreadOnly) params.set("unreadOnly", "true");
    if (prioritizeUnread) params.set("prioritizeUnread", "true");
    return request<PagedResult<AnnouncementItem>>(`${route}?${appendPage(params, options)}`);
  },
  getItem: (id: string) => request<AnnouncementItem>(`${route}/${id}`),
  create: (values: AnnouncementDraft) => request<AnnouncementItem>(route, { method: "POST", body: JSON.stringify(values) }),
  markRead: (id: string) => request<AnnouncementItem>(`${route}/${id}/read`, { method: "PUT" }),
  update: (id: string, values: AnnouncementDraft) => request<AnnouncementItem>(`${route}/${id}`, { method: "PUT", body: JSON.stringify(values) }),
  remove: (id: string) => request<void>(`${route}/${id}`, { method: "DELETE" }),
};
