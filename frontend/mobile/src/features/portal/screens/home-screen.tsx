import Ionicons from '@expo/vector-icons/Ionicons';
import { useRouter } from 'expo-router';
import { Image, ScrollView, StyleSheet, Text, View } from 'react-native';
import { Card, EmptyBlock, MetricCard, PortalPage, portalStyles, ScheduleCard, SectionHeading } from '@/components/portal-ui';
import { palette, radius, shadow } from '@/constants/theme';
import type { MobileRole } from '@/features/auth/auth-context';
import type { Announcement, ScheduleItem } from '../portal-types';
import { usePortal } from '../portal-context';

export function HomeScreen({ role }: { role: MobileRole }) {
  const router = useRouter();
  const portal = usePortal();
  const values = portal.profile?.values;
  const today = new Intl.DateTimeFormat('en-US', { weekday: 'long' }).format(new Date());
  const todaySchedule = portal.schedule.filter(item => item.values.dayOfWeek === today).sort((a, b) => a.values.startsAt.localeCompare(b.values.startsAt));
  const present = portal.attendance.filter(item => ['Present', 'Late'].includes(item.values.status)).length;
  const attendanceRate = portal.attendance.length ? `${Math.round(present / portal.attendance.length * 100)}%` : '—';
  const scores = portal.grades.map(item => Number(item.values.score)).filter(Number.isFinite);
  const average = scores.length ? (scores.reduce((total, score) => total + score, 0) / scores.length).toFixed(1) : '—';
  const courses = [...new Map(portal.schedule.filter(item => item.values.courseId).map(item => [item.values.courseId, item])).values()];
  const notificationsRoute = role === 'teacher' ? '/(teacher)/notifications' : '/(student)/notifications';

  return <PortalPage title={values ? `Hello, ${values.name.split(' ')[0]}` : 'Home'} subtitle={role === 'teacher' ? `Your teaching day at a glance · ${today}` : `Your academic day at a glance · ${today}`}>
    <NextClassHero item={todaySchedule[0]} role={role}/>

    <SectionHeading title="Overview" detail="Current activity"/>
    <View style={portalStyles.grid}>
      {role === 'teacher' ? <><MetricCard icon="calendar-outline" label="Classes today" value={todaySchedule.length}/><MetricCard icon="book-outline" label="Assigned courses" value={courses.length} tone="violet"/><MetricCard icon="people-outline" label="My students" value={portal.students.length} tone="green"/><MetricCard icon="checkmark-done-outline" label="Attendance records" value={portal.attendance.length} tone="amber"/></> : <><MetricCard icon="calendar-outline" label="Classes today" value={todaySchedule.length}/><MetricCard icon="checkmark-circle-outline" label="Attendance" value={attendanceRate} tone="green"/><MetricCard icon="ribbon-outline" label="Average score" value={average} tone="violet"/><MetricCard icon="book-outline" label="Courses" value={courses.length} tone="amber"/></>}
    </View>

    <SectionHeading title="Ongoing courses" detail={`${courses.length} assigned`}/>
    {courses.length ? <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.courseStrip}>{courses.map((item, index) => <CourseOverviewCard item={item} index={index} totalSessions={portal.schedule.filter(schedule => schedule.values.courseId === item.values.courseId).length} key={item.values.courseId}/>)}</ScrollView> : <EmptyBlock icon="book-outline" title="No assigned courses" detail="Courses will appear after Administrator completes Timetable Enrollment."/>}

    <SectionHeading title="Today’s schedule" detail={`${todaySchedule.length} ${todaySchedule.length === 1 ? 'class' : 'classes'}`}/>
    <View style={portalStyles.stack}>{todaySchedule.length ? todaySchedule.map(item => <ScheduleCard item={item} key={item.id}/>) : <EmptyBlock icon="calendar-clear-outline" title="No class today" detail="Pull down to refresh after Administrator updates Timetable Enrollment."/>}</View>

    <SectionHeading title="Latest notifications" detail={`${portal.announcements.length} total`} actionLabel="View all" onAction={() => router.push(notificationsRoute)}/>
    {portal.announcements.length ? <Card style={styles.announcementPanel}>{portal.announcements.slice(0, 3).map((item, index) => <AnnouncementRow item={item} divided={index > 0} key={item.id}/>)}</Card> : <EmptyBlock icon="notifications-off-outline" title="No notifications" detail="Administrator notifications will appear here."/>}
  </PortalPage>;
}

function NextClassHero({ item, role }: { item?: ScheduleItem; role: MobileRole }) {
  return <View style={styles.hero}>
    <View style={styles.heroShapeLarge}/><View style={styles.heroShapeSmall}/><View style={styles.heroGoldLine}/>
    <View style={styles.heroHeader}><View style={styles.heroLabel}><View style={styles.liveDot}/><Text style={styles.heroLabelText}>{item ? 'NEXT CLASS' : 'TODAY'}</Text></View>{item ? <Text style={styles.heroDay}>{item.values.dayOfWeek}</Text> : null}</View>
    <Text style={styles.heroTitle} numberOfLines={2}>{item?.values.course || 'No class scheduled today'}</Text>
    <Text style={styles.heroSubtitle} numberOfLines={1}>{item ? (role === 'teacher' ? `${item.values.classroom} · Year ${item.values.yearLevel}` : item.values.teacher) : 'Your day is clear. Check the weekly schedule for upcoming classes.'}</Text>
    {item ? <View style={styles.heroFooter}><View><Text style={styles.heroTimeLabel}>CLASS TIME</Text><Text style={styles.heroTime}>{item.values.startsAt} – {item.values.endsAt}</Text></View><View style={styles.heroRoom}><Ionicons name="location-outline" size={15} color="#FFFFFF"/><Text style={styles.heroRoomText}>{item.values.classroom}</Text></View></View> : null}
  </View>;
}

function CourseOverviewCard({ item, index, totalSessions }: { item: ScheduleItem; index: number; totalSessions: number }) {
  const tones = [
    { icon: palette.blue, pale: palette.bluePale, accent: palette.blue },
    { icon: palette.sky, pale: palette.skyPale, accent: palette.sky },
    { icon: palette.gold, pale: palette.goldPale, accent: palette.gold },
  ];
  const tone = tones[index % tones.length];
  return <View style={styles.courseCard}>
    <View style={[styles.courseAccent, { backgroundColor: tone.accent }]}/>
    <View style={[styles.courseIcon, { backgroundColor: tone.pale }]}><Ionicons name="book-outline" size={20} color={tone.icon}/></View>
    <Text style={styles.courseCode}>{item.values.courseCode}</Text>
    <Text style={styles.courseTitle} numberOfLines={2}>{item.values.course}</Text>
    <Text style={styles.courseTeacher} numberOfLines={1}>{item.values.teacher}</Text>
    <View style={styles.courseFooter}><Text style={styles.courseCount}>{totalSessions} weekly {totalSessions === 1 ? 'class' : 'classes'}</Text><Text style={styles.courseYear}>Year {item.values.yearLevel}</Text></View>
  </View>;
}

function AnnouncementRow({ item, divided }: { item: Announcement; divided: boolean }) {
  return <View style={[styles.announcementRow, divided && styles.announcementDivided]}>
    <View style={styles.announcementIcon}><Image source={require('../../../../assets/images/ink-logo.png')} style={styles.announcementLogo} resizeMode="contain"/></View>
    <View style={styles.announcementCopy}><View style={styles.announcementHeading}><Text style={styles.announcementType}>{item.type}</Text><Text style={styles.announcementDate}>{formatDate(item.createAt)}</Text></View><Text style={styles.announcementTitle} numberOfLines={1}>{item.title}</Text><Text style={styles.announcementMessage} numberOfLines={2}>{item.message}</Text></View>
  </View>;
}

function formatDate(value: string) {
  const date = new Date(value);
  return Number.isNaN(date.valueOf()) ? value : new Intl.DateTimeFormat('en-GB', { day: '2-digit', month: 'short' }).format(date);
}

const styles = StyleSheet.create({
  hero: { minHeight: 220, overflow: 'hidden', padding: 22, borderRadius: radius.large, backgroundColor: palette.blueDark, ...shadow },
  heroShapeLarge: { position: 'absolute', width: 240, height: 240, borderRadius: 120, right: -82, top: -90, backgroundColor: '#2F49B9' },
  heroShapeSmall: { position: 'absolute', width: 150, height: 150, borderRadius: 75, right: 38, bottom: -104, backgroundColor: '#326FAE' },
  heroGoldLine: { position: 'absolute', width: 120, height: 5, right: 20, top: 0, backgroundColor: '#D8B335' },
  heroHeader: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
  heroLabel: { flexDirection: 'row', alignItems: 'center', gap: 7 },
  liveDot: { width: 7, height: 7, borderRadius: 4, backgroundColor: '#E0BC3D' },
  heroLabelText: { color: '#DDE3FF', fontSize: 10, fontWeight: '800', letterSpacing: 1.1 },
  heroDay: { color: '#DDE3FF', fontSize: 11, fontWeight: '700' },
  heroTitle: { maxWidth: '82%', color: '#FFFFFF', fontSize: 27, lineHeight: 32, fontWeight: '800', letterSpacing: -0.6, marginTop: 27 },
  heroSubtitle: { maxWidth: '78%', color: '#C8D1F3', fontSize: 13, marginTop: 7 },
  heroFooter: { flexDirection: 'row', alignItems: 'flex-end', justifyContent: 'space-between', marginTop: 26 },
  heroTimeLabel: { color: '#AEB9E7', fontSize: 9, fontWeight: '800', letterSpacing: 0.8 },
  heroTime: { color: '#FFFFFF', fontSize: 19, fontWeight: '800', marginTop: 3 },
  heroRoom: { flexDirection: 'row', alignItems: 'center', gap: 5, paddingHorizontal: 10, paddingVertical: 7, borderRadius: radius.pill, backgroundColor: 'rgba(255,255,255,0.14)' },
  heroRoomText: { color: '#FFFFFF', fontSize: 11, fontWeight: '700' },
  courseStrip: { gap: 12, paddingRight: 18, paddingBottom: 5 },
  courseCard: { width: 220, minHeight: 196, overflow: 'hidden', padding: 17, borderRadius: radius.medium, backgroundColor: palette.panel, borderWidth: 1, borderColor: palette.line, ...shadow },
  courseAccent: { position: 'absolute', left: 0, right: 0, bottom: 0, height: 5 },
  courseIcon: { width: 42, height: 42, borderRadius: 11, alignItems: 'center', justifyContent: 'center' },
  courseCode: { color: palette.blue, fontSize: 10, fontWeight: '800', letterSpacing: 0.6, marginTop: 15 },
  courseTitle: { minHeight: 48, color: palette.ink, fontSize: 19, lineHeight: 23, fontWeight: '800', letterSpacing: -0.3, marginTop: 4 },
  courseTeacher: { color: palette.muted, fontSize: 12, marginTop: 4 },
  courseFooter: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginTop: 17, paddingTop: 11, borderTopWidth: StyleSheet.hairlineWidth, borderTopColor: palette.line },
  courseCount: { color: palette.muted, fontSize: 10, fontWeight: '600' },
  courseYear: { color: palette.blueDark, fontSize: 10, fontWeight: '800' },
  announcementPanel: { paddingVertical: 2, shadowOpacity: 0, elevation: 0 },
  announcementRow: { minHeight: 96, flexDirection: 'row', alignItems: 'flex-start', gap: 12, paddingVertical: 15 },
  announcementDivided: { borderTopWidth: StyleSheet.hairlineWidth, borderTopColor: palette.line },
  announcementIcon: { width: 38, height: 42, alignItems: 'center', justifyContent: 'center' },
  announcementLogo: { width: 38, height: 42 },
  announcementCopy: { flex: 1 },
  announcementHeading: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: 8 },
  announcementType: { color: palette.blue, fontSize: 11, fontWeight: '800', textTransform: 'uppercase', letterSpacing: 0.5 },
  announcementDate: { color: palette.muted, fontSize: 10, fontWeight: '600' },
  announcementTitle: { color: palette.ink, fontSize: 14, fontWeight: '800', marginTop: 5 },
  announcementMessage: { color: palette.muted, fontSize: 12, lineHeight: 18, marginTop: 4 },
});
