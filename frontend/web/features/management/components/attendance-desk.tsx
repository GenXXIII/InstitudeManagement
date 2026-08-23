import type { AttendanceItem } from "../types/attendance";
import { initials } from "../management-utils";
import { ManagementActions } from "./management-actions";
import { ManagementDataCell } from "./management-data-cell";

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
    return <article className="panel student-record-row" key={group.studentId}><header>
      <ManagementDataCell label="StudentID" className="record-business-id"><strong>{group.studentCode}</strong></ManagementDataCell>
      <ManagementDataCell label="Photo" className="record-photo-cell"><span className="initial-chip">{initials(group.student)}</span></ManagementDataCell>
      <ManagementDataCell label="Student Name" className="student-record-identity"><strong>{group.student}</strong><small>{group.department} - {records[0]?.values.academicYear} - {records[0]?.values.term}</small></ManagementDataCell>
      <ManagementDataCell label="Attendance summary" className="student-record-summary"><span><small>Total</small><b>{records.length}</b></span><span><small>Present</small><b>{present}</b></span><span><small>Absent</small><b>{absent}</b></span></ManagementDataCell>
    </header><div className="student-record-cells">{records.map(item => <div className={`attendance-record-cell status-${item.values.status.toLowerCase()}`} key={item.id}><div className="record-field-grid attendance-field-grid">
      <ManagementDataCell label="AttendanceID"><strong>{item.values.attendanceCode}</strong></ManagementDataCell>
      <ManagementDataCell label="Date"><strong>{item.values.date}</strong></ManagementDataCell>
      <ManagementDataCell label="Check-in"><strong>{item.values.checkedInAt || "No check-in"}</strong></ManagementDataCell>
      <ManagementDataCell label="Attendance"><strong>{item.values.status}</strong></ManagementDataCell>
      <ManagementDataCell label="Method"><strong>{item.values.method}</strong></ManagementDataCell>
      <ManagementDataCell label="Create At"><strong>{item.values.createAt}</strong></ManagementDataCell>
    </div><ManagementDataCell label="Actions" className="management-action-cell"><ManagementActions item={item} onEdit={onEdit} onDeactivate={onDeactivate}/></ManagementDataCell></div>)}</div></article>;
  })}</section>;
}
