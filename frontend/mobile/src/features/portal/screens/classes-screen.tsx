import Ionicons from '@expo/vector-icons/Ionicons';
import { useState } from 'react';
import { Alert, Pressable, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
import { Card, PortalPage, portalStyles, SectionHeading, StatusPill } from '@/components/portal-ui';
import { palette, radius } from '@/constants/theme';
import type { MobileRole } from '@/features/auth/auth-context';
import { AttendanceContent } from './attendance-screen';
import { ScheduleContent } from './schedule-screen';
import { usePortal } from '../portal-context';
import type { ScheduleItem } from '../portal-types';

type ClassesView = 'attendance' | 'schedule';

const views = [
  { id: 'attendance', label: 'Attendance', icon: 'checkmark-circle-outline' },
  { id: 'schedule', label: 'Schedule', icon: 'calendar-outline' },
] as const;

export function ClassesScreen({ role }: { role: MobileRole }) {
  const [view, setView] = useState<ClassesView>('attendance');
  const accent = palette.blue;

  return <PortalPage
    title="Classes"
    subtitle={role === 'teacher' ? 'Review your teaching schedule and read-only class attendance in one place.' : 'Review your weekly schedule and personal attendance history.'}
  >
    {role === 'student' ? <StudentClassActions/> : null}
    <View style={styles.switcher}>
      {views.map(item => {
        const active = view === item.id;
        return <Pressable
          accessibilityRole="tab"
          accessibilityState={{ selected: active }}
          key={item.id}
          onPress={() => setView(item.id)}
          style={[styles.switcherItem, active && styles.switcherItemActive]}
        >
          <Ionicons name={item.icon} size={17} color={active ? accent : palette.muted}/>
          <Text style={[styles.switcherLabel, active && styles.switcherLabelActive]}>{item.label}</Text>
        </Pressable>;
      })}
    </View>
    {view === 'schedule' ? <ScheduleContent/> : <AttendanceContent role={role}/>} 
  </PortalPage>;
}

function StudentClassActions() {
  const portal = usePortal();
  const [selectedDate, setSelectedDate] = useState(() => localDateKey(new Date()));
  const [reason, setReason] = useState('');
  const [sending, setSending] = useState(false);
  const now = new Date();
  const items = portal.schedule.map(item => ({ item, sessionDate: sessionDateFor(item, now) })).sort((left, right) => left.sessionDate.localeCompare(right.sessionDate) || left.item.values.startsAt.localeCompare(right.item.values.startsAt)).slice(0, 3);
  const dates = Array.from({ length: 7 }, (_, index) => { const date = new Date(now.getFullYear(), now.getMonth(), now.getDate() + index); return { key: localDateKey(date), label: index === 0 ? 'Today' : index === 1 ? 'Tomorrow' : new Intl.DateTimeFormat('en-GB', { weekday: 'short', day: '2-digit' }).format(date) }; });
  const request = portal.permissionRequests.find(item => item.sessionDate === selectedDate);

  async function submit() {
    if (!reason.trim()) { Alert.alert('Permission reason required', 'Tell your Teacher why you need permission for the selected day.'); return; }
    setSending(true);
    try { await portal.requestPermission(selectedDate, reason.trim()); setReason(''); }
    catch (cause) { Alert.alert('Permission not sent', cause instanceof Error ? cause.message : 'Try again.'); }
    finally { setSending(false); }
  }

  return <View style={portalStyles.stack}>
    <SectionHeading title="Whole-day permission" detail="Ask before or during the selected day"/>
    <Card>
      <Text style={styles.permissionTitle}>Choose the full day</Text>
      <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.permissionDates}>{dates.map(date => <Pressable key={date.key} onPress={() => { setSelectedDate(date.key); setReason(''); }} style={[styles.permissionDate, selectedDate === date.key && styles.permissionDateActive]}><Text style={[styles.permissionDateLabel, selectedDate === date.key && styles.permissionDateLabelActive]}>{date.label}</Text><Text style={[styles.permissionDateValue, selectedDate === date.key && styles.permissionDateLabelActive]}>{date.key.slice(5)}</Text></Pressable>)}</ScrollView>
      {request && request.status !== 'Rejected' ? <View style={styles.dayRequestState}><View><Text style={styles.permissionTitle}>{formatSessionDate(request.sessionDate)}</Text><Text style={styles.permissionDetail}>{request.reason}</Text></View><StatusPill value={request.status}/></View> : <View style={styles.permissionComposer}>{request?.status === 'Rejected' ? <Text style={styles.permissionRejected}>The Teacher rejected the earlier request. You may explain and ask again.</Text> : null}<TextInput value={reason} onChangeText={setReason} multiline placeholder="Reason for whole-day permission…" placeholderTextColor="#8996A8" style={styles.permissionInput}/><Pressable disabled={sending} onPress={() => void submit()} style={styles.permissionSend}><Text style={styles.permissionSendText}>{sending ? 'Sending…' : 'Send whole-day request'}</Text></Pressable></View>}
    </Card>
    <SectionHeading title="My next classes" detail="Current and upcoming classes"/>{items.map(({ item, sessionDate }) => {
    const running = sessionDate === localDateKey(now) && portal.startedScheduleIds.includes(item.id) && isWithin(item, now);
    return <Card key={`${item.id}-${sessionDate}`}><View style={styles.studentClassHeader}><View style={styles.studentClassCopy}><Text style={styles.studentClassCourse}>{item.values.course}</Text><Text style={styles.studentClassMeta}>{formatSessionDate(sessionDate)} · {item.values.startsAt}–{item.values.endsAt} · {item.values.classroom}</Text></View>{running ? <View style={styles.studyNow}><View style={styles.studyDot}/><Text style={styles.studyNowText}>Study now</Text></View> : null}</View></Card>;
  })}</View>;
}

function sessionDateFor(item: ScheduleItem, now: Date) {
  const names = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
  const target = names.indexOf(item.values.dayOfWeek);
  let offset = (target - now.getDay() + 7) % 7;
  const current = `${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}`;
  if (offset === 0 && current >= item.values.endsAt) offset = 7;
  const date = new Date(now.getFullYear(), now.getMonth(), now.getDate() + offset);
  return localDateKey(date);
}
function isWithin(item: ScheduleItem, now: Date) { const current = `${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}`; return current >= item.values.startsAt && current < item.values.endsAt; }
function localDateKey(value: Date) { return `${value.getFullYear()}-${String(value.getMonth() + 1).padStart(2, '0')}-${String(value.getDate()).padStart(2, '0')}`; }
function formatSessionDate(value: string) { const date = new Date(`${value}T00:00:00`); return new Intl.DateTimeFormat('en-GB', { weekday: 'short', day: '2-digit', month: 'short' }).format(date); }

const styles = StyleSheet.create({
  switcher: { flexDirection: 'row', padding: 4, borderWidth: 1, borderColor: palette.line, borderRadius: radius.medium, backgroundColor: '#FFFFFF' },
  switcherItem: { minHeight: 45, flex: 1, flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 8, borderRadius: radius.small },
  switcherItemActive: { backgroundColor: palette.bluePale },
  switcherLabel: { color: palette.muted, fontSize: 12, fontWeight: '700' },
  switcherLabelActive: { color: palette.blueDark, fontWeight: '800' },
  studentClassHeader: { flexDirection: 'row', alignItems: 'center', gap: 10 },
  studentClassCopy: { flex: 1 },
  studentClassCourse: { color: palette.ink, fontSize: 15, fontWeight: '800' },
  studentClassMeta: { color: palette.muted, fontSize: 10, marginTop: 5 },
  studyNow: { flexDirection: 'row', alignItems: 'center', gap: 6, paddingVertical: 7, paddingHorizontal: 9, borderRadius: radius.small, backgroundColor: '#E6F7EF' },
  studyDot: { width: 7, height: 7, borderRadius: 4, backgroundColor: palette.green },
  studyNowText: { color: '#22775A', fontSize: 11, fontWeight: '800' },
  permissionTitle: { color: palette.ink, fontSize: 13, fontWeight: '800' },
  permissionDates: { gap: 8, paddingVertical: 13 },
  permissionDate: { minWidth: 72, alignItems: 'center', gap: 4, paddingHorizontal: 9, paddingVertical: 9, borderWidth: 1, borderColor: palette.line, borderRadius: radius.small, backgroundColor: '#FFFFFF' },
  permissionDateActive: { borderColor: palette.blue, backgroundColor: palette.bluePale },
  permissionDateLabel: { color: palette.muted, fontSize: 10, fontWeight: '800' },
  permissionDateValue: { color: palette.ink, fontSize: 10, fontWeight: '700' },
  permissionDateLabelActive: { color: palette.blueDark },
  dayRequestState: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: 12, paddingTop: 4 },
  permissionRejected: { color: '#B74450', fontSize: 11, lineHeight: 17 },
  permissionDetail: { color: palette.muted, fontSize: 11, lineHeight: 17, marginTop: 12 },
  permissionComposer: { gap: 9, marginTop: 12 },
  permissionInput: { minHeight: 72, padding: 11, borderWidth: 1, borderColor: palette.line, borderRadius: radius.small, color: palette.ink, textAlignVertical: 'top' },
  permissionSend: { minHeight: 42, alignItems: 'center', justifyContent: 'center', paddingHorizontal: 14, borderRadius: radius.small, backgroundColor: palette.blue },
  permissionSendText: { color: '#FFFFFF', fontWeight: '800' },
});
