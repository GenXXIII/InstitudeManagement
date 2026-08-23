"use client";

import { configuredGrade, useInstituteSettings } from "@/features/administration/institute-settings-context";
import { useEffect, useState } from "react";
import type { CourseItem } from "../types/course";
import type { GradeItem } from "../types/grade";
import type { StudentItem } from "../types/student";
import { initials } from "../management-utils";
import { ManagementActions } from "./management-actions";
import { ManagementDataCell } from "./management-data-cell";

type GradeGroup = { studentId: string; student: string; studentCode: string; departmentId: string; department: string; records: GradeItem[] };
const cancelledPendingStorageKey = "ink.cancelled-grade-pending.v1";

export function Gradebook({ items, courses, students, onEdit, onDeactivate }: { items: GradeItem[]; courses: CourseItem[]; students: StudentItem[]; onEdit: (item: GradeItem) => void; onDeactivate: (item: GradeItem) => void }) {
  const { settings } = useInstituteSettings();
  const [cancelledPending, setCancelledPending] = useState<Set<string>>(new Set());
  useEffect(() => {
    const timer = window.setTimeout(() => {
      try { setCancelledPending(new Set(JSON.parse(window.localStorage.getItem(cancelledPendingStorageKey) ?? "[]") as string[])); }
      catch { setCancelledPending(new Set()); }
    }, 0);
    return () => window.clearTimeout(timer);
  }, []);
  const groups = Array.from(items.reduce((result, item) => {
    const student = students.find(candidate => candidate.id === item.values.studentId);
    const group = result.get(item.values.studentId) ?? { studentId: item.values.studentId, student: item.values.student, studentCode: student?.values.studentCode ?? "Unknown", departmentId: item.values.departmentId, department: item.values.department, records: [] };
    group.records.push(item);
    result.set(item.values.studentId, group);
    return result;
  }, new Map<string, GradeGroup>()).values());

  function cancelPending(key: string) {
    setCancelledPending(current => {
      const next = new Set(current);
      next.add(key);
      window.localStorage.setItem(cancelledPendingStorageKey, JSON.stringify([...next]));
      return next;
    });
  }

  return <section className="student-record-ledger grade-student-ledger">{groups.map(group => {
    const records = group.records.toSorted((a, b) => a.values.course.localeCompare(b.values.course));
    const departmentCourses = courses.filter(course => course.values.departmentId === group.departmentId && course.values.status !== "Inactive");
    const gradedCourseIds = new Set(records.map(record => record.values.courseId));
    const total = records.reduce((sum, record) => sum + Number(record.values.score || 0), 0);
    const average = records.length ? total / records.length : 0;
    const grade = configuredGrade(average, settings["grade-rules"]);
    return <article className="panel student-record-row" key={group.studentId}><header>
      <ManagementDataCell label="StudentID" className="record-business-id"><strong>{group.studentCode}</strong></ManagementDataCell>
      <ManagementDataCell label="Photo" className="record-photo-cell"><span className="initial-chip">{initials(group.student)}</span></ManagementDataCell>
      <ManagementDataCell label="Student Name" className="student-record-identity"><strong>{group.student}</strong><small>{group.department} - {records[0]?.values.academicYear} - {records[0]?.values.term}</small></ManagementDataCell>
      <ManagementDataCell label="Grade summary" className="grade-totals"><span><small>Total</small><b>{total.toFixed(1)}</b></span><span><small>Average</small><b>{average.toFixed(1)}%</b></span><span><small>Grade</small><b className={`grade-letter grade-${grade.toLowerCase()}`}>{grade}</b></span></ManagementDataCell>
    </header><div className="student-record-cells">{records.map(item => <div className="grade-record-cell" key={item.id}><div className="record-field-grid grade-field-grid">
      <ManagementDataCell label="GradeID"><strong>{item.values.gradeCode}</strong></ManagementDataCell>
      <ManagementDataCell label="Course"><strong>{item.values.course}</strong></ManagementDataCell>
      <ManagementDataCell label="Score"><strong>{Number(item.values.score).toFixed(1)}</strong></ManagementDataCell>
      <ManagementDataCell label="Grade"><strong>{item.values.grade}</strong></ManagementDataCell>
      <ManagementDataCell label="Academic year"><strong>{item.values.academicYear}</strong></ManagementDataCell>
      <ManagementDataCell label="Term"><strong>{item.values.term}</strong></ManagementDataCell>
      <ManagementDataCell label="Create At"><strong>{item.values.createAt}</strong></ManagementDataCell>
    </div><ManagementDataCell label="Actions" className="management-action-cell"><ManagementActions item={item} onEdit={onEdit} onDeactivate={onDeactivate}/></ManagementDataCell></div>)}{departmentCourses.filter(course => {
      const pendingKey = `${group.studentId}:${course.id}:${records[0]?.values.academicYear}:${records[0]?.values.term}`;
      return !gradedCourseIds.has(course.id) && !cancelledPending.has(pendingKey);
    }).map(course => {
      const pendingKey = `${group.studentId}:${course.id}:${records[0]?.values.academicYear}:${records[0]?.values.term}`;
      return <div className="grade-record-cell pending-grade" key={course.id}><ManagementDataCell label="Course"><strong>{course.values.name}</strong><small>Grade pending</small></ManagementDataCell><ManagementDataCell label="Actions" className="management-action-cell"><button type="button" className="cancel-pending-grade" onClick={() => cancelPending(pendingKey)}>Cancel</button></ManagementDataCell></div>;
    })}</div></article>;
  })}</section>;
}
