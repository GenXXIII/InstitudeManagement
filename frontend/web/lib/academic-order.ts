type AcademicRow = { values?: Record<string, unknown> } | Record<string, unknown>;

export function compareAcademicRows(left: AcademicRow, right: AcademicRow) {
  const leftValues = valuesOf(left);
  const rightValues = valuesOf(right);
  return numberOf(leftValues, ["year", "yearLevel", "yearLevels"]) - numberOf(rightValues, ["year", "yearLevel", "yearLevels"])
    || numberOf(leftValues, ["semester", "term"]) - numberOf(rightValues, ["semester", "term"])
    || shiftOrder(textOf(leftValues, ["shift"])) - shiftOrder(textOf(rightValues, ["shift"]))
    || textOf(leftValues, ["academicYear"]).localeCompare(textOf(rightValues, ["academicYear"]), undefined, { numeric: true, sensitivity: "base" });
}

export function shiftOrder(value: string) {
  const index = ["morning", "afternoon", "evening", "weekend"].indexOf(value.trim().toLowerCase());
  return index < 0 ? 99 : index;
}

export function academicNumber(value: string | number | undefined) {
  const match = String(value ?? "").match(/\d+/);
  return match ? Number(match[0]) : 99;
}

function valuesOf(row: AcademicRow) {
  const nested = "values" in row && row.values && typeof row.values === "object" ? row.values : row;
  return nested as Record<string, unknown>;
}
function numberOf(values: Record<string, unknown>, keys: string[]) { return academicNumber(textOf(values, keys)); }
function textOf(values: Record<string, unknown>, keys: string[]) {
  for (const key of keys) {
    const match = Object.entries(values).find(([candidate]) => candidate.toLowerCase() === key.toLowerCase());
    if (match && match[1] !== null && match[1] !== undefined) return String(match[1]);
  }
  return "";
}
