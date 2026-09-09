import { request } from "@/lib/http";
import type { AnnouncementDraft, AnnouncementItem } from "./announcement-types";

const route = "/api/notification-center/alerts";

export const announcementsApi = {
  get: () => request<AnnouncementItem[]>(route),
  getItem: (id: string) => request<AnnouncementItem>(`${route}/${id}`),
  create: (values: AnnouncementDraft) => request<AnnouncementItem>(route, { method: "POST", body: JSON.stringify(values) }),
  markRead: (id: string) => request<AnnouncementItem>(`${route}/${id}/read`, { method: "PUT" }),
  update: (id: string, values: AnnouncementDraft) => request<AnnouncementItem>(`${route}/${id}`, { method: "PUT", body: JSON.stringify(values) }),
  remove: (id: string) => request<void>(`${route}/${id}`, { method: "DELETE" }),
};
