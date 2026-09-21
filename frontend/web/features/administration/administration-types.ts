export const settingSections = [
  "institute",
  "academic-year",
  "semester",
  "finance",
  "departments",
  "courses",
  "classrooms",
  "code-formats",
  "users-access",
  "student-rules",
  "teacher-rules",
  "attendance-rules",
  "grade-rules",
  "notifications",
  "system",
  "security",
] as const;

export type SettingSection = typeof settingSections[number];
export type InstituteSettings = Record<SettingSection, Record<string, string>>;

export type Settings = {
  section: SettingSection;
  values: Record<string, string>;
  isConfigured: boolean;
  updatedAtUtc: string | null;
};

export type AdministrationCategory = "essential" | "calendar" | "finance" | "structure" | "people" | "policies" | "communication" | "platform";
export type AdministrationIcon = "building" | "calendar" | "book" | "room" | "users" | "teacher" | "check" | "finance" | "grade" | "bell" | "settings" | "archive";
export type SettingFieldType = "text" | "textarea" | "email" | "tel" | "url" | "number" | "date" | "select" | "toggle" | "multiselect" | "checklist" | "asset" | "derived";

export type SettingOption = { value: string; label: string };

export type SettingFieldDefinition = {
  key: string;
  label: string;
  description: string;
  type: SettingFieldType;
  required?: boolean;
  options?: readonly SettingOption[];
  min?: number;
  max?: number;
  step?: number;
  unit?: string;
  placeholder?: string;
  accept?: string;
  assetKind?: "logo" | "favicon";
  readOnly?: boolean;
  showWhen?: (values: Record<string, string>) => boolean;
  derive?: (values: Record<string, string>) => string;
};

export type ConfigurationGroup = {
  title: string;
  description: string;
  fields: readonly SettingFieldDefinition[];
};

export type ManagementLink = {
  title: string;
  description: string;
  href?: string;
  label: string;
};

export type AdministrationSectionDefinition = {
  section: SettingSection;
  title: string;
  shortTitle: string;
  description: string;
  category: AdministrationCategory;
  icon: AdministrationIcon;
  groups: readonly ConfigurationGroup[];
  managementLinks?: readonly ManagementLink[];
};
