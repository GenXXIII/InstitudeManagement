import type { SettingFieldDefinition, SettingFieldType, SettingOption } from "../administration-types";

type Extras = Omit<SettingFieldDefinition, "key" | "label" | "description" | "type">;

export function field(key: string, label: string, description: string, type: SettingFieldType = "text", extras: Extras = {}): SettingFieldDefinition {
  return { key, label, description, type, ...extras };
}

export function options(...values: string[]): readonly SettingOption[] {
  return values.map(value => ({ value, label: value }));
}

export function labelledOptions(values: ReadonlyArray<readonly [string, string]>): readonly SettingOption[] {
  return values.map(([value, label]) => ({ value, label }));
}

export function utcOffset(values: Record<string, string>) {
  const timeZone = values.timeZone || "Asia/Phnom_Penh";
  try {
    const part = new Intl.DateTimeFormat("en", { timeZone, timeZoneName: "longOffset" }).formatToParts(new Date()).find(item => item.type === "timeZoneName")?.value;
    return (part || "GMT+07:00").replace("GMT", "UTC");
  } catch { return "Select a valid IANA time zone"; }
}
