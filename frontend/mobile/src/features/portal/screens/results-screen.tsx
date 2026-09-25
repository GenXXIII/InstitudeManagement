import Ionicons from '@expo/vector-icons/Ionicons';
import { useEffect, useMemo, useRef, useState } from 'react';
import { Alert, Pressable, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
import { Card, EmptyBlock, Identity, PortalPage, portalStyles, SectionHeading, StatusPill } from '@/components/portal-ui';
import { palette, radius } from '@/constants/theme';
import type { MobileRole } from '@/features/auth/auth-context';
import { blankAssessmentDraft, clearAssessmentDraft, readAssessmentDraft, writeAssessmentDraft, type AssessmentDraft } from '../assessment-draft-storage';
import type { GradeItem, ScheduleItem, StudentItem } from '../portal-types';
import { usePortal } from '../portal-context';

type AssessmentComponent = keyof AssessmentDraft;
type DraftMap = Record<string, AssessmentDraft>;

export function ResultsScreen({ role }: { role: MobileRole }) {
  return role === 'teacher' ? <TeacherAssessment/> : <PublishedStudentResults/>;
}

function TeacherAssessment() {
  const portal = usePortal();
  const courses = useMemo(() => uniqueCourseAssignments(portal.schedule), [portal.schedule]);
  const [selectedKey, setSelectedKey] = useState('');
  const [drafts, setDrafts] = useState<DraftMap>({});
  const draftsRef = useRef<DraftMap>({});
  const writeQueues = useRef<Record<string, Promise<void>>>({});
  const [draftsLoaded, setDraftsLoaded] = useState(false);
  const [draftError, setDraftError] = useState('');
  const [working, setWorking] = useState(false);
  const selected = courses.find(item => assignmentKey(item) === selectedKey) ?? courses[0];
  const courseId = selected?.values.courseId ?? '';
  const teacherId = portal.profile?.id ?? 'teacher';
  const roster = useMemo(() => selected ? portal.students.filter(student => sameCohort(selected, student)) : [], [portal.students, selected]);
  const rosterIds = useMemo(() => new Set(roster.map(student => student.id)), [roster]);
  const courseGrades = useMemo(() => portal.grades.filter(item => item.values.courseId === courseId && rosterIds.has(item.values.studentId) && item.values.submittedByTeacherId === portal.profile?.id), [courseId, portal.grades, portal.profile?.id, rosterIds]);
  const anchor = courseGrades[0];
  const workflowStatus = courseWorkflowStatus(courseGrades);
  const editable = workflowStatus === 'Draft' || workflowStatus === 'Rejected' || workflowStatus === 'ResubmitAuthorized';

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

  function selectCourse(item: ScheduleItem) {
    const nextKey = assignmentKey(item);
    if (nextKey === assignmentKey(selected)) return;
    replaceDrafts({});
    setDraftsLoaded(false);
    setDraftError('');
    setSelectedKey(nextKey);
  }

  function queueDraftOperation(studentId: string, operation: () => Promise<void>) {
    const queueKey = `${courseId}:${studentId}`;
    const queued = (writeQueues.current[queueKey] ?? Promise.resolve()).then(operation);
    const handled = queued.catch(() => setDraftError('A score draft could not be saved on this device.'));
    writeQueues.current[queueKey] = handled;
    return handled;
  }

  function changeScore(studentId: string, component: AssessmentComponent, value: string) {
    if (!courseId || !editable) return;
    const draft = { ...(draftsRef.current[studentId] ?? blankAssessmentDraft()), [component]: numericInput(value) };
    replaceDrafts({ ...draftsRef.current, [studentId]: draft });
    setDraftError('');
    void queueDraftOperation(studentId, () => writeAssessmentDraft(teacherId, courseId, studentId, draft));
  }

  function valuesFor(student: StudentItem) {
    const existing = gradeFor(courseGrades, student.id);
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
      assignmentScore: Number(values.assignment),
      midtermScore: Number(values.midterm),
      finalExamScore: Number(values.finalExam),
      complete: complete(values.assignment, portal.gradeWeights.assignment) && complete(values.midterm, portal.gradeWeights.midterm) && complete(values.finalExam, portal.gradeWeights.finalExam),
    };
  }

  const payloads = roster.map(payloadFor);
  const allReady = roster.length > 0 && payloads.every(item => item.complete) && draftsLoaded;
  const draftCount = Object.values(drafts).filter(draft => Object.values(draft).some(Boolean)).length;

  async function clearCourseDrafts() {
    await Promise.all(roster.map(student => queueDraftOperation(student.id, () => clearAssessmentDraft(teacherId, courseId, student.id))));
    replaceDrafts({});
  }

  async function courseAction() {
    if (!selected || !courseId) return;
    if ((workflowStatus === 'Draft' || workflowStatus === 'Rejected' || workflowStatus === 'ResubmitAuthorized') && !allReady) {
      Alert.alert('Complete the course roster', 'Every Student needs Assignment, Midterm, and Final Term scores before the whole course can be sent.');
      return;
    }
    setWorking(true);
    try {
      const scores = payloads.map(({ complete: _complete, ...item }) => item);
      if (workflowStatus === 'Draft' || workflowStatus === 'Rejected') {
        await portal.requestCourseSubmission(courseId, scores);
        await clearCourseDrafts();
        Alert.alert('Course approval requested', `One submission approval request was sent for all ${roster.length} Students in ${selected.values.course}.`);
      } else if (workflowStatus === 'SubmissionAuthorized' && anchor) {
        await portal.submitAuthorizedCourse(anchor.id);
        Alert.alert('Course results submitted', `The complete ${selected.values.course} roster is waiting for Administrator acceptance.`);
      } else if (workflowStatus === 'ResubmitAuthorized' && anchor) {
        await portal.submitAuthorizedCourse(anchor.id, scores);
        await clearCourseDrafts();
        Alert.alert('Course results resubmitted', `Corrected results for all ${roster.length} Students were resubmitted together.`);
      } else if (workflowStatus === 'Approved' && anchor) {
        await portal.requestCourseResubmission(anchor.id);
        Alert.alert('Resubmission approval requested', `One resubmission request was sent for the complete ${selected.values.course} roster.`);
      }
    } catch (reason) {
      Alert.alert('Course workflow not updated', reason instanceof Error ? reason.message : 'Try again. Local drafts remain saved.');
    } finally {
      setWorking(false);
    }
  }

  return <PortalPage title="Assessment" subtitle="Enter individual scores, then request and submit the complete assigned course roster as one institutional workflow.">
    {courses.length ? <>
      <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.courseTabs}>{courses.map(course => <Pressable key={assignmentKey(course)} onPress={() => selectCourse(course)} style={[styles.courseTab, assignmentKey(selected) === assignmentKey(course) && styles.courseTabActive]}><Text style={[styles.courseTabCode, assignmentKey(selected) === assignmentKey(course) && styles.courseTabTextActive]}>{course.values.courseCode}</Text><Text numberOfLines={1} style={[styles.courseTabName, assignmentKey(selected) === assignmentKey(course) && styles.courseTabTextActive]}>{course.values.course}</Text><Text style={styles.courseTabCohort}>Year {course.values.yearLevel} · {course.values.shift}</Text></Pressable>)}</ScrollView>
      <Card style={styles.workflowCard}>
        <View style={styles.workflowHeader}><View style={styles.workflowIcon}><Ionicons name="people-outline" size={20} color={palette.blue}/></View><View style={styles.workflowCopy}><Text style={styles.workflowTitle}>Whole-course submission</Text><Text style={styles.workflowDetail}>{workflowMessage(workflowStatus, roster.length)}</Text></View><StatusPill value={workflowDisplay(workflowStatus)}/></View>
        <AssessmentWorkflowSteps status={workflowStatus}/>
        <View style={styles.progressTrack}><View style={[styles.progressValue, { width: `${roster.length ? payloads.filter(item => item.complete).length / roster.length * 100 : 0}%` }]}/></View>
        <View style={styles.progressCopy}><Text>{payloads.filter(item => item.complete).length} of {roster.length} Student grades complete</Text><Text>{draftError || `${draftCount} local drafts`}</Text></View>
        {courseActionAvailable(workflowStatus) ? <Pressable onPress={() => void courseAction()} disabled={working || ((workflowStatus === 'Draft' || workflowStatus === 'Rejected' || workflowStatus === 'ResubmitAuthorized') && !allReady)} style={[styles.courseAction, (working || ((workflowStatus === 'Draft' || workflowStatus === 'Rejected' || workflowStatus === 'ResubmitAuthorized') && !allReady)) && styles.buttonDisabled]}><Ionicons name="checkmark-done-outline" size={18} color="white"/><Text style={styles.courseActionText}>{working ? 'Updating course workflow...' : courseActionLabel(workflowStatus)}</Text></Pressable> : null}
        {anchor?.values.reviewNote ? <Text style={styles.reviewNote}>{anchor.values.reviewNote}</Text> : null}
      </Card>
      <SectionHeading title={selected?.values.course ?? 'Course'} detail={`${roster.length} Students · one course roster`}/>
      <View style={portalStyles.stack}>{roster.map(student => {
        const existing = gradeFor(courseGrades, student.id);
        const values = valuesFor(student);
        return <Card key={student.id}>
          <Identity photo={student.values.photoDataUrl} name={student.values.name} detail={`Public ID ${student.values.publicId || 'not assigned'} · ${existing ? `Total ${existing.values.score}/100 (${existing.values.grade})` : 'Not submitted'}`} trailing={existing ? <StatusPill value={existing.values.reviewStatus}/> : undefined}/>
          <View style={styles.componentInputs}><ComponentInput disabled={!editable} label="Assignment" maximum={portal.gradeWeights.assignment} value={values.assignment} onChange={value => changeScore(student.id, 'assignment', value)}/><ComponentInput disabled={!editable} label="Midterm" maximum={portal.gradeWeights.midterm} value={values.midterm} onChange={value => changeScore(student.id, 'midterm', value)}/><ComponentInput disabled={!editable} label="Final Term" maximum={portal.gradeWeights.finalExam} value={values.finalExam} onChange={value => changeScore(student.id, 'finalExam', value)}/></View>
          <View style={styles.studentState}><Ionicons name={editable ? 'cloud-done-outline' : 'lock-closed-outline'} size={14} color={palette.blue}/><Text>{editable ? Object.values(drafts[student.id] ?? {}).some(Boolean) ? 'Student draft saved' : 'Included in whole-course draft' : `Controlled by ${workflowDisplay(workflowStatus)} workflow`}</Text></View>
        </Card>;
      })}</View>
    </> : <EmptyBlock icon="book-outline" title="No assigned course" detail="Administrator must connect this Teacher to a course in Timetable Enrollment before assessments can be submitted."/>}
  </PortalPage>;
}

function PublishedStudentResults() {
  const portal = usePortal();
  const ordered = [...portal.publishedResults].sort((a, b) => b.academicYear.localeCompare(a.academicYear) || b.semester.localeCompare(a.semester));
  return <PortalPage title="My results" subtitle="Only semester results published by Administrator are visible here.">
    <SectionHeading title="Published academic results" detail={`${ordered.length} semesters`}/>
    <View style={portalStyles.stack}>{ordered.length ? ordered.map(result => <Card key={`${result.academicYear}-${result.semester}`}><View style={styles.publishedHeader}><View><Text style={styles.resultCourse}>{result.academicYear} · {result.semester}</Text><Text style={styles.resultCode}>Published {new Date(result.publishedAtUtc).toLocaleDateString()}</Text></View><StatusPill value={result.totalGrade}/></View><View style={styles.publishedSummary}><Text style={styles.summaryPill}>Present {result.presentCount}</Text><Text style={styles.summaryPill}>Permission {result.permissionCount}</Text><Text style={styles.summaryPill}>Absent {result.absentCount}</Text><Text style={styles.summaryPill}>Attendance {result.attendanceGrade}/{result.attendanceScore.toFixed(2)}</Text><Text style={styles.summaryPill}>Total {result.totalScore.toFixed(1)}</Text><Text style={styles.summaryPill}>Average {result.average.toFixed(2)}/{result.overallGrade}</Text></View><View style={styles.resultComponents}>{result.grades.map(grade => <View style={styles.publishedCourse} key={grade.courseId}><View><Text style={styles.resultCourse}>{grade.name}</Text><Text style={styles.resultCode}>{grade.courseCode}</Text></View><Text style={styles.resultScore}>{grade.score.toFixed(1)}/{grade.grade}</Text></View>)}</View></Card>) : <EmptyBlock icon="ribbon-outline" title="No published result yet" detail="Results appear after every course is confirmed and the Administrator publishes all Semester Results."/>}</View>
  </PortalPage>;
}

function ComponentInput({ label, maximum, value, disabled, onChange }: { label: string; maximum: number; value: string; disabled: boolean; onChange: (value: string) => void }) {
  return <View style={styles.componentInput}><Text style={styles.componentLabel}>{label}</Text><View style={styles.componentInputRow}><TextInput editable={!disabled} value={value} onChangeText={onChange} keyboardType="decimal-pad" placeholder="0" placeholderTextColor="#94A2B5" style={[styles.scoreInput, disabled && styles.inputDisabled]}/><Text style={styles.maximum}>/ {maximum}</Text></View></View>;
}

function AssessmentWorkflowSteps({ status }: { status: string }) {
  const current = status === 'Approved' ? 4
    : status === 'Submitted' || status === 'Pending' ? 3
      : status === 'SubmissionAuthorized' || status === 'ResubmitAuthorized' ? 2
        : status === 'SubmissionRequested' || status === 'ResubmitRequested' ? 1
          : 0;
  return <View style={styles.workflowSteps}>{['Scores', 'Permission', 'Submit', 'Review', 'Accepted'].map((label, index) => <View style={styles.workflowStep} key={label}><View style={[styles.workflowStepDot, index <= current && styles.workflowStepDotActive]}>{index < current ? <Ionicons name="checkmark" size={12} color="#FFFFFF"/> : <Text style={styles.workflowStepNumber}>{index + 1}</Text>}</View><Text style={[styles.workflowStepLabel, index <= current && styles.workflowStepLabelActive]}>{label}</Text>{index < 4 ? <View style={[styles.workflowStepLine, index < current && styles.workflowStepLineActive]}/> : null}</View>)}</View>;
}

function uniqueCourseAssignments(schedule: ScheduleItem[]) {
  return [...new Map(schedule.map(item => [assignmentKey(item), item])).values()];
}
function assignmentKey(item?: ScheduleItem) { return item ? [item.values.courseId, item.values.departmentId, item.values.yearLevel, item.values.shift].join('|') : ''; }
function sameCohort(course: ScheduleItem, student: StudentItem) { return (!course.values.departmentId || course.values.departmentId === student.values.departmentId) && course.values.yearLevel === student.values.year && (!course.values.shift || course.values.shift === student.values.shift); }
function gradeFor(grades: GradeItem[], studentId: string) { return grades.find(item => item.values.studentId === studentId); }
function courseWorkflowStatus(grades: GradeItem[]) {
  if (!grades.length) return 'Draft';
  const statuses = [...new Set(grades.map(item => item.values.reviewStatus))];
  return statuses.length === 1 ? statuses[0] : 'Inconsistent';
}
function courseActionAvailable(status: string) { return ['Draft', 'Rejected', 'SubmissionAuthorized', 'Approved', 'ResubmitAuthorized'].includes(status); }
function courseActionLabel(status: string) {
  if (status === 'SubmissionAuthorized') return 'Submit all Student results';
  if (status === 'Approved') return 'Request course resubmission';
  if (status === 'ResubmitAuthorized') return 'Resubmit all Student results';
  if (status === 'Rejected') return 'Request course approval again';
  return 'Request course submission approval';
}
function workflowDisplay(status: string) {
  const labels: Record<string, string> = { Draft: 'Course draft', SubmissionRequested: 'Approval pending', SubmissionAuthorized: 'Submission authorized', Submitted: 'Final review pending', Pending: 'Final review pending', Approved: 'Accepted', Rejected: 'Rejected', ResubmitRequested: 'Resubmission pending', ResubmitAuthorized: 'Resubmission authorized', Inconsistent: 'Refresh required' };
  return labels[status] ?? status;
}
function workflowMessage(status: string, count: number) {
  if (status === 'Draft') return `Complete all ${count} Student grades, then send one approval request for this assigned course.`;
  if (status === 'SubmissionRequested') return 'The complete course roster is waiting for Administrator submission authorization.';
  if (status === 'SubmissionAuthorized') return 'Administrator authorized this roster. Submit all Student results together.';
  if (status === 'Submitted' || status === 'Pending') return 'The complete course roster is waiting for Administrator final acceptance.';
  if (status === 'Approved') return 'The complete course roster is accepted. Request permission before making a new submission.';
  if (status === 'Rejected') return 'Correct the Student grades and send one new approval request for the complete roster.';
  if (status === 'ResubmitRequested') return 'The course resubmission request is waiting for Administrator authorization.';
  if (status === 'ResubmitAuthorized') return 'Correct all required grades, then resubmit the complete course roster together.';
  return 'The course roster has inconsistent states. Refresh before continuing.';
}
function numericInput(value: string) { const cleaned = value.replace(/[^0-9.]/g, ''); const [whole = '', ...decimal] = cleaned.split('.'); return decimal.length ? `${whole}.${decimal.join('').slice(0, 2)}` : whole.slice(0, 5); }
function complete(value: string, maximum: number) { const number = Number(value); return value.trim() !== '' && Number.isFinite(number) && number >= 0 && number <= maximum; }

const styles = StyleSheet.create({
  courseTabs: { gap: 10, paddingRight: 12, paddingBottom: 3 },
  courseTab: { width: 190, minHeight: 105, padding: 16, borderRadius: radius.large, borderWidth: 1, borderColor: palette.line, backgroundColor: 'white' },
  courseTabActive: { borderColor: palette.blue, backgroundColor: palette.blue },
  courseTabCode: { color: palette.blue, fontSize: 12, fontWeight: '900' },
  courseTabName: { color: palette.ink, fontSize: 16, fontWeight: '900', marginTop: 6 },
  courseTabCohort: { color: palette.muted, fontSize: 12, marginTop: 6 },
  courseTabTextActive: { color: '#FFFFFF' },
  workflowCard: { gap: 15, borderTopWidth: 6, borderTopColor: palette.gold },
  workflowHeader: { flexDirection: 'row', alignItems: 'center', gap: 10 },
  workflowIcon: { width: 46, height: 46, alignItems: 'center', justifyContent: 'center', borderRadius: 15, backgroundColor: palette.bluePale },
  workflowCopy: { flex: 1 },
  workflowTitle: { color: palette.ink, fontSize: 17, fontWeight: '900' },
  workflowDetail: { color: palette.muted, fontSize: 13, lineHeight: 19, marginTop: 5 },
  workflowSteps: { flexDirection: 'row', alignItems: 'flex-start', justifyContent: 'space-between', paddingVertical: 4 },
  workflowStep: { flex: 1, alignItems: 'center', position: 'relative', gap: 6 },
  workflowStepDot: { zIndex: 2, width: 27, height: 27, borderRadius: 14, alignItems: 'center', justifyContent: 'center', backgroundColor: '#E6EBF3', borderWidth: 2, borderColor: '#FFFFFF' },
  workflowStepDotActive: { backgroundColor: palette.blue },
  workflowStepNumber: { color: palette.muted, fontSize: 11, fontWeight: '900' },
  workflowStepLabel: { color: palette.muted, fontSize: 10, fontWeight: '700', textAlign: 'center' },
  workflowStepLabelActive: { color: palette.blueDark, fontWeight: '900' },
  workflowStepLine: { position: 'absolute', zIndex: 1, top: 13, left: '63%', width: '74%', height: 3, backgroundColor: '#E6EBF3' },
  workflowStepLineActive: { backgroundColor: palette.blue },
  progressTrack: { height: 8, overflow: 'hidden', borderRadius: 4, backgroundColor: '#E6EBF3' },
  progressValue: { height: '100%', backgroundColor: palette.blue },
  progressCopy: { flexDirection: 'row', justifyContent: 'space-between', gap: 8 },
  courseAction: { minHeight: 52, flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 8, borderRadius: radius.small, backgroundColor: palette.blue },
  courseActionText: { color: 'white', fontSize: 14, fontWeight: '900' },
  buttonDisabled: { opacity: 0.4 },
  reviewNote: { color: palette.red, fontSize: 13, lineHeight: 19, padding: 13, borderRadius: radius.small, backgroundColor: palette.redPale },
  componentInputs: { gap: 9, marginTop: 11 },
  componentInput: { gap: 5 },
  componentLabel: { color: palette.ink, fontSize: 14, fontWeight: '900' },
  componentInputRow: { flexDirection: 'row', alignItems: 'center', gap: 8 },
  scoreInput: { flex: 1, height: 48, paddingHorizontal: 14, borderRadius: radius.small, borderWidth: 1, borderColor: '#C8D4E5', color: palette.ink, backgroundColor: palette.canvas, fontSize: 16, fontWeight: '800' },
  inputDisabled: { backgroundColor: '#F2F4F7', color: palette.muted },
  maximum: { width: 48, color: palette.muted, fontSize: 14, fontWeight: '800' },
  studentState: { flexDirection: 'row', alignItems: 'center', gap: 6, marginTop: 11 },
  publishedHeader: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: 10 },
  resultCourse: { color: palette.ink, fontWeight: '900', fontSize: 17 },
  resultCode: { color: palette.muted, fontSize: 12, marginTop: 5 },
  resultScore: { color: palette.blueDark, fontSize: 21, fontWeight: '900' },
  publishedSummary: { flexDirection: 'row', flexWrap: 'wrap', gap: 8, marginTop: 15 },
  summaryPill: { overflow: 'hidden', paddingHorizontal: 10, paddingVertical: 7, borderRadius: radius.pill, backgroundColor: palette.bluePale, color: palette.blueDark, fontSize: 12, fontWeight: '800' },
  resultComponents: { marginTop: 12, borderTopWidth: 1, borderTopColor: palette.line },
  publishedCourse: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: 10, paddingVertical: 10, borderBottomWidth: StyleSheet.hairlineWidth, borderBottomColor: palette.line },
});
