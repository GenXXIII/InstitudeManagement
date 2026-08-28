export type HistorySearchParams = Record<string, string | string[] | undefined>;

export function historyHref(module: string, searchParams: HistorySearchParams = {}, id?: string) {
  const resource = historyResource(module);
  const path = resource === "result-semester" || !id
    ? `/records/${resource}`
    : `/records/${resource}/${encodeURIComponent(historyKey(resource, id))}`;
  const query = new URLSearchParams();
  for (const [key, value] of Object.entries(searchParams)) {
    if (Array.isArray(value)) value.forEach(item => query.append(key, item));
    else if (value) query.set(key, value);
  }
  return `${path}${query.size ? `?${query}` : ""}`;
}

function historyKey(resource: string, id: string) {
  if (id.includes(":")) return id;
  const types: Record<string, string> = {
    "class-sessions": "Class session", students: "Student", teachers: "Teacher", courses: "Course",
    classrooms: "Classroom", timetable: "Timetable", attendance: "Attendance", departments: "Department",
  };
  return `${types[resource] ?? "Student"}:${id}`;
}

function historyResource(module: string) {
  if (module === "sessions" || module === "class-sessions") return "class-sessions";
  if (module === "results" || module === "grades" || module === "result-semester") return "result-semester";
  return ["students", "teachers", "courses", "classrooms", "timetable", "attendance", "departments"].includes(module) ? module : "students";
}
