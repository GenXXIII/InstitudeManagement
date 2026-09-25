import Ionicons from '@expo/vector-icons/Ionicons';
import { ScrollView, StyleSheet, Text, View } from 'react-native';
import { EmptyBlock, PortalPage, portalStyles, ScheduleCard, SectionHeading } from '@/components/portal-ui';
import { palette, radius, shadow } from '@/constants/theme';
import type { MobileRole } from '@/features/auth/auth-context';
import type { ScheduleItem } from '../portal-types';
import { usePortal } from '../portal-context';

export function HomeScreen({ role }: { role: MobileRole }) {
  const portal = usePortal();
  const values = portal.profile?.values;
  const today = new Intl.DateTimeFormat('en-US', { weekday: 'long' }).format(new Date());
  const todaySchedule = portal.schedule.filter(item => item.values.dayOfWeek === today).sort((a, b) => a.values.startsAt.localeCompare(b.values.startsAt));
  const courses = [...new Map(portal.schedule.filter(item => item.values.courseId).map(item => [item.values.courseId, item])).values()];

  return <PortalPage title={values ? `Hello, ${values.name.split(' ')[0]}` : 'Home'} subtitle={role === 'teacher' ? `Your teaching day at a glance · ${today}` : `Your academic day at a glance · ${today}`}>
    <NextClassHero item={todaySchedule[0]} role={role}/>

    <SectionHeading title="Ongoing courses" detail={`${courses.length} assigned`}/>
    {courses.length ? <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.courseStrip}>{courses.map((item, index) => <CourseOverviewCard item={item} index={index} totalSessions={portal.schedule.filter(schedule => schedule.values.courseId === item.values.courseId).length} key={item.values.courseId}/>)}</ScrollView> : <EmptyBlock icon="book-outline" title="No assigned courses" detail="Courses will appear after Administrator completes Timetable Enrollment."/>}

    <SectionHeading title="Today’s schedule" detail={`${todaySchedule.length} ${todaySchedule.length === 1 ? 'class' : 'classes'}`}/>
    <View style={portalStyles.stack}>{todaySchedule.length ? todaySchedule.map(item => <ScheduleCard item={item} key={item.id}/>) : <EmptyBlock icon="calendar-clear-outline" title="No class today" detail="Pull down to refresh after Administrator updates Timetable Enrollment."/>}</View>

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

const styles = StyleSheet.create({
  hero: { minHeight: 230, overflow: 'hidden', padding: 22, borderRadius: radius.large, borderWidth: 1, borderColor: '#CFE0FF', backgroundColor: '#EAF2FF', ...shadow },
  heroShapeLarge: { position: 'absolute', width: 250, height: 250, borderRadius: 125, right: -88, top: -94, backgroundColor: '#BFD8FF' },
  heroShapeSmall: { position: 'absolute', width: 158, height: 158, borderRadius: 79, right: 34, bottom: -108, backgroundColor: '#C9F0FF' },
  heroGoldLine: { position: 'absolute', width: 130, height: 7, right: 20, top: 0, borderBottomLeftRadius: 7, borderBottomRightRadius: 7, backgroundColor: palette.gold },
  heroHeader: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
  heroLabel: { flexDirection: 'row', alignItems: 'center', gap: 7 },
  liveDot: { width: 8, height: 8, borderRadius: 4, backgroundColor: palette.gold },
  heroLabelText: { color: palette.blueDark, fontSize: 11, fontWeight: '900', letterSpacing: 1.2 },
  heroDay: { color: palette.blueDark, fontSize: 13, fontWeight: '800' },
  heroTitle: { maxWidth: '82%', color: palette.ink, fontSize: 29, lineHeight: 34, fontWeight: '900', letterSpacing: -0.8, marginTop: 27 },
  heroSubtitle: { maxWidth: '78%', color: palette.muted, fontSize: 14, lineHeight: 20, marginTop: 8 },
  heroFooter: { flexDirection: 'row', alignItems: 'flex-end', justifyContent: 'space-between', marginTop: 26 },
  heroTimeLabel: { color: palette.muted, fontSize: 10, fontWeight: '900', letterSpacing: 0.9 },
  heroTime: { color: palette.ink, fontSize: 20, fontWeight: '900', marginTop: 3 },
  heroRoom: { flexDirection: 'row', alignItems: 'center', gap: 6, paddingHorizontal: 12, paddingVertical: 8, borderRadius: radius.pill, backgroundColor: palette.blue },
  heroRoomText: { color: '#FFFFFF', fontSize: 12, fontWeight: '900' },
  courseStrip: { gap: 12, paddingRight: 18, paddingBottom: 5 },
  courseCard: { width: 235, minHeight: 205, overflow: 'hidden', padding: 18, borderRadius: radius.large, backgroundColor: palette.panel, borderWidth: 1, borderColor: palette.line, ...shadow },
  courseAccent: { position: 'absolute', left: 0, right: 0, bottom: 0, height: 5 },
  courseIcon: { width: 42, height: 42, borderRadius: 11, alignItems: 'center', justifyContent: 'center' },
  courseCode: { color: palette.blue, fontSize: 12, fontWeight: '900', letterSpacing: 0.7, marginTop: 15 },
  courseTitle: { minHeight: 50, color: palette.ink, fontSize: 20, lineHeight: 25, fontWeight: '900', letterSpacing: -0.4, marginTop: 5 },
  courseTeacher: { color: palette.muted, fontSize: 14, marginTop: 5 },
  courseFooter: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginTop: 17, paddingTop: 11, borderTopWidth: StyleSheet.hairlineWidth, borderTopColor: palette.line },
  courseCount: { color: palette.muted, fontSize: 12, fontWeight: '700' },
  courseYear: { color: palette.blueDark, fontSize: 12, fontWeight: '900' },
});
