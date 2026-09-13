import Ionicons from '@expo/vector-icons/Ionicons';
import { StyleSheet, Text, View } from 'react-native';
import { Card, EmptyBlock, Identity, MetricCard, PortalPage, portalStyles, ScheduleCard, SectionHeading, StatusPill } from '@/components/portal-ui';
import { palette, radius } from '@/constants/theme';
import type { MobileRole } from '@/features/auth/auth-context';
import { usePortal } from '../portal-context';

export function HomeScreen({ role }: { role: MobileRole }) {
  const portal = usePortal();
  const values = portal.profile?.values;
  const today = new Intl.DateTimeFormat('en-US', { weekday: 'long' }).format(new Date());
  const todaySchedule = portal.schedule.filter(item => item.values.dayOfWeek === today).sort((a, b) => a.values.startsAt.localeCompare(b.values.startsAt));
  const present = portal.attendance.filter(item => ['Present', 'Late'].includes(item.values.status)).length;
  const attendanceRate = portal.attendance.length ? `${Math.round(present / portal.attendance.length * 100)}%` : '—';
  const scores = portal.grades.map(item => Number(item.values.score)).filter(Number.isFinite);
  const average = scores.length ? (scores.reduce((total, score) => total + score, 0) / scores.length).toFixed(1) : '—';
  const courses = new Set(portal.schedule.map(item => item.values.courseId).filter(Boolean)).size;

  return <PortalPage title={values ? `Hello, ${values.name.split(' ')[0]}` : 'Home'} subtitle={role === 'teacher' ? `Your classes, students, and teaching work for ${today}.` : `Your classes, attendance, and results for ${today}.`}>
    {values ? <Card><Identity photo={values.photoDataUrl} name={values.name} detail={`${values.department || 'Department pending'} · ${values.status}`} trailing={<StatusPill value={values.status}/>} /></Card> : null}
    <View style={portalStyles.grid}>
      {role === 'teacher' ? <><MetricCard icon="calendar-outline" label="Classes today" value={todaySchedule.length}/><MetricCard icon="book-outline" label="Assigned courses" value={courses} tone="violet"/><MetricCard icon="people-outline" label="My students" value={portal.students.length} tone="green"/><MetricCard icon="checkmark-done-outline" label="Attendance records" value={portal.attendance.length} tone="amber"/></> : <><MetricCard icon="calendar-outline" label="Classes today" value={todaySchedule.length}/><MetricCard icon="checkmark-circle-outline" label="Attendance" value={attendanceRate} tone="green"/><MetricCard icon="ribbon-outline" label="Average score" value={average} tone="violet"/><MetricCard icon="book-outline" label="Courses" value={courses} tone="amber"/></>}
    </View>
    <SectionHeading title="Today’s schedule" detail={`${todaySchedule.length} classes`}/>
    <View style={portalStyles.stack}>{todaySchedule.length ? todaySchedule.map(item => <ScheduleCard item={item} key={item.id}/>) : <EmptyBlock icon="calendar-clear-outline" title="No class today" detail="Pull down to refresh after Administrator updates Timetable Enrollment."/>}</View>
    <SectionHeading title="Latest announcements" detail={`${portal.announcements.length} total`}/>
    <View style={portalStyles.stack}>{portal.announcements.length ? portal.announcements.slice(0, 3).map(item => <Card key={item.id}><View style={styles.announcementTop}><View style={styles.announcementIcon}><Ionicons name={item.type === 'Emergency' ? 'warning-outline' : 'notifications-outline'} color={item.type === 'Emergency' ? palette.red : palette.blue} size={17}/></View><View style={styles.announcementCopy}><Text style={styles.announcementTitle}>{item.title}</Text><Text style={styles.announcementCode}>{item.announcementCode} · {formatDate(item.createAt)}</Text></View></View><Text style={styles.announcementMessage}>{item.message}</Text></Card>) : <EmptyBlock icon="notifications-off-outline" title="No announcements" detail="Administrator announcements will appear here."/>}</View>
  </PortalPage>;
}

function formatDate(value: string) {
  const date = new Date(value);
  return Number.isNaN(date.valueOf()) ? value : new Intl.DateTimeFormat('en-GB', { day: '2-digit', month: 'short', year: 'numeric' }).format(date);
}

const styles = StyleSheet.create({
  announcementTop: { flexDirection: 'row', alignItems: 'center', gap: 10 },
  announcementIcon: { width: 32, height: 32, borderRadius: radius.small, backgroundColor: '#FFFFFF', borderWidth: 1, borderColor: palette.line, alignItems: 'center', justifyContent: 'center' },
  announcementCopy: { flex: 1 },
  announcementTitle: { color: palette.ink, fontSize: 13, fontWeight: '700' },
  announcementCode: { color: palette.muted, fontSize: 9, marginTop: 2 },
  announcementMessage: { color: palette.muted, fontSize: 12, lineHeight: 18, marginTop: 10 },
});
