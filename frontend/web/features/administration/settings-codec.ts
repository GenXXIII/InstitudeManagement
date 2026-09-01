export function parseCsv(value: string | undefined) {
  if (!value?.trim()) return [];
  const trimmed = value.trim();
  if (trimmed.startsWith("[")) {
    try {
      const parsed = JSON.parse(trimmed) as unknown;
      if (Array.isArray(parsed)) return parsed.filter((item): item is string => typeof item === "string");
    } catch { /* Legacy malformed JSON falls through to CSV. */ }
  }
  return trimmed.split(",").map(item => item.trim()).filter(Boolean);
}

export function formatCsv(values: readonly string[]) {
  return values.map(value => value.trim()).filter(Boolean).join(",");
}

export function selectedCount(value: string | undefined) {
  return parseCsv(value).length;
}

export function formatUpdatedAt(value: string | null) {
  if (!value) return "Not saved yet";
  const date = new Date(value);
  if (Number.isNaN(date.valueOf())) return "Saved configuration";
  return new Intl.DateTimeFormat("en-GB", { dateStyle: "medium", timeStyle: "short" }).format(date);
}
