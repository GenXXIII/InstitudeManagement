import { StyleSheet, Text, View } from 'react-native';
import { EmptyBlock, PortalPage, portalStyles, ScheduleCard, SectionHeading } from '@/components/portal-ui';
import type { MobileRole } from '@/features/auth/auth-context';
import { palette } from '@/constants/theme';
import { usePortal } from '../portal-context';

const days = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];

export function ScheduleScreen({ role }: { role: MobileRole }) {
  return <PortalPage title="Weekly schedule" subtitle={role === 'teacher' ? 'Only classes assigned to this Teacher account are shown.' : 'Only classes matching this Student department and year are shown.'}>
    <ScheduleContent/>
  </PortalPage>;
}

export function ScheduleContent() {
  const portal = usePortal();
  const grouped = days.map(day => ({ day, items: portal.schedule.filter(item => item.values.dayOfWeek === day).sort((a, b) => a.values.startsAt.localeCompare(b.values.startsAt)) })).filter(group => group.items.length);
  return <>
    {grouped.length ? grouped.map(group => <View style={portalStyles.stack} key={group.day}><SectionHeading title={group.day} detail={`${group.items.length} ${group.items.length === 1 ? 'class' : 'classes'}`}/>{group.items.map(item => <ScheduleCard item={item} key={item.id}/>)}</View>) : <EmptyBlock icon="calendar-outline" title="No assigned schedule" detail="Administrator can add the teaching time in Management → Schedule, then connect the class in Timetable Enrollment."/>}
    <View style={styles.note}><Text style={styles.noteTitle}>Teaching-period time</Text><Text style={styles.noteCopy}>Starting and ending times come directly from Administrator Schedule Management.</Text></View>
  </>;
}

const styles = StyleSheet.create({
  note: { padding: 16, borderWidth: 1, borderLeftWidth: 4, borderColor: palette.line, borderLeftColor: palette.blue, borderRadius: 12, backgroundColor: '#FFFFFF' },
  noteTitle: { color: palette.ink, fontSize: 14, fontWeight: '800' },
  noteCopy: { color: palette.muted, fontSize: 12, lineHeight: 19, marginTop: 4 },
});
