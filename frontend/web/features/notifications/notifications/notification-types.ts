export type NotificationItem = {
  id: string;
  notificationCode: string;
  type: string;
  title: string;
  message: string;
  severity: "Info" | "Warning" | "Critical";
  isRead: boolean;
  createAt: string;
};

export type NotificationDraft = {
  title: string;
  message: string;
  severity: NotificationItem["severity"];
  isRead: boolean;
};
