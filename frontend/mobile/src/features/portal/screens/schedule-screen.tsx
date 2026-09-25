import Ionicons from '@expo/vector-icons/Ionicons';
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
    <View style={styles.note}>
      <View style={styles.noteIcon}><Ionicons name="time-outline" size={22} color={palette.blue}/></View>
      <View style={styles.noteContent}><Text style={styles.noteTitle}>Always on institute time</Text><Text style={styles.noteCopy}>Class start and end times stay synchronized with the official Administrator schedule.</Text></View>
    </View>
  </>;
}

const styles = StyleSheet.create({
  note: { flexDirection: 'row', alignItems: 'center', gap: 13, padding: 17, borderWidth: 1, borderColor: '#C8E9F8', borderRadius: 22, backgroundColor: palette.skyPale },
  noteIcon: { width: 46, height: 46, alignItems: 'center', justifyContent: 'center', borderRadius: 16, backgroundColor: '#FFFFFF' },
  noteContent: { flex: 1 },
  noteTitle: { color: palette.ink, fontSize: 16, fontWeight: '900' },
  noteCopy: { color: palette.muted, fontSize: 13, lineHeight: 20, marginTop: 4 },
});
