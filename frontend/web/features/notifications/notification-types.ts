export type NotificationItem = { id: string; notificationCode: string; type: string; title: string; message: string; severity: "Info" | "Warning" | "Critical"; isRead: boolean; createAt: string };
export type AnnouncementItem = { id: string; announcementCode: string; notificationId?: string; type: "General" | "Attendance" | "Emergency" | "Result"; title: string; message: string; createAt: string };
export type NotificationHistoryItem = { id: string; notificationHistoryCode: string; sourceId: string; sourceCode: string; kind: string; type: string; title: string; message: string; action: string; createAt: string };
export type NotificationDraft = { title: string; message: string; severity: NotificationItem["severity"]; isRead: boolean };
export type AnnouncementDraft = { type: AnnouncementItem["type"]; title: string; message: string };
