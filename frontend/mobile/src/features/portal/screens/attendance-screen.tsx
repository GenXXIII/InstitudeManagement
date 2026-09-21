import Ionicons from '@expo/vector-icons/Ionicons';
import { useEffect, useState } from 'react';
import { Alert, Pressable, StyleSheet, Text, View } from 'react-native';
import { Card, EmptyBlock, PortalPage, portalStyles, SectionHeading, StatusPill } from '@/components/portal-ui';
import { palette, radius } from '@/constants/theme';
import type { MobileRole } from '@/features/auth/auth-context';
import type { ScheduleItem } from '../portal-types';
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
    <TeacherPermissionRequests/>
    <SectionHeading title="Today’s timetable" detail={todayName}/>
    <View style={portalStyles.stack}>{todaySchedules.length ? todaySchedules.map(item => <ClassStartCard item={item} now={now} started={portal.startedScheduleIds.includes(item.id)} starting={startingId === item.id} onStart={() => void start(item)} onView={() => setStartedScheduleId(item.id)} key={item.id}/>) : <EmptyBlock icon="calendar-clear-outline" title="No class to start today" detail="A Start class action appears when the Teacher has an active timetable enrollment for today."/>}</View>
  </>;

  return <>
    <RunningClassBanner item={activeSchedule} now={now}/>
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
    <View style={styles.runningHeader}><View style={styles.runningState}><View style={styles.runningDot}/><Text style={styles.runningLabel}>RUNNING CLASS</Text></View><Text style={styles.runningClock}>{formatClock(now)}</Text></View>
    <Text style={styles.runningCourse}>{item.values.course}</Text>
    <Text style={styles.runningMeta}>{item.values.startsAt} – {item.values.endsAt} · {item.values.classroom} · Year {item.values.yearLevel} · {item.values.shift}</Text>
  </View>;
}

function StudentAttendance() {
  const portal = usePortal();
  const ordered = [...portal.attendance].sort((a, b) => b.values.date.localeCompare(a.values.date));
  return <>
    <SectionHeading title="Attendance history" detail={`${ordered.length} records`}/>
    <View style={portalStyles.stack}>{ordered.length ? ordered.map(item => <Card key={item.id}><View style={styles.recordTop}><View><Text style={styles.recordDate}>{formatDate(item.values.date)}</Text><Text style={styles.recordCode}>{item.values.attendanceCode} · {item.values.term}</Text></View><StatusPill value={item.values.status}/></View><Text style={styles.recordMeta}>Check in {item.values.checkedInAt || 'not recorded'} · {item.values.method || 'Institute record'}</Text></Card>) : <EmptyBlock icon="document-outline" title="No attendance yet" detail="Attendance recorded by your Teacher will appear here."/>}</View>
  </>;
}

function formatDate(value: string) {
  const date = new Date(`${value}T00:00:00`);
  return Number.isNaN(date.valueOf()) ? value : new Intl.DateTimeFormat('en-GB', { weekday: 'short', day: '2-digit', month: 'short', year: 'numeric' }).format(date);
}

function formatClock(value: Date) {
  return new Intl.DateTimeFormat('en-GB', { hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false }).format(value);
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

function isWithinTimetable(item: ScheduleItem, now: Date) {
  const current = `${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}`;
  return current >= item.values.startsAt && current < item.values.endsAt;
}

function timetableActionLabel(item: ScheduleItem, now: Date) {
  const current = `${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}`;
  return current < item.values.startsAt ? `Starts at ${item.values.startsAt}` : 'Class ended';
}

const styles = StyleSheet.create({
  classStartHeader: { flexDirection: 'row', alignItems: 'center', gap: 11 },
  classStartIcon: { width: 42, height: 42, borderRadius: radius.small, alignItems: 'center', justifyContent: 'center', backgroundColor: palette.bluePale },
  classStartCopy: { flex: 1 },
  classStartCourse: { color: palette.ink, fontSize: 16, fontWeight: '800' },
  classStartTime: { color: palette.blue, fontSize: 12, fontWeight: '700', marginTop: 4 },
  classStartMeta: { color: palette.muted, fontSize: 11, marginTop: 12 },
  startClassButton: { minHeight: 44, flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 8, marginTop: 15, borderRadius: radius.small, backgroundColor: palette.blue },
  startClassButtonDisabled: { backgroundColor: '#9AA4C7' },
  startClassText: { color: '#FFFFFF', fontSize: 13, fontWeight: '800' },
  runningBanner: { overflow: 'hidden', padding: 19, borderRadius: radius.large, backgroundColor: palette.blueDark },
  runningHeader: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: 12 },
  runningState: { flexDirection: 'row', alignItems: 'center', gap: 7 },
  runningDot: { width: 8, height: 8, borderRadius: 4, backgroundColor: '#55D8A4' },
  runningLabel: { color: '#DDE3FF', fontSize: 10, fontWeight: '800', letterSpacing: 0.9 },
  runningClock: { color: '#FFFFFF', fontSize: 18, fontWeight: '800', fontVariant: ['tabular-nums'] },
  runningCourse: { color: '#FFFFFF', fontSize: 23, lineHeight: 29, fontWeight: '800', marginTop: 19 },
  runningMeta: { color: '#C8D1F3', fontSize: 11, lineHeight: 18, marginTop: 7 },
  studentAttendanceRow: { minHeight: 48, flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: 14 },
  studentAttendanceIdentity: { flex: 1, minWidth: 0 },
  studentAttendanceName: { color: palette.ink, fontSize: 15, fontWeight: '800' },
  studentAttendancePublicId: { color: palette.muted, fontSize: 11, marginTop: 5 },
  permissionRequestTop: { flexDirection: 'row', alignItems: 'center', gap: 12 },
  permissionReason: { color: palette.ink, fontSize: 12, lineHeight: 18, marginTop: 12 },
  permissionReviewActions: { flexDirection: 'row', justifyContent: 'flex-end', gap: 8, marginTop: 13 },
  permissionReject: { minWidth: 90, minHeight: 40, alignItems: 'center', justifyContent: 'center', borderWidth: 1, borderColor: '#E7BCC3', borderRadius: radius.small, backgroundColor: '#FFF2F3' },
  permissionRejectText: { color: '#B74450', fontWeight: '800' },
  permissionApprove: { minWidth: 100, minHeight: 40, alignItems: 'center', justifyContent: 'center', borderRadius: radius.small, backgroundColor: palette.blue },
  permissionApproveText: { color: '#FFFFFF', fontWeight: '800' },
  pressed: { opacity: 0.65 },
  recordTop: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
  recordDate: { color: palette.ink, fontWeight: '800', fontSize: 15 },
  recordCode: { color: palette.blue, fontSize: 10, fontWeight: '700', marginTop: 4 },
  recordMeta: { color: palette.muted, fontSize: 12, marginTop: 12 },
});
