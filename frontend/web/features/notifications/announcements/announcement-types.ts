export type AnnouncementItem = {
  id: string;
  announcementCode: string;
  notificationId?: string;
  type: "General" | "Attendance" | "Emergency" | "Result";
  title: string;
  message: string;
  createAt: string;
};

export type AnnouncementDraft = {
  announcementCode: string;
  type: AnnouncementItem["type"];
  title: string;
  message: string;
};
