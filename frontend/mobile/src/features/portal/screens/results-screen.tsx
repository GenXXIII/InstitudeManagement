import { useMemo, useState } from 'react';
import { Alert, Pressable, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
import { Card, EmptyBlock, Identity, MetricCard, PortalPage, portalStyles, SectionHeading, StatusPill } from '@/components/portal-ui';
import { palette, radius } from '@/constants/theme';
import type { MobileRole } from '@/features/auth/auth-context';
import { usePortal } from '../portal-context';

export function ResultsScreen({ role }: { role: MobileRole }) {
  return role === 'teacher' ? <TeacherGradebook/> : <StudentResults/>;
}

function TeacherGradebook() {
  const portal = usePortal();
  const courses = useMemo(() => [...new Map(portal.schedule.filter(item => item.values.courseId).map(item => [item.values.courseId, item])).values()], [portal.schedule]);
  const [selectedCourseId, setSelectedCourseId] = useState('');
  const [scores, setScores] = useState<Record<string, string>>({});
  const [savingId, setSavingId] = useState('');
  const selected = courses.find(item => item.values.courseId === selectedCourseId) ?? courses[0];
  const roster = selected ? portal.students.filter(student => (!selected.values.departmentId || selected.values.departmentId === student.values.departmentId) && selected.values.yearLevel === student.values.year) : [];

  async function save(studentId: string) {
    if (!selected) return;
    const existing = portal.grades.find(item => item.values.studentId === studentId && item.values.courseId === selected.values.courseId);
    const values = {
      assignmentScore: Number(componentValue(scores, studentId, 'assignment', existing?.values.assignmentScore)),
      midtermScore: Number(componentValue(scores, studentId, 'midterm', existing?.values.midtermScore)),
      finalExamScore: Number(componentValue(scores, studentId, 'finalExam', existing?.values.finalExamScore)),
    };
    if (!valid(values.assignmentScore, portal.gradeWeights.assignment) || !valid(values.midtermScore, portal.gradeWeights.midterm) || !valid(values.finalExamScore, portal.gradeWeights.finalExam)) { Alert.alert('Invalid components', 'Enter each score between 0 and the configured maximum shown beside it.'); return; }
    setSavingId(studentId);
    try { await portal.submitGrade(studentId, selected.values.courseId, values); setScores(current => clearStudentScores(current, studentId)); }
    catch (reason) { Alert.alert('Grade not saved', reason instanceof Error ? reason.message : 'Try again.'); }
    finally { setSavingId(''); }
  }

  return <PortalPage title="Gradebook" subtitle="Attendance is automatic. Enter assignment, midterm, and final-exam components for your assigned students.">
    {courses.length ? <><ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.courseTabs}>{courses.map(course => <Pressable key={course.values.courseId} onPress={() => setSelectedCourseId(course.values.courseId)} style={[styles.courseTab, selected?.values.courseId === course.values.courseId && styles.courseTabActive]}><Text style={[styles.courseTabCode, selected?.values.courseId === course.values.courseId && styles.courseTabTextActive]}>{course.values.courseCode}</Text><Text numberOfLines={1} style={[styles.courseTabName, selected?.values.courseId === course.values.courseId && styles.courseTabTextActive]}>{course.values.course}</Text></Pressable>)}</ScrollView><SectionHeading title={selected?.values.course ?? 'Course'} detail={`${roster.length} students`}/><View style={portalStyles.stack}>{roster.map(student => {
      const existing = portal.grades.find(item => item.values.studentId === student.id && item.values.courseId === selected?.values.courseId);
      return <Card key={student.id}><Identity photo={student.values.photoDataUrl} name={student.values.name} detail={`${student.values.studentCode} · ${existing ? `Total ${existing.values.score}/100 (${existing.values.grade})` : 'Not graded'}`} trailing={existing ? <StatusPill value={existing.values.grade}/> : undefined}/><View style={styles.attendanceEvidence}><Text>Attendance</Text><Text>{existing?.values.attendanceScore ?? '0'}/{portal.gradeWeights.attendance} · {existing?.values.attendancePresent ?? '0'}/{existing?.values.attendanceSessions ?? '0'} held sessions present</Text></View><View style={styles.componentInputs}><ComponentInput label="Assignment" maximum={portal.gradeWeights.assignment} value={componentValue(scores, student.id, 'assignment', existing?.values.assignmentScore)} onChange={value => setScores(current => ({ ...current, [scoreKey(student.id, 'assignment')]: value }))}/><ComponentInput label="Midterm" maximum={portal.gradeWeights.midterm} value={componentValue(scores, student.id, 'midterm', existing?.values.midtermScore)} onChange={value => setScores(current => ({ ...current, [scoreKey(student.id, 'midterm')]: value }))}/><ComponentInput label="Final exam" maximum={portal.gradeWeights.finalExam} value={componentValue(scores, student.id, 'finalExam', existing?.values.finalExamScore)} onChange={value => setScores(current => ({ ...current, [scoreKey(student.id, 'finalExam')]: value }))}/></View><Pressable onPress={() => void save(student.id)} disabled={savingId === student.id} style={styles.saveButton}><Text style={styles.saveText}>{savingId === student.id ? 'Saving…' : 'Save components'}</Text></Pressable></Card>;
    })}</View></> : <EmptyBlock icon="book-outline" title="No assigned course" detail="Administrator must connect this Teacher to a course in Timetable Enrollment before grades can be submitted."/>}
  </PortalPage>;
}

function StudentResults() {
  const portal = usePortal();
  const ordered = [...portal.grades].sort((a, b) => a.values.course.localeCompare(b.values.course));
  const scores = ordered.map(item => Number(item.values.score)).filter(Number.isFinite);
  const total = scores.reduce((sum, score) => sum + score, 0);
  const average = scores.length ? (total / scores.length).toFixed(2) : '—';
  return <PortalPage title="My results" subtitle="Course scores and grade letters are read-only Student Records.">
    <View style={portalStyles.grid}><MetricCard icon="ribbon-outline" label="Average" value={average} tone="violet"/><MetricCard icon="calculator-outline" label="Total score" value={scores.length ? total.toFixed(1) : '—'} tone="green"/><MetricCard icon="book-outline" label="Courses graded" value={ordered.length}/><MetricCard icon="school-outline" label="Semester" value={ordered[0]?.values.term || '—'} tone="amber"/></View>
    <SectionHeading title="Course results" detail={`${ordered.length} grades`}/>
    <View style={portalStyles.stack}>{ordered.length ? ordered.map(item => <Card key={item.id}><View style={styles.resultRow}><View style={styles.gradeBadge}><Text>{item.values.grade}</Text></View><View style={styles.resultCopy}><Text style={styles.resultCourse}>{item.values.course}</Text><Text style={styles.resultCode}>{item.values.gradeCode} · {item.values.academicYear}</Text></View><Text style={styles.resultScore}>{item.values.score}/100</Text></View><View style={styles.resultComponents}><ResultComponent label="Attendance" score={item.values.attendanceScore} maximum={item.values.attendanceMaximum} detail={`${item.values.attendancePresent}/${item.values.attendanceSessions} sessions present`}/><ResultComponent label="Assignment" score={item.values.assignmentScore} maximum={item.values.assignmentMaximum}/><ResultComponent label="Midterm" score={item.values.midtermScore} maximum={item.values.midtermMaximum}/><ResultComponent label="Final exam" score={item.values.finalExamScore} maximum={item.values.finalExamMaximum}/></View></Card>) : <EmptyBlock icon="ribbon-outline" title="No results yet" detail="Scores submitted by your Teacher will appear here."/>}</View>
  </PortalPage>;
}

function ComponentInput({ label, maximum, value, onChange }: { label: string; maximum: number; value: string; onChange: (value: string) => void }) {
  return <View style={styles.componentInput}><Text>{label}</Text><View style={styles.componentInputRow}><TextInput value={value} onChangeText={onChange} keyboardType="decimal-pad" placeholder="0" placeholderTextColor="#94A2B5" style={styles.scoreInput}/><Text>/ {maximum}</Text></View></View>;
}

function ResultComponent({ label, score, maximum, detail }: { label: string; score: string; maximum: string; detail?: string }) {
  return <View style={styles.resultComponent}><Text>{label}</Text><Text>{score}/{maximum}</Text>{detail && <Text>{detail}</Text>}</View>;
}

function scoreKey(studentId: string, component: string) { return `${studentId}:${component}`; }
function componentValue(scores: Record<string, string>, studentId: string, component: string, existing?: string) { return scores[scoreKey(studentId, component)] ?? existing ?? ''; }
function valid(value: number, maximum: number) { return Number.isFinite(value) && value >= 0 && value <= maximum; }
function clearStudentScores(scores: Record<string, string>, studentId: string) { return Object.fromEntries(Object.entries(scores).filter(([key]) => !key.startsWith(`${studentId}:`))); }

const styles = StyleSheet.create({
  courseTabs: { gap: 8, paddingRight: 8 },
  courseTab: { width: 140, padding: 12, borderRadius: radius.medium, borderWidth: 1, borderColor: palette.line, backgroundColor: 'white' },
  courseTabActive: { backgroundColor: palette.blue, borderColor: palette.blue },
  courseTabCode: { color: palette.blue, fontSize: 9, fontWeight: '900' },
  courseTabName: { color: palette.ink, fontSize: 12, fontWeight: '800', marginTop: 4 },
  courseTabTextActive: { color: 'white' },
  attendanceEvidence: { flexDirection: 'row', justifyContent: 'space-between', gap: 8, marginTop: 12, padding: 10, borderRadius: radius.small, backgroundColor: palette.bluePale },
  componentInputs: { gap: 8, marginTop: 10 },
  componentInput: { gap: 4 },
  componentInputRow: { flexDirection: 'row', alignItems: 'center', gap: 8 },
  scoreInput: { flex: 1, height: 40, paddingHorizontal: 12, borderRadius: radius.small, borderWidth: 1, borderColor: palette.line, color: palette.ink, backgroundColor: '#F8FBFF' },
  saveButton: { minWidth: 98, height: 40, marginTop: 10, borderRadius: radius.small, backgroundColor: palette.blue, alignItems: 'center', justifyContent: 'center', paddingHorizontal: 12 },
  saveText: { color: 'white', fontWeight: '900', fontSize: 10 },
  resultRow: { flexDirection: 'row', alignItems: 'center', gap: 11 },
  gradeBadge: { width: 40, height: 40, borderRadius: 13, backgroundColor: palette.bluePale, alignItems: 'center', justifyContent: 'center' },
  resultCopy: { flex: 1 },
  resultCourse: { color: palette.ink, fontWeight: '900', fontSize: 13 },
  resultCode: { color: palette.muted, fontSize: 9, marginTop: 3 },
  resultScore: { color: palette.blueDark, fontSize: 21, fontWeight: '900' },
  resultComponents: { gap: 6, marginTop: 12 },
  resultComponent: { flexDirection: 'row', flexWrap: 'wrap', justifyContent: 'space-between', gap: 4, padding: 8, borderRadius: radius.small, backgroundColor: '#F7FAFE' },
});
