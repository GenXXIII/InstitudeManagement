import Ionicons from '@expo/vector-icons/Ionicons';
import { useEffect, useMemo, useRef, useState } from 'react';
import { Alert, Pressable, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
import { Card, EmptyBlock, Identity, MetricCard, PortalPage, portalStyles, SectionHeading, StatusPill } from '@/components/portal-ui';
import { palette, radius } from '@/constants/theme';
import type { MobileRole } from '@/features/auth/auth-context';
import {
  blankAssessmentDraft,
  clearAssessmentDraft,
  readAssessmentDraft,
  writeAssessmentDraft,
  type AssessmentDraft,
} from '../assessment-draft-storage';
import type { AttendanceItem, GradeItem, StudentItem } from '../portal-types';
import { usePortal } from '../portal-context';

type AssessmentComponent = keyof AssessmentDraft;
type DraftMap = Record<string, AssessmentDraft>;

export function ResultsScreen({ role }: { role: MobileRole }) {
  return role === 'teacher' ? <TeacherAssessment/> : <StudentResults/>;
}

function TeacherAssessment() {
  const portal = usePortal();
  const courses = useMemo(() => [...new Map(portal.schedule.filter(item => item.values.courseId).map(item => [item.values.courseId, item])).values()], [portal.schedule]);
  const [selectedCourseId, setSelectedCourseId] = useState('');
  const [drafts, setDrafts] = useState<DraftMap>({});
  const draftsRef = useRef<DraftMap>({});
  const writeQueues = useRef<Record<string, Promise<void>>>({});
  const [draftsLoaded, setDraftsLoaded] = useState(false);
  const [draftError, setDraftError] = useState('');
  const [savingId, setSavingId] = useState('');
  const [savingAll, setSavingAll] = useState(false);
  const [attendanceStudentId, setAttendanceStudentId] = useState('');
  const selected = courses.find(item => item.values.courseId === selectedCourseId) ?? courses[0];
  const roster = useMemo(() => selected ? portal.students.filter(student => (!selected.values.departmentId || selected.values.departmentId === student.values.departmentId) && selected.values.yearLevel === student.values.year) : [], [portal.students, selected]);
  const attendanceStudent = portal.students.find(student => student.id === attendanceStudentId);
  const teacherId = portal.profile?.id ?? 'teacher';
  const courseId = selected?.values.courseId ?? '';

  function replaceDrafts(next: DraftMap) {
    draftsRef.current = next;
    setDrafts(next);
  }

  useEffect(() => {
    let active = true;
    if (!courseId || !roster.length) {
      Promise.resolve().then(() => { if (active) setDraftsLoaded(true); });
      return () => { active = false; };
    }
    Promise.all(roster.map(async student => [student.id, await readAssessmentDraft(teacherId, courseId, student.id)] as const))
      .then(entries => {
        if (!active) return;
        const restored = Object.fromEntries(entries.filter((entry): entry is readonly [string, AssessmentDraft] => entry[1] !== null));
        draftsRef.current = restored;
        setDrafts(restored);
      })
      .catch(() => { if (active) setDraftError('Drafts could not be restored on this device.'); })
      .finally(() => { if (active) setDraftsLoaded(true); });
    return () => { active = false; };
  }, [courseId, roster, teacherId]);

  function selectCourse(nextCourseId: string) {
    if (nextCourseId === courseId) return;
    replaceDrafts({});
    setDraftsLoaded(false);
    setDraftError('');
    setSelectedCourseId(nextCourseId);
  }

  function queueDraftOperation(studentId: string, operation: () => Promise<void>) {
    const queueKey = `${courseId}:${studentId}`;
    const queued = (writeQueues.current[queueKey] ?? Promise.resolve()).then(operation);
    const handled = queued.catch(() => setDraftError('A score draft could not be saved on this device.'));
    writeQueues.current[queueKey] = handled;
    return handled;
  }

  function changeScore(studentId: string, component: AssessmentComponent, value: string) {
    if (!courseId) return;
    const draft = { ...(draftsRef.current[studentId] ?? blankAssessmentDraft()), [component]: numericInput(value) };
    replaceDrafts({ ...draftsRef.current, [studentId]: draft });
    setDraftError('');
    void queueDraftOperation(studentId, () => writeAssessmentDraft(teacherId, courseId, studentId, draft));
  }

  async function clearStudentDraft(studentId: string) {
    await queueDraftOperation(studentId, () => clearAssessmentDraft(teacherId, courseId, studentId));
    const next = { ...draftsRef.current };
    delete next[studentId];
    replaceDrafts(next);
  }

  function valuesFor(student: StudentItem) {
    const existing = gradeFor(portal.grades, student.id, courseId);
    const draft = drafts[student.id];
    return {
      assignment: draft?.assignment || existing?.values.assignmentScore || '',
      midterm: draft?.midterm || existing?.values.midtermScore || '',
      finalExam: draft?.finalExam || existing?.values.finalExamScore || '',
    };
  }

  function payloadFor(student: StudentItem) {
    const values = valuesFor(student);
    return {
      studentId: student.id,
      courseId,
      scores: {
        assignmentScore: Number(values.assignment),
        midtermScore: Number(values.midterm),
        finalExamScore: Number(values.finalExam),
      },
      complete: complete(values.assignment, portal.gradeWeights.assignment) && complete(values.midterm, portal.gradeWeights.midterm) && complete(values.finalExam, portal.gradeWeights.finalExam),
    };
  }

  async function saveStudent(student: StudentItem) {
    const payload = payloadFor(student);
    if (!payload.complete) {
      Alert.alert('Complete this assessment', 'Enter Assignment, Midterm, and Final Term scores within the maximums shown.');
      return;
    }
    setSavingId(student.id);
    try {
      await portal.submitGrade(payload.studentId, payload.courseId, payload.scores);
      await clearStudentDraft(student.id);
    } catch (reason) {
      Alert.alert('Assessment not submitted', reason instanceof Error ? reason.message : 'Try again. Your draft is still saved.');
    } finally {
      setSavingId('');
    }
  }

  async function saveAll() {
    const payloads = roster.map(payloadFor);
    if (!payloads.length || payloads.some(item => !item.complete)) {
      Alert.alert('Complete the roster', 'Every student needs Assignment, Midterm, and Final Term scores before Submit all is available. Your current work remains saved as drafts.');
      return;
    }
    setSavingAll(true);
    try {
      await portal.submitGrades(payloads.map(({ studentId, courseId: submissionCourseId, scores }) => ({ studentId, courseId: submissionCourseId, scores })));
      await Promise.all(roster.map(student => clearStudentDraft(student.id)));
      Alert.alert('Assessment submitted', `Scores for ${roster.length} students were submitted successfully.`);
    } catch (reason) {
      Alert.alert('Roster not fully submitted', reason instanceof Error ? reason.message : 'Try again. Unsent work remains saved as drafts.');
    } finally {
      setSavingAll(false);
    }
  }

  const readyCount = roster.filter(student => payloadFor(student).complete).length;
  const draftCount = Object.values(drafts).filter(draft => Object.values(draft).some(Boolean)).length;
  const allReady = roster.length > 0 && readyCount === roster.length && draftsLoaded;

  if (attendanceStudent) {
    return <StudentAttendanceDetail
      student={attendanceStudent}
      records={portal.attendance.filter(item => item.values.studentId === attendanceStudent.id)}
      onBack={() => setAttendanceStudentId('')}
    />;
  }

  return <PortalPage title="Assessment" subtitle="Attendance is calculated automatically. Complete Assignment, Midterm, and Final Term scores for one student or the full class.">
    {courses.length ? <>
      <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.courseTabs}>{courses.map(course => <Pressable key={course.values.courseId} onPress={() => selectCourse(course.values.courseId)} style={[styles.courseTab, selected?.values.courseId === course.values.courseId && styles.courseTabActive]}><Text style={[styles.courseTabCode, selected?.values.courseId === course.values.courseId && styles.courseTabTextActive]}>{course.values.courseCode}</Text><Text numberOfLines={1} style={[styles.courseTabName, selected?.values.courseId === course.values.courseId && styles.courseTabTextActive]}>{course.values.course}</Text></Pressable>)}</ScrollView>
      <Card style={styles.draftCard}>
        <View style={styles.draftHeader}><View style={styles.draftIcon}><Ionicons name="cloud-done-outline" size={19} color={palette.blue}/></View><View style={styles.draftCopy}><Text style={styles.draftTitle}>{draftsLoaded ? `${draftCount} local ${draftCount === 1 ? 'draft' : 'drafts'} saved` : 'Restoring saved drafts…'}</Text><Text style={styles.draftDetail}>Typed scores stay on this device until they are submitted successfully.</Text></View></View>
        <View style={styles.progressTrack}><View style={[styles.progressValue, { width: `${roster.length ? readyCount / roster.length * 100 : 0}%` }]}/></View>
        <View style={styles.progressCopy}><Text>{readyCount} of {roster.length} students complete</Text><Text>{draftError || 'Autosave on'}</Text></View>
        <Pressable onPress={() => void saveAll()} disabled={!allReady || savingAll} style={[styles.submitAll, (!allReady || savingAll) && styles.buttonDisabled]}><Ionicons name="checkmark-done-outline" size={18} color="white"/><Text style={styles.submitAllText}>{savingAll ? 'Submitting class…' : 'Submit all students'}</Text></Pressable>
      </Card>
      <SectionHeading title={selected?.values.course ?? 'Course'} detail={`${roster.length} students`}/>
      <View style={portalStyles.stack}>{roster.map(student => {
        const existing = gradeFor(portal.grades, student.id, courseId);
        const values = valuesFor(student);
        const ready = payloadFor(student).complete;
        const hasDraft = Object.values(drafts[student.id] ?? {}).some(Boolean);
        return <Card key={student.id}>
          <Identity photo={student.values.photoDataUrl} name={student.values.name} detail={`Public ID ${student.values.publicId || 'not assigned'} · ${existing ? `Total ${existing.values.score}/100 (${existing.values.grade})` : 'Not submitted'}`} trailing={existing ? <StatusPill value={existing.values.grade}/> : undefined}/>
          <Pressable accessibilityRole="button" accessibilityLabel={`View attendance for ${student.values.name}`} accessibilityHint="Opens this student's attendance records" onPress={() => setAttendanceStudentId(student.id)} style={({ pressed }) => [styles.attendanceEvidence, pressed && styles.attendanceEvidencePressed]}><View><Text style={styles.evidenceLabel}>Attendance · automatic</Text><Text style={styles.evidenceDetail}>{existing?.values.attendancePresent ?? '0'}/{existing?.values.attendanceSessions ?? '0'} held sessions present</Text></View><View style={styles.evidenceAction}><Text style={styles.evidenceScore}>{existing?.values.attendanceScore ?? '0'}/{portal.gradeWeights.attendance}</Text><Ionicons name="chevron-forward" size={17} color={palette.blue}/></View></Pressable>
          <View style={styles.componentInputs}>
            <ComponentInput label="Assignment" maximum={portal.gradeWeights.assignment} value={values.assignment} onChange={value => changeScore(student.id, 'assignment', value)}/>
            <ComponentInput label="Midterm" maximum={portal.gradeWeights.midterm} value={values.midterm} onChange={value => changeScore(student.id, 'midterm', value)}/>
            <ComponentInput label="Final Term" maximum={portal.gradeWeights.finalExam} value={values.finalExam} onChange={value => changeScore(student.id, 'finalExam', value)}/>
          </View>
          <View style={styles.studentActions}><View style={styles.savedState}><Ionicons name={hasDraft ? 'cloud-done-outline' : ready ? 'checkmark-circle-outline' : 'ellipse-outline'} size={14} color={hasDraft ? palette.blue : ready ? palette.green : palette.muted}/><Text>{hasDraft ? 'Draft saved' : ready ? 'Ready' : 'Scores incomplete'}</Text></View><Pressable onPress={() => void saveStudent(student)} disabled={!ready || savingId === student.id || savingAll} style={[styles.saveButton, (!ready || savingId === student.id || savingAll) && styles.buttonDisabled]}><Text style={styles.saveText}>{savingId === student.id ? 'Submitting…' : 'Submit student'}</Text></Pressable></View>
        </Card>;
      })}</View>
    </> : <EmptyBlock icon="book-outline" title="No assigned course" detail="Administrator must connect this Teacher to a course in Timetable Enrollment before assessments can be submitted."/>}
  </PortalPage>;
}

function StudentAttendanceDetail({ student, records, onBack }: { student: StudentItem; records: AttendanceItem[]; onBack: () => void }) {
  const ordered = [...records].sort((a, b) => b.values.date.localeCompare(a.values.date));
  const count = (status: string) => ordered.filter(item => item.values.status === status).length;

  return <PortalPage title="Student attendance" subtitle="Read-only attendance records for this student.">
    <Pressable accessibilityRole="button" accessibilityLabel="Back to assessment" onPress={onBack} style={({ pressed }) => [styles.attendanceBack, pressed && styles.pressed]}><Ionicons name="chevron-back" size={18} color={palette.blue}/><Text style={styles.attendanceBackText}>Assessment</Text></Pressable>
    <Card><Identity photo={student.values.photoDataUrl} name={student.values.name} detail={`Public ID ${student.values.publicId || 'not assigned'} · Year ${student.values.year || '—'}`}/></Card>
    <View style={portalStyles.grid}><MetricCard icon="checkmark-circle-outline" label="Present" value={count('Present')} tone="green"/><MetricCard icon="time-outline" label="Late" value={count('Late')} tone="amber"/><MetricCard icon="close-circle-outline" label="Absent" value={count('Absent')} tone="violet"/><MetricCard icon="document-text-outline" label="Total records" value={ordered.length}/></View>
    <SectionHeading title="Attendance history" detail={`${ordered.length} records`}/>
    <View style={portalStyles.stack}>{ordered.length ? ordered.map(item => <Card key={item.id}><View style={styles.attendanceRecordTop}><Text style={styles.attendanceRecordDate}>{formatAttendanceDate(item.values.date)}</Text><StatusPill value={item.values.status}/></View><Text style={styles.attendanceRecordMeta}>Check-in {item.values.checkedInAt || 'not recorded'} · {item.values.method || 'Institute record'}</Text><Text style={styles.attendanceRecordPeriod}>{item.values.academicYear} · {item.values.term}</Text></Card>) : <EmptyBlock icon="document-outline" title="No attendance records" detail="Recorded attendance for this student will appear here."/>}</View>
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
    <View style={portalStyles.stack}>{ordered.length ? ordered.map(item => <Card key={item.id}><View style={styles.resultRow}><View style={styles.gradeBadge}><Text>{item.values.grade}</Text></View><View style={styles.resultCopy}><Text style={styles.resultCourse}>{item.values.course}</Text><Text style={styles.resultCode}>{item.values.gradeCode} · {item.values.academicYear}</Text></View><Text style={styles.resultScore}>{item.values.score}/100</Text></View><View style={styles.resultComponents}><ResultComponent label="Attendance" score={item.values.attendanceScore} maximum={item.values.attendanceMaximum} detail={`${item.values.attendancePresent}/${item.values.attendanceSessions} sessions present`}/><ResultComponent label="Assignment" score={item.values.assignmentScore} maximum={item.values.assignmentMaximum}/><ResultComponent label="Midterm" score={item.values.midtermScore} maximum={item.values.midtermMaximum}/><ResultComponent label="Final Term" score={item.values.finalExamScore} maximum={item.values.finalExamMaximum}/></View></Card>) : <EmptyBlock icon="ribbon-outline" title="No results yet" detail="Scores submitted by your Teacher will appear here."/>}</View>
  </PortalPage>;
}

function ComponentInput({ label, maximum, value, onChange }: { label: string; maximum: number; value: string; onChange: (value: string) => void }) {
  return <View style={styles.componentInput}><Text style={styles.componentLabel}>{label}</Text><View style={styles.componentInputRow}><TextInput value={value} onChangeText={onChange} keyboardType="decimal-pad" placeholder="0" placeholderTextColor="#94A2B5" style={styles.scoreInput}/><Text style={styles.maximum}>/ {maximum}</Text></View></View>;
}

function ResultComponent({ label, score, maximum, detail }: { label: string; score: string; maximum: string; detail?: string }) {
  return <View style={styles.resultComponent}><Text style={styles.resultComponentLabel}>{label}</Text><Text style={styles.resultComponentScore}>{score}/{maximum}</Text>{detail ? <Text style={styles.resultComponentDetail}>{detail}</Text> : null}</View>;
}

function gradeFor(grades: GradeItem[], studentId: string, courseId: string) {
  return grades.find(item => item.values.studentId === studentId && item.values.courseId === courseId);
}

function formatAttendanceDate(value: string) {
  const date = new Date(`${value}T00:00:00`);
  return Number.isNaN(date.valueOf()) ? value : new Intl.DateTimeFormat('en-GB', { weekday: 'short', day: '2-digit', month: 'short', year: 'numeric' }).format(date);
}

function numericInput(value: string) {
  const cleaned = value.replace(/[^0-9.]/g, '');
  const [whole = '', ...decimal] = cleaned.split('.');
  return decimal.length ? `${whole}.${decimal.join('').slice(0, 2)}` : whole.slice(0, 5);
}

function complete(value: string, maximum: number) {
  const number = Number(value);
  return value.trim() !== '' && Number.isFinite(number) && number >= 0 && number <= maximum;
}

const styles = StyleSheet.create({
  courseTabs: { gap: 9, paddingRight: 10 },
  courseTab: { width: 160, padding: 14, borderRadius: radius.medium, borderWidth: 1, borderBottomWidth: 3, borderColor: palette.line, backgroundColor: 'white' },
  courseTabActive: { borderColor: palette.blue, backgroundColor: palette.bluePale },
  courseTabCode: { color: palette.blue, fontSize: 10, fontWeight: '800' },
  courseTabName: { color: palette.ink, fontSize: 14, fontWeight: '800', marginTop: 5 },
  courseTabTextActive: { color: palette.blueDark },
  draftCard: { gap: 11, borderLeftWidth: 3, borderLeftColor: palette.blue, backgroundColor: '#FFFFFF' },
  draftHeader: { flexDirection: 'row', alignItems: 'center', gap: 10 },
  draftIcon: { width: 28, height: 32, alignItems: 'flex-start', justifyContent: 'center' },
  draftCopy: { flex: 1 },
  draftTitle: { color: palette.ink, fontSize: 14, fontWeight: '800' },
  draftDetail: { color: palette.muted, fontSize: 12, lineHeight: 18, marginTop: 4 },
  progressTrack: { height: 4, overflow: 'hidden', backgroundColor: '#E6E9EE' },
  progressValue: { height: '100%', backgroundColor: palette.blue },
  progressCopy: { flexDirection: 'row', justifyContent: 'space-between', gap: 8 },
  submitAll: { minHeight: 44, flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 8, borderRadius: radius.small, backgroundColor: palette.blue },
  submitAllText: { color: 'white', fontSize: 13, fontWeight: '800' },
  attendanceEvidence: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: 8, marginTop: 12, paddingVertical: 10, paddingHorizontal: 11, borderRadius: radius.small, borderWidth: 1, borderColor: palette.line, backgroundColor: '#F8F9FB' },
  attendanceEvidencePressed: { borderColor: palette.blue, backgroundColor: palette.bluePale },
  evidenceLabel: { color: palette.ink, fontSize: 12, fontWeight: '800' },
  evidenceDetail: { color: palette.muted, fontSize: 10, marginTop: 4 },
  evidenceAction: { flexDirection: 'row', alignItems: 'center', gap: 5 },
  evidenceScore: { color: palette.blueDark, fontSize: 15, fontWeight: '800' },
  componentInputs: { gap: 9, marginTop: 11 },
  componentInput: { gap: 5 },
  componentLabel: { color: palette.ink, fontSize: 12, fontWeight: '800' },
  componentInputRow: { flexDirection: 'row', alignItems: 'center', gap: 8 },
  scoreInput: { flex: 1, height: 42, paddingHorizontal: 12, borderRadius: radius.small, borderWidth: 1, borderColor: '#CCD5E0', color: palette.ink, backgroundColor: '#FFFFFF', fontWeight: '700' },
  maximum: { width: 42, color: palette.muted, fontSize: 12, fontWeight: '700' },
  studentActions: { minHeight: 45, flexDirection: 'row', alignItems: 'flex-end', justifyContent: 'space-between', gap: 10, marginTop: 8 },
  savedState: { flexDirection: 'row', alignItems: 'center', gap: 5 },
  saveButton: { minWidth: 114, height: 40, borderRadius: radius.small, backgroundColor: palette.blue, alignItems: 'center', justifyContent: 'center', paddingHorizontal: 12 },
  saveText: { color: 'white', fontWeight: '800', fontSize: 12 },
  buttonDisabled: { opacity: 0.4 },
  attendanceBack: { alignSelf: 'flex-start', minHeight: 38, flexDirection: 'row', alignItems: 'center', gap: 3, paddingRight: 12 },
  attendanceBackText: { color: palette.blue, fontSize: 12, fontWeight: '700' },
  attendanceRecordTop: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: 10 },
  attendanceRecordDate: { flex: 1, color: palette.ink, fontSize: 14, fontWeight: '800' },
  attendanceRecordMeta: { color: palette.ink, fontSize: 12, marginTop: 11 },
  attendanceRecordPeriod: { color: palette.muted, fontSize: 10, marginTop: 5 },
  pressed: { opacity: 0.68 },
  resultRow: { flexDirection: 'row', alignItems: 'center', gap: 11 },
  gradeBadge: { width: 38, height: 38, borderRadius: radius.small, borderWidth: 1, borderColor: palette.blue, backgroundColor: '#FFFFFF', alignItems: 'center', justifyContent: 'center' },
  resultCopy: { flex: 1 },
  resultCourse: { color: palette.ink, fontWeight: '800', fontSize: 15 },
  resultCode: { color: palette.muted, fontSize: 10, marginTop: 4 },
  resultScore: { color: palette.blueDark, fontSize: 20, fontWeight: '800' },
  resultComponents: { marginTop: 12, borderTopWidth: 1, borderTopColor: palette.line },
  resultComponent: { flexDirection: 'row', flexWrap: 'wrap', justifyContent: 'space-between', gap: 4, paddingVertical: 9, borderBottomWidth: StyleSheet.hairlineWidth, borderBottomColor: palette.line },
  resultComponentLabel: { color: palette.ink, fontSize: 12, fontWeight: '700' },
  resultComponentScore: { color: palette.ink, fontSize: 12, fontWeight: '800' },
  resultComponentDetail: { width: '100%', color: palette.muted, fontSize: 10 },
});
