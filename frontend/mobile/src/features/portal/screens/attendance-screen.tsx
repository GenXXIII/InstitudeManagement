import Ionicons from '@expo/vector-icons/Ionicons';
import { CameraView, type BarcodeScanningResult, useCameraPermissions } from 'expo-camera';
import { useEffect, useState } from 'react';
import { ActivityIndicator, Alert, Modal, Pressable, StyleSheet, Text, View } from 'react-native';
import QRCode from 'react-native-qrcode-svg';
import { Card, EmptyBlock, PortalPage, portalStyles, SectionHeading, StatusPill } from '@/components/portal-ui';
import { palette, radius } from '@/constants/theme';
import type { MobileRole } from '@/features/auth/auth-context';
import type { ClassAttendanceQr, ScheduleItem } from '../portal-types';
import { usePortal } from '../portal-context';

export function AttendanceScreen({ role }: { role: MobileRole }) {
  return <PortalPage title={role === 'teacher' ? 'Class attendance' : 'My attendance'} subtitle={role === 'teacher' ? 'View attendance for students in your current class. Attendance is read-only for Teachers.' : 'Your personal attendance history is read-only and comes from institute records.'}>
    <AttendanceContent role={role}/>
  </PortalPage>;
}

export function AttendanceContent({ role }: { role: MobileRole }) {
  return role === 'student' ? <StudentAttendance/> : <TeacherAttendance/>;
}

function TeacherAttendance() {
  const portal = usePortal();
  const [startingId, setStartingId] = useState('');
  const [startedScheduleId, setStartedScheduleId] = useState('');
  const [now, setNow] = useState(() => new Date());
  const todayName = new Intl.DateTimeFormat('en-US', { weekday: 'long' }).format(new Date());
  const todaySchedules = portal.schedule.filter(item => item.values.dayOfWeek === todayName && item.values.status !== 'Cancelled').sort((a, b) => a.values.startsAt.localeCompare(b.values.startsAt));
  const activeSchedule = todaySchedules.find(item => item.id === startedScheduleId && isWithinTimetable(item, now))
    ?? todaySchedules.find(item => portal.startedScheduleIds.includes(item.id) && isWithinTimetable(item, now));
  const roster = !activeSchedule ? [] : portal.students.filter(student =>
      (!activeSchedule.values.departmentId || activeSchedule.values.departmentId === student.values.departmentId)
      && activeSchedule.values.yearLevel === student.values.year
      && (!activeSchedule.values.shift || activeSchedule.values.shift === student.values.shift));
  const pendingPermissions = portal.permissionRequests.filter(item => item.status === 'Pending').length;
  const startedToday = todaySchedules.filter(item => portal.startedScheduleIds.includes(item.id)).length;

  useEffect(() => {
    const timer = setInterval(() => setNow(new Date()), 1000);
    return () => clearInterval(timer);
  }, []);

  async function start(item: ScheduleItem) {
    if (!portal.profile) return;
    setStartingId(item.id);
    try {
      await portal.startClass(item.id, portal.profile.id);
      setStartedScheduleId(item.id);
    } catch (reason) {
      Alert.alert('Class not started', reason instanceof Error ? reason.message : 'Try again.');
    } finally {
      setStartingId('');
    }
  }

  if (!activeSchedule) return <>
    <ClassControlSummary classes={todaySchedules.length} started={startedToday} permissions={pendingPermissions}/>
    <TeacherPermissionRequests/>
    <SectionHeading title="Today’s timetable" detail={todayName}/>
    <View style={portalStyles.stack}>{todaySchedules.length ? todaySchedules.map(item => <ClassStartCard item={item} now={now} started={portal.startedScheduleIds.includes(item.id)} starting={startingId === item.id} onStart={() => void start(item)} onView={() => setStartedScheduleId(item.id)} key={item.id}/>) : <EmptyBlock icon="calendar-clear-outline" title="No class to start today" detail="A Start class action appears when the Teacher has an active timetable enrollment for today."/>}</View>
  </>;

  return <>
    <ClassControlSummary classes={todaySchedules.length} started={startedToday} permissions={pendingPermissions}/>
    <RunningClassBanner item={activeSchedule} now={now}/>
    <TeacherAttendanceQr item={activeSchedule} now={now}/>
    <TeacherPermissionRequests/>
    <SectionHeading title="Student attendance" detail={`Read-only · ${roster.length} students`}/>
    <View style={portalStyles.stack}>{roster.length ? roster.map(student => {
      const record = portal.attendance
        .filter(item => item.values.studentId === student.id && item.values.date === localDateKey(now))
        .sort((a, b) => b.values.checkedInAt.localeCompare(a.values.checkedInAt))[0];
      return <Card key={student.id}><View style={styles.studentAttendanceRow}><View style={styles.studentAttendanceIdentity}><Text style={styles.studentAttendanceName}>{student.values.name}</Text><Text style={styles.studentAttendancePublicId}>Public ID {student.values.publicId || 'not assigned'}</Text></View><StatusPill value={teacherAttendanceStatus(record?.values.status)}/></View></Card>;
    }) : <EmptyBlock icon="people-outline" title="No students in this class" detail="Students appear after Administrator completes Student and Timetable Enrollment for this class’s department, year, and shift."/>}</View>
  </>;
}

function ClassControlSummary({ classes, started, permissions }: { classes: number; started: number; permissions: number }) {
  const items = [
    { label: 'Today classes', value: classes, icon: 'calendar-outline' as const, tone: palette.bluePale, color: palette.blue },
    { label: 'Started', value: started, icon: 'play-outline' as const, tone: palette.greenPale, color: palette.green },
    { label: 'To review', value: permissions, icon: 'document-text-outline' as const, tone: palette.goldPale, color: palette.gold },
  ];
  return <View style={styles.controlSummary}>{items.map(item => <View style={styles.controlMetric} key={item.label}><View style={[styles.controlMetricIcon, { backgroundColor: item.tone }]}><Ionicons name={item.icon} size={18} color={item.color}/></View><Text style={styles.controlMetricValue}>{item.value}</Text><Text style={styles.controlMetricLabel}>{item.label}</Text></View>)}</View>;
}

function TeacherPermissionRequests() {
  const portal = usePortal();
  const [working, setWorking] = useState('');
  const requests = portal.permissionRequests.filter(item => item.status === 'Pending');
  if (!requests.length) return null;
  async function review(id: string, decision: 'Approved' | 'Rejected') {
    setWorking(id);
    try { await portal.reviewPermission(id, decision); }
    catch (reason) { Alert.alert('Permission not reviewed', reason instanceof Error ? reason.message : 'Try again.'); }
    finally { setWorking(''); }
  }
  return <View style={portalStyles.stack}><SectionHeading title="Whole-day permission requests" detail={`${requests.length} waiting`}/>{requests.map(item => <Card key={item.id}><View style={styles.permissionRequestTop}><View style={styles.studentAttendanceIdentity}><Text style={styles.studentAttendanceName}>{item.studentName}</Text><Text style={styles.studentAttendancePublicId}>Public ID {item.studentPublicId || 'not assigned'} · {formatDate(item.sessionDate)} · whole day</Text></View><StatusPill value="Pending"/></View><Text style={styles.permissionReason}>{item.reason}</Text><View style={styles.permissionReviewActions}><Pressable disabled={working === item.id} onPress={() => void review(item.id, 'Rejected')} style={styles.permissionReject}><Text style={styles.permissionRejectText}>Reject</Text></Pressable><Pressable disabled={working === item.id} onPress={() => void review(item.id, 'Approved')} style={styles.permissionApprove}><Text style={styles.permissionApproveText}>{working === item.id ? 'Saving…' : 'Approve whole day'}</Text></Pressable></View></Card>)}</View>;
}

function ClassStartCard({ item, now, started, starting, onStart, onView }: { item: ScheduleItem; now: Date; started: boolean; starting: boolean; onStart: () => void; onView: () => void }) {
  const canStart = isWithinTimetable(item, now);
  const actionLabel = started && canStart ? 'View attendance' : starting ? 'Starting\u2026' : canStart ? 'Start class now' : timetableActionLabel(item, now);
  return <Card>
    <View style={styles.classStartHeader}><View style={styles.classStartIcon}><Ionicons name="school-outline" size={20} color={palette.blue}/></View><View style={styles.classStartCopy}><Text style={styles.classStartCourse}>{item.values.course}</Text><Text style={styles.classStartTime}>{item.values.startsAt} – {item.values.endsAt}</Text></View></View>
    <Text style={styles.classStartMeta}>{item.values.classroom} · Year {item.values.yearLevel} · {item.values.shift}</Text>
    <Pressable disabled={!canStart || starting} accessibilityRole="button" accessibilityLabel={`${actionLabel}: ${item.values.course}`} onPress={started ? onView : onStart} style={({ pressed }) => [styles.startClassButton, !canStart && styles.startClassButtonDisabled, pressed && styles.pressed]}><Ionicons name={started && canStart ? 'people' : 'play'} size={17} color="#FFFFFF"/><Text style={styles.startClassText}>{actionLabel}</Text></Pressable>
  </Card>;
}

function RunningClassBanner({ item, now }: { item: ScheduleItem; now: Date }) {
  return <View style={styles.runningBanner}>
    <View style={styles.runningHeader}><View style={styles.runningState}><View style={styles.runningDot}/><Text style={styles.runningLabel}>RUNNING CLASS</Text></View><View style={styles.timeLeftBlock}><Text style={styles.timeLeftLabel}>TIME LEFT</Text><Text style={styles.runningClock}>{formatTimeLeft(secondsUntilClassEnds(item, now))}</Text></View></View>
    <Text style={styles.runningCourse}>{item.values.course}</Text>
    <Text style={styles.runningMeta}>{item.values.startsAt} – {item.values.endsAt} · {item.values.classroom} · Year {item.values.yearLevel} · {item.values.shift}</Text>
  </View>;
}

function TeacherAttendanceQr({ item, now }: { item: ScheduleItem; now: Date }) {
  const { getClassAttendanceQr } = usePortal();
  const [qr, setQr] = useState<ClassAttendanceQr | null>(null);
  const [message, setMessage] = useState('');

  useEffect(() => {
    let active = true;
    async function rotate() {
      try {
        const next = await getClassAttendanceQr(item.id);
        if (active) { setQr(next); setMessage(''); }
      } catch (reason) {
        if (active) setMessage(reason instanceof Error ? reason.message : 'Could not create the attendance QR.');
      }
    }
    void rotate();
    const timer = setInterval(() => void rotate(), 15000);
    return () => { active = false; clearInterval(timer); };
  }, [getClassAttendanceQr, item.id]);

  const refreshSeconds = qr ? Math.max(0, Math.ceil((new Date(qr.expiresAtUtc).getTime() - now.getTime()) / 1000)) : 0;
  return <Card style={styles.qrCard}>
    <View style={styles.qrCardHeader}><View style={styles.qrCardIcon}><Ionicons name="qr-code-outline" size={22} color={palette.blue}/></View><View style={styles.qrCardCopy}><Text style={styles.qrCardTitle}>Dynamic attendance QR</Text><Text style={styles.qrCardDetail}>Students tap Start class, then scan this rotating code.</Text></View></View>
    <View style={styles.qrFrame}>{qr ? <QRCode value={qr.payload} size={210} quietZone={8} backgroundColor="#FFFFFF" color="#0B1423"/> : <ActivityIndicator size="large" color={palette.blue}/>}</View>
    <View style={styles.qrRotation}><View style={styles.qrRotationDot}/><Text style={styles.qrRotationText}>{qr ? `Secure code rotates automatically · ${refreshSeconds}s valid` : 'Creating secure class code…'}</Text></View>
    {message ? <Text style={styles.qrError}>{message}</Text> : null}
  </Card>;
}

function StudentAttendance() {
  const portal = usePortal();
  const [now, setNow] = useState(() => new Date());
  const [scannerSchedule, setScannerSchedule] = useState<ScheduleItem | null>(null);
  const todayName = new Intl.DateTimeFormat('en-US', { weekday: 'long' }).format(now);
  const activeSchedule = portal.schedule
    .filter(item => item.values.dayOfWeek === todayName && item.values.status !== 'Cancelled')
    .find(item => portal.startedScheduleIds.includes(item.id) && isWithinTimetable(item, now));
  const todayRecord = portal.attendance
    .filter(item => item.values.date === localDateKey(now))
    .sort((a, b) => b.values.checkedInAt.localeCompare(a.values.checkedInAt))[0];
  const attendanceAssigned = isStudentCheckedIn(todayRecord?.values.status);
  const ordered = [...portal.attendance].sort((a, b) => b.values.date.localeCompare(a.values.date));

  useEffect(() => {
    const clock = setInterval(() => setNow(new Date()), 1000);
    return () => clearInterval(clock);
  }, []);

  return <>
    {activeSchedule ? <>
      <RunningClassBanner item={activeSchedule} now={now}/>
      <Card style={styles.studentStartCard}>
        <View style={styles.studentStartTop}><View style={styles.studentStartIcon}><Ionicons name={attendanceAssigned ? 'checkmark-circle-outline' : 'scan-outline'} size={23} color={attendanceAssigned ? palette.green : palette.blue}/></View><View style={styles.studentStartCopy}><Text style={styles.studentStartTitle}>{attendanceAssigned ? 'Attendance assigned' : 'Start this class'}</Text><Text style={styles.studentStartDetail}>{attendanceAssigned && todayRecord ? `${todayRecord.values.status} · ${todayRecord.values.checkedInAt || 'checked in'} · ${todayRecord.values.method || 'Dynamic QR'}` : `Your Teacher has started ${activeSchedule.values.course}. Scan the rotating QR to assign your attendance.`}</Text></View></View>
        <Pressable disabled={attendanceAssigned} onPress={() => setScannerSchedule(activeSchedule)} style={({ pressed }) => [styles.studentStartButton, attendanceAssigned && styles.studentStartButtonDone, pressed && styles.pressed]}><Ionicons name={attendanceAssigned ? 'checkmark' : 'play'} size={18} color="#FFFFFF"/><Text style={styles.startClassText}>{attendanceAssigned ? 'Attendance assigned' : 'Start class'}</Text></Pressable>
      </Card>
    </> : <View style={styles.waitingForTeacher}><Ionicons name="hourglass-outline" size={20} color={palette.blue}/><View style={styles.waitingCopy}><Text style={styles.waitingTitle}>Waiting for Teacher</Text><Text style={styles.waitingDetail}>Start class becomes available here after your Teacher starts the current timetable period.</Text></View></View>}
    <SectionHeading title="Attendance history" detail={`${ordered.length} records`}/>
    <View style={portalStyles.stack}>{ordered.length ? ordered.map(item => <Card key={item.id}><View style={styles.recordTop}><View><Text style={styles.recordDate}>{formatDate(item.values.date)}</Text><Text style={styles.recordCode}>{item.values.attendanceCode} · {item.values.term}</Text></View><StatusPill value={item.values.status}/></View><Text style={styles.recordMeta}>Check in {item.values.checkedInAt || 'not recorded'} · {item.values.method || 'Institute record'}</Text></Card>) : <EmptyBlock icon="document-outline" title="No attendance yet" detail="Attendance recorded by your Teacher will appear here."/>}</View>
    {scannerSchedule ? <ClassQrScanner item={scannerSchedule} onClose={() => setScannerSchedule(null)}/> : null}
  </>;
}

export function ClassQrScanner({ item, onClose }: { item: ScheduleItem; onClose: () => void }) {
  const portal = usePortal();
  const [permission, requestPermission] = useCameraPermissions();
  const [locked, setLocked] = useState(false);
  const [message, setMessage] = useState('');

  async function scanned(result: BarcodeScanningResult) {
    if (locked) return;
    setLocked(true);
    setMessage('Assigning your attendance…');
    try {
      const checkIn = await portal.checkInClass(item.id, result.data);
      onClose();
      Alert.alert('Class started', `Attendance assigned as ${checkIn.status} at ${checkIn.checkedInAt ?? 'now'}.`);
    } catch (reason) {
      setMessage(reason instanceof Error ? reason.message : 'This attendance QR could not be accepted.');
      setLocked(false);
    }
  }

  return <Modal visible transparent animationType="fade" onRequestClose={onClose}>
    <View style={styles.modalBackdrop}><View style={styles.scannerCard}>
      <View style={styles.scannerHeader}><View style={styles.scannerTitleCopy}><Text style={styles.scannerEyebrow}>START CLASS</Text><Text style={styles.scannerTitle}>{item.values.course}</Text><Text style={styles.scannerMeta}>{item.values.classroom} · ends {item.values.endsAt}</Text></View><Pressable accessibilityLabel="Close attendance scanner" onPress={onClose} style={styles.closeButton}><Ionicons name="close" size={21} color={palette.ink}/></Pressable></View>
      {!permission ? <ActivityIndicator size="large" color={palette.blue}/> : !permission.granted ? <View style={styles.cameraPermission}><Ionicons name="camera-outline" size={34} color={palette.blue}/><Text style={styles.cameraPermissionText}>Camera access is required to scan your Teacher&apos;s attendance QR.</Text><Pressable onPress={() => void requestPermission()} style={styles.studentStartButton}><Text style={styles.startClassText}>Allow Camera</Text></Pressable></View> : <View style={styles.cameraFrame}><CameraView style={StyleSheet.absoluteFill} barcodeScannerSettings={{ barcodeTypes: ['qr'] }} onBarcodeScanned={locked ? undefined : event => void scanned(event)}/><View style={styles.scanGuide}/></View>}
      <Text style={styles.scannerHelp}>Scan the current rotating QR on your Teacher&apos;s screen. Expired codes are rejected automatically.</Text>
      {message ? <Text style={styles.scannerMessage}>{message}</Text> : null}
    </View></View>
  </Modal>;
}

function formatDate(value: string) {
  const date = new Date(`${value}T00:00:00`);
  return Number.isNaN(date.valueOf()) ? value : new Intl.DateTimeFormat('en-GB', { weekday: 'short', day: '2-digit', month: 'short', year: 'numeric' }).format(date);
}

function secondsUntilClassEnds(item: ScheduleItem, now: Date) {
  const [hour, minute] = item.values.endsAt.split(':').map(Number);
  const end = new Date(now.getFullYear(), now.getMonth(), now.getDate(), hour, minute, 0, 0);
  return Math.max(0, Math.ceil((end.getTime() - now.getTime()) / 1000));
}

function formatTimeLeft(totalSeconds: number) {
  const hours = Math.floor(totalSeconds / 3600);
  const minutes = Math.floor((totalSeconds % 3600) / 60);
  const seconds = totalSeconds % 60;
  return `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
}

function localDateKey(value: Date) {
  const year = value.getFullYear();
  const month = String(value.getMonth() + 1).padStart(2, '0');
  const day = String(value.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

function teacherAttendanceStatus(value?: string) {
  const status = value?.trim().toLowerCase();
  if (status === 'present' || status === 'late') return 'Present';
  if (status === 'permission' || status === 'excused') return 'Permission';
  return 'Absent';
}

function isStudentCheckedIn(value?: string) {
  const status = value?.trim().toLowerCase();
  return status === 'present' || status === 'late';
}

function isWithinTimetable(item: ScheduleItem, now: Date) {
  const current = `${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}`;
  return current >= item.values.startsAt && current < item.values.endsAt;
}

function timetableActionLabel(item: ScheduleItem, now: Date) {
  const current = `${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}`;
  return current < item.values.startsAt ? `Starts at ${item.values.startsAt}` : 'Class ended';
}

const styles = StyleSheet.create({
  controlSummary: { flexDirection: 'row', gap: 9 },
  controlMetric: { flex: 1, minHeight: 116, alignItems: 'flex-start', justifyContent: 'center', padding: 13, borderRadius: radius.medium, backgroundColor: '#FFFFFF', borderWidth: 1, borderColor: palette.line },
  controlMetricIcon: { width: 36, height: 36, borderRadius: 12, alignItems: 'center', justifyContent: 'center' },
  controlMetricValue: { color: palette.ink, fontSize: 25, fontWeight: '900', marginTop: 10 },
  controlMetricLabel: { color: palette.muted, fontSize: 11, lineHeight: 15, fontWeight: '700', marginTop: 2 },
  classStartHeader: { flexDirection: 'row', alignItems: 'center', gap: 11 },
  classStartIcon: { width: 48, height: 48, borderRadius: 16, alignItems: 'center', justifyContent: 'center', backgroundColor: palette.bluePale },
  classStartCopy: { flex: 1 },
  classStartCourse: { color: palette.ink, fontSize: 18, fontWeight: '900' },
  classStartTime: { color: palette.blue, fontSize: 14, fontWeight: '800', marginTop: 5 },
  classStartMeta: { color: palette.muted, fontSize: 13, marginTop: 13 },
  startClassButton: { minHeight: 50, flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 8, marginTop: 16, borderRadius: radius.small, backgroundColor: palette.blue },
  startClassButtonDisabled: { backgroundColor: '#9AA4C7' },
  startClassText: { color: '#FFFFFF', fontSize: 14, fontWeight: '900' },
  runningBanner: { overflow: 'hidden', padding: 21, borderRadius: radius.large, backgroundColor: palette.blueDark, borderBottomWidth: 7, borderBottomColor: palette.gold },
  runningHeader: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: 12 },
  runningState: { flexDirection: 'row', alignItems: 'center', gap: 7 },
  runningDot: { width: 8, height: 8, borderRadius: 4, backgroundColor: '#55D8A4' },
  runningLabel: { color: '#E8EEFF', fontSize: 11, fontWeight: '900', letterSpacing: 1 },
  timeLeftBlock: { alignItems: 'flex-end' },
  timeLeftLabel: { color: '#AFC0EF', fontSize: 9, fontWeight: '900', letterSpacing: 1.1, marginBottom: 2 },
  runningClock: { color: '#FFFFFF', fontSize: 20, fontWeight: '900', fontVariant: ['tabular-nums'] },
  runningCourse: { color: '#FFFFFF', fontSize: 26, lineHeight: 32, fontWeight: '900', marginTop: 20 },
  runningMeta: { color: '#D3DFFF', fontSize: 13, lineHeight: 20, marginTop: 8 },
  qrCard: { alignItems: 'center', gap: 16, borderColor: '#C9D7FA' },
  qrCardHeader: { width: '100%', flexDirection: 'row', alignItems: 'center', gap: 12 },
  qrCardIcon: { width: 45, height: 45, alignItems: 'center', justifyContent: 'center', borderRadius: 15, backgroundColor: palette.bluePale },
  qrCardCopy: { flex: 1 },
  qrCardTitle: { color: palette.ink, fontSize: 17, fontWeight: '900' },
  qrCardDetail: { color: palette.muted, fontSize: 12, lineHeight: 17, marginTop: 3 },
  qrFrame: { width: 236, minHeight: 236, alignItems: 'center', justifyContent: 'center', padding: 5, borderRadius: radius.medium, backgroundColor: '#FFFFFF', borderWidth: 1, borderColor: palette.line },
  qrRotation: { flexDirection: 'row', alignItems: 'center', gap: 7, paddingHorizontal: 11, paddingVertical: 7, borderRadius: radius.pill, backgroundColor: palette.greenPale },
  qrRotationDot: { width: 7, height: 7, borderRadius: 4, backgroundColor: palette.green },
  qrRotationText: { color: palette.green, fontSize: 11, fontWeight: '800' },
  qrError: { color: palette.red, fontSize: 12, lineHeight: 18, textAlign: 'center' },
  studentStartCard: { gap: 16, borderColor: '#C9D7FA' },
  studentStartTop: { flexDirection: 'row', alignItems: 'center', gap: 12 },
  studentStartIcon: { width: 48, height: 48, alignItems: 'center', justifyContent: 'center', borderRadius: 16, backgroundColor: palette.bluePale },
  studentStartCopy: { flex: 1 },
  studentStartTitle: { color: palette.ink, fontSize: 17, fontWeight: '900' },
  studentStartDetail: { color: palette.muted, fontSize: 12, lineHeight: 18, marginTop: 4 },
  studentStartButton: { minHeight: 50, flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 8, paddingHorizontal: 16, borderRadius: radius.small, backgroundColor: palette.blue },
  studentStartButtonDone: { backgroundColor: palette.green },
  waitingForTeacher: { flexDirection: 'row', alignItems: 'center', gap: 12, padding: 15, borderRadius: radius.medium, backgroundColor: palette.bluePale, borderWidth: 1, borderColor: '#C9D7FA' },
  waitingCopy: { flex: 1 },
  waitingTitle: { color: palette.blueDark, fontSize: 14, fontWeight: '900' },
  waitingDetail: { color: palette.muted, fontSize: 12, lineHeight: 17, marginTop: 3 },
  modalBackdrop: { flex: 1, justifyContent: 'center', padding: 18, backgroundColor: 'rgba(7, 15, 35, 0.72)' },
  scannerCard: { gap: 16, padding: 18, borderRadius: radius.large, backgroundColor: '#FFFFFF' },
  scannerHeader: { flexDirection: 'row', alignItems: 'flex-start', justifyContent: 'space-between', gap: 12 },
  scannerTitleCopy: { flex: 1 },
  scannerEyebrow: { color: palette.blue, fontSize: 10, fontWeight: '900', letterSpacing: 1.1 },
  scannerTitle: { color: palette.ink, fontSize: 21, fontWeight: '900', marginTop: 4 },
  scannerMeta: { color: palette.muted, fontSize: 12, marginTop: 4 },
  closeButton: { width: 38, height: 38, alignItems: 'center', justifyContent: 'center', borderRadius: 19, backgroundColor: palette.canvas },
  cameraPermission: { alignItems: 'center', gap: 14, paddingVertical: 24 },
  cameraPermissionText: { color: palette.muted, fontSize: 13, lineHeight: 19, textAlign: 'center' },
  cameraFrame: { height: 310, overflow: 'hidden', borderRadius: radius.medium, backgroundColor: '#0B1423' },
  scanGuide: { position: 'absolute', top: 49, right: 34, bottom: 49, left: 34, borderWidth: 3, borderColor: '#FFFFFF', borderRadius: 20 },
  scannerHelp: { color: palette.muted, fontSize: 12, lineHeight: 18, textAlign: 'center' },
  scannerMessage: { color: palette.red, fontSize: 12, lineHeight: 18, fontWeight: '800', textAlign: 'center' },
  studentAttendanceRow: { minHeight: 48, flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: 14 },
  studentAttendanceIdentity: { flex: 1, minWidth: 0 },
  studentAttendanceName: { color: palette.ink, fontSize: 17, fontWeight: '900' },
  studentAttendancePublicId: { color: palette.muted, fontSize: 13, marginTop: 5 },
  permissionRequestTop: { flexDirection: 'row', alignItems: 'center', gap: 12 },
  permissionReason: { color: palette.ink, fontSize: 14, lineHeight: 21, marginTop: 14, padding: 12, borderRadius: radius.small, backgroundColor: palette.canvas },
  permissionReviewActions: { flexDirection: 'row', justifyContent: 'flex-end', gap: 8, marginTop: 13 },
  permissionReject: { minWidth: 96, minHeight: 46, alignItems: 'center', justifyContent: 'center', borderWidth: 1, borderColor: '#F0C5C5', borderRadius: radius.small, backgroundColor: palette.redPale },
  permissionRejectText: { color: palette.red, fontSize: 13, fontWeight: '900' },
  permissionApprove: { minWidth: 128, minHeight: 46, alignItems: 'center', justifyContent: 'center', borderRadius: radius.small, backgroundColor: palette.blue },
  permissionApproveText: { color: '#FFFFFF', fontSize: 13, fontWeight: '900' },
  pressed: { opacity: 0.65 },
  recordTop: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
  recordDate: { color: palette.ink, fontWeight: '900', fontSize: 17 },
  recordCode: { color: palette.blue, fontSize: 12, fontWeight: '800', marginTop: 5 },
  recordMeta: { color: palette.muted, fontSize: 14, marginTop: 13 },
});
