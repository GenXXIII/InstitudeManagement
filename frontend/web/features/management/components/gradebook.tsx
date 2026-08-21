"use client";

import { configuredGrade, useInstituteSettings } from "@/features/administration/institute-settings-context";
import type { CourseItem } from "../types/course";
import type { GradeItem } from "../types/grade";
import { initials } from "../management-utils";
import { ManagementActions } from "./management-actions";

type GradeGroup = { studentId: string; student: string; departmentId: string; department: string; records: GradeItem[] };

export function Gradebook({ items, courses, onEdit, onDeactivate }: { items: GradeItem[]; courses: CourseItem[]; onEdit: (item: GradeItem) => void; onDeactivate: (item: GradeItem) => void }) {
  const { settings } = useInstituteSettings();
  const groups = Array.from(items.reduce((result, item) => {
    const group = result.get(item.values.studentId) ?? { studentId: item.values.studentId, student: item.values.student, departmentId: item.values.departmentId, department: item.values.department, records: [] };
    group.records.push(item); result.set(item.values.studentId, group); return result;
  }, new Map<string, GradeGroup>()).values());

  return <section className="student-record-ledger grade-student-ledger">{groups.map(group => {
    const records = group.records.toSorted((a, b) => a.values.course.localeCompare(b.values.course));
    const departmentCourses = courses.filter(course => course.values.departmentId === group.departmentId && course.values.status !== "Inactive");
    const gradedCourseIds = new Set(records.map(record => record.values.courseId));
    const total = records.reduce((sum, record) => sum + Number(record.values.score || 0), 0);
    const average = records.length ? total / records.length : 0;
    const grade = configuredGrade(average, settings["grade-rules"]);
    return <article className="panel student-record-row" key={group.studentId}><header><span className="initial-chip">{initials(group.student)}</span><div><strong>{group.student}</strong><small>{group.department} · {records[0]?.values.academicYear} · {records[0]?.values.term}</small></div><div className="grade-totals"><span>Total <b>{total.toFixed(1)}</b></span><span>Average <b>{average.toFixed(1)}%</b></span><span>Grade <b className={`grade-letter grade-${grade.toLowerCase()}`}>{grade}</b></span></div></header><div className="student-record-cells">{records.map(item => <div className="grade-record-cell" key={item.id}><div><strong>{item.values.course} {Number(item.values.score).toFixed(1)}/{item.values.grade}</strong><small>{item.values.term}</small></div><ManagementActions item={item} onEdit={onEdit} onDeactivate={onDeactivate}/></div>)}{departmentCourses.filter(course => !gradedCourseIds.has(course.id)).map(course => <div className="grade-record-cell pending-grade" key={course.id}><div><strong>{course.values.name}</strong><small>Grade pending</small></div></div>)}</div></article>;
  })}</section>;
}
