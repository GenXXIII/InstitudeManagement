"use client";

import { configuredGrade, useInstituteSettings } from "@/features/administration/institute-settings-context";
import { useEffect, useState } from "react";
import type { CourseItem } from "../types/course";
import type { GradeItem } from "../types/grade";
import type { StudentItem } from "../types/student";
import { initials } from "../management-utils";
import { ManagementActions } from "./management-actions";

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
    return <article className="panel student-record-row" key={group.studentId}><header><div className="record-business-id"><span>StudentCode</span><strong>{group.studentCode}</strong></div><span className="initial-chip">{initials(group.student)}</span><div className="student-record-identity"><strong>{group.student}</strong><small>{group.department} - {records[0]?.values.academicYear} - {records[0]?.values.term}</small></div><div className="grade-totals"><span>Total <b>{total.toFixed(1)}</b></span><span>Average <b>{average.toFixed(1)}%</b></span><span>Grade <b className={`grade-letter grade-${grade.toLowerCase()}`}>{grade}</b></span></div></header><div className="student-record-cells">{records.map(item => <div className="grade-record-cell" key={item.id}><div><strong>{item.values.course} {Number(item.values.score).toFixed(1)}/{item.values.grade}</strong><small>Grade {item.values.gradeCode} - {item.values.term} - Create At {item.values.createAt}</small></div><ManagementActions item={item} onEdit={onEdit} onDeactivate={onDeactivate}/></div>)}{departmentCourses.filter(course => {
      const pendingKey = `${group.studentId}:${course.id}:${records[0]?.values.academicYear}:${records[0]?.values.term}`;
      return !gradedCourseIds.has(course.id) && !cancelledPending.has(pendingKey);
    }).map(course => {
      const pendingKey = `${group.studentId}:${course.id}:${records[0]?.values.academicYear}:${records[0]?.values.term}`;
      return <div className="grade-record-cell pending-grade" key={course.id}><div><strong>{course.values.name}</strong><small>Grade pending</small></div><button type="button" className="cancel-pending-grade" onClick={() => cancelPending(pendingKey)}>Cancel</button></div>;
    })}</div></article>;
  })}</section>;
}
