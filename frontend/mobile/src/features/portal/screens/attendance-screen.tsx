import { useMemo, useState } from 'react';
import { Alert, Pressable, StyleSheet, Text, View } from 'react-native';
import { Card, EmptyBlock, Identity, MetricCard, PortalPage, portalStyles, SectionHeading, StatusPill } from '@/components/portal-ui';
import { palette, radius } from '@/constants/theme';
import type { MobileRole } from '@/features/auth/auth-context';
import { usePortal } from '../portal-context';

const attendanceOptions = ['Present', 'Late', 'Absent'] as const;

export function AttendanceScreen({ role }: { role: MobileRole }) {
  if (role === 'student') return <StudentAttendance/>;
  return <TeacherAttendance/>;
}

function TeacherAttendance() {
  const portal = usePortal();
  const [savingId, setSavingId] = useState('');
  const todayName = new Intl.DateTimeFormat('en-US', { weekday: 'long' }).format(new Date());
  const todaySchedules = portal.schedule.filter(item => item.values.dayOfWeek === todayName);
  const roster = useMemo(() => {
    if (!todaySchedules.length) return portal.students;
    return portal.students.filter(student => todaySchedules.some(schedule =>
      (!schedule.values.departmentId || schedule.values.departmentId === student.values.departmentId) && schedule.values.yearLevel === student.values.year));
  }, [portal.students, todaySchedules]);

  async function save(studentId: string, status: string) {
    setSavingId(studentId);
    try { await portal.recordAttendance(studentId, status); }
    catch (reason) { Alert.alert('Attendance not saved', reason instanceof Error ? reason.message : 'Try again.'); }
    finally { setSavingId(''); }
  }

  return <PortalPage title="Class attendance" subtitle="Record attendance for students in your assigned class cohorts.">
    <View style={portalStyles.grid}><MetricCard icon="people-outline" label="Students today" value={roster.length}/><MetricCard icon="calendar-outline" label="Classes today" value={todaySchedules.length} tone="violet"/></View>
    <SectionHeading title="Today’s roster" detail={todayName}/>
    <View style={portalStyles.stack}>{roster.length ? roster.map(student => {
      const latest = portal.attendance.filter(item => item.values.studentId === student.id).sort((a, b) => b.values.date.localeCompare(a.values.date))[0];
      return <Card key={student.id}><Identity photo={student.values.photoDataUrl} name={student.values.name} detail={`${student.values.studentCode} · Year ${student.values.year || '—'}`} trailing={latest ? <StatusPill value={latest.values.status}/> : undefined}/><View style={styles.actions}>{attendanceOptions.map(status => <Pressable disabled={savingId === student.id} onPress={() => void save(student.id, status)} style={({ pressed }) => [styles.action, status === 'Present' ? styles.present : status === 'Late' ? styles.late : styles.absent, pressed && styles.pressed]} key={status}><Text style={[styles.actionText, status === 'Present' ? styles.presentText : status === 'Late' ? styles.lateText : styles.absentText]}>{savingId === student.id ? 'Saving…' : status}</Text></Pressable>)}</View></Card>;
    }) : <EmptyBlock icon="people-outline" title="No assigned students" detail="Students appear after Administrator completes Teacher, Student, Course, and Timetable Enrollment relationships."/>}</View>
  </PortalPage>;
}

function StudentAttendance() {
  const portal = usePortal();
  const ordered = [...portal.attendance].sort((a, b) => b.values.date.localeCompare(a.values.date));
  const count = (status: string) => ordered.filter(item => item.values.status === status).length;
  return <PortalPage title="My attendance" subtitle="Your personal attendance history is read-only and comes from institute records.">
    <View style={portalStyles.grid}><MetricCard icon="checkmark-circle-outline" label="Present" value={count('Present')} tone="green"/><MetricCard icon="time-outline" label="Late" value={count('Late')} tone="amber"/><MetricCard icon="close-circle-outline" label="Absent" value={count('Absent')} tone="violet"/><MetricCard icon="document-text-outline" label="Total records" value={ordered.length}/></View>
    <SectionHeading title="Attendance history" detail={`${ordered.length} records`}/>
    <View style={portalStyles.stack}>{ordered.length ? ordered.map(item => <Card key={item.id}><View style={styles.recordTop}><View><Text style={styles.recordDate}>{formatDate(item.values.date)}</Text><Text style={styles.recordCode}>{item.values.attendanceCode} · {item.values.term}</Text></View><StatusPill value={item.values.status}/></View><Text style={styles.recordMeta}>Check in {item.values.checkedInAt || 'not recorded'} · {item.values.method || 'Institute record'}</Text></Card>) : <EmptyBlock icon="document-outline" title="No attendance yet" detail="Attendance recorded by your Teacher will appear here."/>}</View>
  </PortalPage>;
}

function formatDate(value: string) {
  const date = new Date(`${value}T00:00:00`);
  return Number.isNaN(date.valueOf()) ? value : new Intl.DateTimeFormat('en-GB', { weekday: 'short', day: '2-digit', month: 'short', year: 'numeric' }).format(date);
}

const styles = StyleSheet.create({
  actions: { flexDirection: 'row', gap: 7, marginTop: 13 },
  action: { flex: 1, minHeight: 36, alignItems: 'center', justifyContent: 'center', borderRadius: radius.small, borderWidth: 1 },
  present: { backgroundColor: palette.greenPale, borderColor: '#BDE8D5' },
  late: { backgroundColor: palette.amberPale, borderColor: '#F3D998' },
  absent: { backgroundColor: palette.redPale, borderColor: '#F1C5CC' },
  actionText: { fontSize: 10, fontWeight: '900' },
  presentText: { color: palette.green },
  lateText: { color: palette.amber },
  absentText: { color: palette.red },
  pressed: { opacity: 0.65 },
  recordTop: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
  recordDate: { color: palette.ink, fontWeight: '900', fontSize: 13 },
  recordCode: { color: palette.blue, fontSize: 9, marginTop: 3 },
  recordMeta: { color: palette.muted, fontSize: 11, marginTop: 11 },
});
