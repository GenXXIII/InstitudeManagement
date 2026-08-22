import type { AttendanceItem } from "../types/attendance";
import { initials } from "../management-utils";
import { ManagementActions } from "./management-actions";

type AttendanceGroup = { studentId: string; student: string; studentCode: string; department: string; records: AttendanceItem[] };

export function AttendanceDesk({ items, onEdit, onDeactivate }: { items: AttendanceItem[]; onEdit: (item: AttendanceItem) => void; onDeactivate: (item: AttendanceItem) => void }) {
  const groups = Array.from(items.reduce((result, item) => {
    const group = result.get(item.values.studentId) ?? { studentId: item.values.studentId, student: item.values.student, studentCode: item.values.studentCode, department: item.values.department, records: [] };
    group.records.push(item);
    result.set(item.values.studentId, group);
    return result;
  }, new Map<string, AttendanceGroup>()).values());

  return <section className="student-record-ledger attendance-student-ledger">{groups.map(group => {
    const records = group.records.toSorted((a, b) => `${b.values.date}${b.values.checkedInAt}`.localeCompare(`${a.values.date}${a.values.checkedInAt}`));
    const present = records.filter(item => item.values.status === "Present").length;
    const absent = records.filter(item => item.values.status === "Absent").length;
    return <article className="panel student-record-row" key={group.studentId}><header><div className="record-business-id"><span>StudentCode</span><strong>{group.studentCode}</strong></div><span className="initial-chip">{initials(group.student)}</span><div className="student-record-identity"><strong>{group.student}</strong><small>{group.department} - {records[0]?.values.academicYear} - {records[0]?.values.term}</small></div><div className="student-record-summary"><span>Total <b>{records.length}</b></span><span>Present <b>{present}</b></span><span>Absent <b>{absent}</b></span></div></header><div className="student-record-cells">{records.map(item => <div className={`attendance-record-cell status-${item.values.status.toLowerCase()}`} key={item.id}><div><time>{item.values.date}</time><strong>{item.values.status} - {item.values.checkedInAt || "No check-in"}</strong><small>Attendance {item.values.attendanceCode} - {item.values.method} - Create At {item.values.createAt}</small></div><ManagementActions item={item} onEdit={onEdit} onDeactivate={onDeactivate}/></div>)}</div></article>;
  })}</section>;
}
