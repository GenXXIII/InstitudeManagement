import Ionicons from '@expo/vector-icons/Ionicons';
import { useState } from 'react';
import { Alert, Pressable, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
import { Card, EmptyBlock, PortalPage, portalStyles, SectionHeading, StatusPill } from '@/components/portal-ui';
import { palette, radius } from '@/constants/theme';
import type { MobileRole } from '@/features/auth/auth-context';
import { AttendanceContent } from './attendance-screen';
import { ScheduleContent } from './schedule-screen';
import { usePortal } from '../portal-context';
import type { ScheduleItem } from '../portal-types';

type ClassesView = 'overview' | 'control' | 'attendance' | 'schedule' | 'permission';

const teacherViews = [
  { id: 'control', label: 'Class control', icon: 'school-outline' },
  { id: 'schedule', label: 'Schedule', icon: 'calendar-outline' },
] as const;

const studentViews = [
  { id: 'overview', label: 'My classes', icon: 'book-outline' },
  { id: 'attendance', label: 'Attendance', icon: 'checkmark-circle-outline' },
  { id: 'schedule', label: 'Schedule', icon: 'calendar-outline' },
  { id: 'permission', label: 'Permission', icon: 'document-text-outline' },
] as const;

export function ClassesScreen({ role }: { role: MobileRole }) {
  const [view, setView] = useState<ClassesView>(() => role === 'teacher' ? 'control' : 'overview');
  const accent = palette.blue;
  const views = role === 'teacher' ? teacherViews : studentViews;

  return <PortalPage
    title="Classes"
    subtitle={role === 'teacher' ? 'Start today’s classes, review read-only attendance, and decide whole-day permission requests.' : 'See every class, check attendance, follow your schedule, and send guided permission requests.'}
  >
    <View style={styles.switcherFrame}>
      <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.switcher}>
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
      </ScrollView>
    </View>
    {view === 'overview' ? <StudentClassOverview/> : null}
    {view === 'permission' ? <StudentPermissionCenter/> : null}
    {view === 'schedule' ? <ScheduleContent/> : null}
    {view === 'attendance' || view === 'control' ? <AttendanceContent role={role}/> : null}
  </PortalPage>;
}

function StudentClassOverview() {
  const portal = usePortal();
  const now = new Date();
  const items = portal.schedule.map(item => ({ item, sessionDate: sessionDateFor(item, now) })).sort((left, right) => left.sessionDate.localeCompare(right.sessionDate) || left.item.values.startsAt.localeCompare(right.item.values.startsAt)).slice(0, 5);

  return <View style={portalStyles.stack}>
    <View style={styles.classWelcome}><View style={styles.classWelcomeIcon}><Ionicons name="sparkles" size={22} color={palette.gold}/></View><View style={styles.classWelcomeCopy}><Text style={styles.classWelcomeTitle}>Your learning week</Text><Text style={styles.classWelcomeText}>Open Attendance, Schedule, or Permission above whenever you need the complete class workflow.</Text></View></View>
    <SectionHeading title="Next classes" detail={`${items.length} upcoming`}/>
    {items.length ? items.map(({ item, sessionDate }) => {
      const running = sessionDate === localDateKey(now) && portal.startedScheduleIds.includes(item.id) && isWithin(item, now);
      return <Card key={`${item.id}-${sessionDate}`}><View style={styles.studentClassHeader}><View style={styles.classCourseIcon}><Ionicons name="book-outline" size={20} color={palette.blue}/></View><View style={styles.studentClassCopy}><Text style={styles.studentClassCourse}>{item.values.course}</Text><Text style={styles.studentClassMeta}>{formatSessionDate(sessionDate)} · {item.values.startsAt}–{item.values.endsAt}</Text><Text style={styles.studentClassPlace}>{item.values.classroom} · {item.values.teacher}</Text></View>{running ? <View style={styles.studyNow}><View style={styles.studyDot}/><Text style={styles.studyNowText}>Study now</Text></View> : null}</View></Card>;
    }) : <EmptyBlock icon="calendar-clear-outline" title="No upcoming classes" detail="Your classes appear after Administrator completes the current timetable enrollment."/>}
  </View>;
}

function StudentPermissionCenter() {
  const portal = usePortal();
  const [selectedDate, setSelectedDate] = useState(() => localDateKey(new Date()));
  const [reason, setReason] = useState('');
  const [reviewing, setReviewing] = useState(false);
  const [sending, setSending] = useState(false);
  const now = new Date();
  const dates = Array.from({ length: 7 }, (_, index) => {
    const date = new Date(now.getFullYear(), now.getMonth(), now.getDate() + index);
    return { key: localDateKey(date), label: index === 0 ? 'Today' : index === 1 ? 'Tomorrow' : new Intl.DateTimeFormat('en-GB', { weekday: 'short', day: '2-digit' }).format(date) };
  });
  const request = [...portal.permissionRequests]
    .filter(item => item.sessionDate === selectedDate)
    .sort((a, b) => b.requestedAtUtc.localeCompare(a.requestedAtUtc))[0];
  const selectedDay = new Intl.DateTimeFormat('en-US', { weekday: 'long' }).format(new Date(selectedDate + 'T00:00:00'));
  const affectedClasses = portal.schedule.filter(item => item.values.dayOfWeek === selectedDay).sort((a, b) => a.values.startsAt.localeCompare(b.values.startsAt));
  const history = [...portal.permissionRequests].sort((a, b) => b.requestedAtUtc.localeCompare(a.requestedAtUtc));
  const suggestions = ['Medical appointment', 'Family responsibility', 'Institute activity', 'Personal emergency'];

  async function submit() {
    if (!reason.trim()) {
      Alert.alert('Permission reason required', 'Tell your Teacher why you need permission for the selected day.');
      return;
    }
    setSending(true);
    try {
      await portal.requestPermission(selectedDate, reason.trim());
      setReason('');
      setReviewing(false);
    } catch (cause) {
      Alert.alert('Permission not sent', cause instanceof Error ? cause.message : 'Try again.');
    } finally {
      setSending(false);
    }
  }

  return <View style={portalStyles.stack}>
    <View style={styles.permissionHero}>
      <View style={styles.permissionHeroIcon}><Ionicons name="shield-checkmark-outline" size={25} color={palette.blue}/></View>
      <View style={styles.permissionHeroCopy}><Text style={styles.permissionHeroTitle}>Permission request center</Text><Text style={styles.permissionHeroText}>Choose a day, explain the reason, review the affected classes, then send one whole-day request to your Teacher.</Text></View>
    </View>
    <View style={styles.permissionSteps}>
      {['Choose day', 'Add reason', 'Review & send'].map((label, index) => <View style={styles.permissionStep} key={label}><View style={[styles.permissionStepNumber, (index === 0 || reason.trim() || reviewing) && styles.permissionStepActive]}><Text style={styles.permissionStepNumberText}>{index + 1}</Text></View><Text style={styles.permissionStepLabel}>{label}</Text></View>)}
    </View>

    <SectionHeading title="1. Choose the day" detail="One request covers every class that day"/>
    <Card>
      <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.permissionDates}>
        {dates.map(date => <Pressable key={date.key} onPress={() => { setSelectedDate(date.key); setReason(''); setReviewing(false); }} style={[styles.permissionDate, selectedDate === date.key && styles.permissionDateActive]}><Text style={[styles.permissionDateLabel, selectedDate === date.key && styles.permissionDateLabelActive]}>{date.label}</Text><Text style={[styles.permissionDateValue, selectedDate === date.key && styles.permissionDateLabelActive]}>{date.key.slice(5)}</Text></Pressable>)}
      </ScrollView>
      <View style={styles.affectedClasses}>
        <View style={styles.affectedHeader}><Text style={styles.permissionTitle}>{formatSessionDate(selectedDate)}</Text><Text style={styles.affectedCount}>{affectedClasses.length} {affectedClasses.length === 1 ? 'class' : 'classes'}</Text></View>
        {affectedClasses.length ? affectedClasses.map(item => <View style={styles.affectedRow} key={item.id}><Ionicons name="book-outline" size={16} color={palette.blue}/><Text style={styles.affectedCourse}>{item.values.course}</Text><Text style={styles.affectedTime}>{item.values.startsAt}</Text></View>) : <Text style={styles.permissionDetail}>No class is currently scheduled for this day. The request still covers the complete day.</Text>}
      </View>
    </Card>

    {request && request.status !== 'Rejected' ? <Card style={styles.requestLocked}>
      <View style={styles.dayRequestState}><View style={styles.requestStateCopy}><Text style={styles.permissionTitle}>Request already sent</Text><Text style={styles.permissionDetail}>{request.reason}</Text></View><StatusPill value={request.status}/></View>
      <Text style={styles.requestLockedHelp}>Your Teacher will review this request. The status updates here after refresh.</Text>
    </Card> : <>
      <SectionHeading title="2. Explain the reason" detail="Give your Teacher enough context"/>
      <Card>
        {request?.status === 'Rejected' ? <Text style={styles.permissionRejected}>The earlier request was rejected. You can update the explanation and ask again.</Text> : null}
        <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.reasonSuggestions}>
          {suggestions.map(item => <Pressable key={item} onPress={() => { setReason(item); setReviewing(false); }} style={[styles.reasonChip, reason === item && styles.reasonChipActive]}><Text style={[styles.reasonChipText, reason === item && styles.reasonChipTextActive]}>{item}</Text></Pressable>)}
        </ScrollView>
        <TextInput value={reason} onChangeText={value => { setReason(value); setReviewing(false); }} multiline placeholder="Add a clear reason for the whole-day request…" placeholderTextColor="#8996A8" style={styles.permissionInput}/>
      </Card>

      <SectionHeading title="3. Review and send" detail="Confirm before your Teacher receives it"/>
      <Card style={reviewing ? styles.reviewCardActive : undefined}>
        {reviewing ? <View style={styles.permissionReview}>
          <View style={styles.reviewRow}><Text>Date</Text><Text>{formatSessionDate(selectedDate)}</Text></View>
          <View style={styles.reviewRow}><Text>Classes</Text><Text>{affectedClasses.length || 'None scheduled'}</Text></View>
          <View style={styles.reviewReason}><Text>Reason</Text><Text>{reason.trim()}</Text></View>
          <View style={styles.permissionFinalActions}><Pressable disabled={sending} onPress={() => setReviewing(false)} style={styles.permissionBack}><Text style={styles.permissionBackText}>Edit request</Text></Pressable><Pressable disabled={sending} onPress={() => void submit()} style={styles.permissionSend}><Ionicons name="send" size={17} color="#FFFFFF"/><Text style={styles.permissionSendText}>{sending ? 'Sending…' : 'Send to Teacher'}</Text></Pressable></View>
        </View> : <View style={styles.reviewReady}><Ionicons name="document-text-outline" size={23} color={reason.trim() ? palette.blue : palette.muted}/><Text>{reason.trim() ? 'Your request is ready to review.' : 'Add a reason before reviewing the request.'}</Text><Pressable disabled={!reason.trim()} onPress={() => setReviewing(true)} style={[styles.permissionReviewButton, !reason.trim() && styles.permissionButtonDisabled]}><Text style={styles.permissionSendText}>Review request</Text></Pressable></View>}
      </Card>
    </>}

    <SectionHeading title="Request history" detail={history.length + ' total'}/>
    {history.length ? history.map(item => <Card key={item.id}><View style={styles.historyRequestTop}><View style={styles.requestStateCopy}><Text style={styles.permissionTitle}>{formatSessionDate(item.sessionDate)}</Text><Text style={styles.permissionDetail}>{item.reason}</Text></View><StatusPill value={item.status}/></View></Card>) : <EmptyBlock icon="document-outline" title="No permission requests" detail="Requests you send will remain here with their current Teacher decision."/>}
  </View>;
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
  switcherFrame: { overflow: 'hidden', borderWidth: 1, borderColor: palette.line, borderRadius: radius.large, backgroundColor: '#FFFFFF' },
  switcher: { flexGrow: 1, gap: 8, padding: 5 },
  switcherItem: { minWidth: 132, minHeight: 48, flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 8, paddingHorizontal: 14, borderRadius: radius.medium },
  switcherItemActive: { backgroundColor: palette.bluePale, borderWidth: 1, borderColor: '#CEDDFF' },
  switcherLabel: { color: palette.muted, fontSize: 13, fontWeight: '700' },
  switcherLabelActive: { color: palette.blueDark, fontWeight: '900' },
  classWelcome: { overflow: 'hidden', flexDirection: 'row', alignItems: 'center', gap: 14, padding: 18, borderRadius: radius.large, backgroundColor: palette.blueDark },
  classWelcomeIcon: { width: 48, height: 48, borderRadius: 16, alignItems: 'center', justifyContent: 'center', backgroundColor: 'rgba(255,255,255,0.14)' },
  classWelcomeCopy: { flex: 1 },
  classWelcomeTitle: { color: '#FFFFFF', fontSize: 19, fontWeight: '900' },
  classWelcomeText: { color: '#DCE5FF', fontSize: 13, lineHeight: 19, marginTop: 5 },
  classCourseIcon: { width: 46, height: 46, borderRadius: 15, alignItems: 'center', justifyContent: 'center', backgroundColor: palette.bluePale },
  studentClassHeader: { flexDirection: 'row', alignItems: 'center', gap: 12 },
  studentClassCopy: { flex: 1 },
  studentClassCourse: { color: palette.ink, fontSize: 17, fontWeight: '900' },
  studentClassMeta: { color: palette.blueDark, fontSize: 13, fontWeight: '700', marginTop: 5 },
  studentClassPlace: { color: palette.muted, fontSize: 13, marginTop: 4 },
  studyNow: { flexDirection: 'row', alignItems: 'center', gap: 6, paddingVertical: 7, paddingHorizontal: 10, borderRadius: radius.pill, backgroundColor: palette.greenPale },
  studyDot: { width: 7, height: 7, borderRadius: 4, backgroundColor: palette.green },
  studyNowText: { color: palette.green, fontSize: 12, fontWeight: '900' },
  permissionHero: { flexDirection: 'row', gap: 14, padding: 19, borderRadius: radius.large, backgroundColor: palette.skyPale, borderWidth: 1, borderColor: '#C8E9F8' },
  permissionHeroIcon: { width: 50, height: 50, borderRadius: 17, alignItems: 'center', justifyContent: 'center', backgroundColor: '#FFFFFF' },
  permissionHeroCopy: { flex: 1 },
  permissionHeroTitle: { color: palette.ink, fontSize: 19, fontWeight: '900' },
  permissionHeroText: { color: palette.muted, fontSize: 13, lineHeight: 20, marginTop: 5 },
  permissionSteps: { flexDirection: 'row', justifyContent: 'space-between', gap: 7, padding: 12, borderRadius: radius.medium, backgroundColor: '#FFFFFF', borderWidth: 1, borderColor: palette.line },
  permissionStep: { flex: 1, alignItems: 'center', gap: 7 },
  permissionStepNumber: { width: 29, height: 29, borderRadius: 15, alignItems: 'center', justifyContent: 'center', backgroundColor: '#EDF0F5' },
  permissionStepActive: { backgroundColor: palette.blue },
  permissionStepNumberText: { color: '#FFFFFF', fontSize: 12, fontWeight: '900' },
  permissionStepLabel: { color: palette.muted, fontSize: 11, fontWeight: '800', textAlign: 'center' },
  permissionTitle: { color: palette.ink, fontSize: 15, fontWeight: '900' },
  permissionDates: { gap: 9, paddingBottom: 4 },
  permissionDate: { minWidth: 76, alignItems: 'center', gap: 5, paddingHorizontal: 10, paddingVertical: 11, borderWidth: 1, borderColor: palette.line, borderRadius: radius.medium, backgroundColor: '#FFFFFF' },
  permissionDateActive: { borderColor: palette.blue, backgroundColor: palette.blue },
  permissionDateLabel: { color: palette.muted, fontSize: 12, fontWeight: '900' },
  permissionDateValue: { color: palette.ink, fontSize: 12, fontWeight: '800' },
  permissionDateLabelActive: { color: '#FFFFFF' },
  affectedClasses: { gap: 10, marginTop: 16, paddingTop: 15, borderTopWidth: StyleSheet.hairlineWidth, borderTopColor: palette.line },
  affectedHeader: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: 10 },
  affectedCount: { color: palette.blue, fontSize: 12, fontWeight: '900' },
  affectedRow: { minHeight: 42, flexDirection: 'row', alignItems: 'center', gap: 9, paddingHorizontal: 11, borderRadius: radius.small, backgroundColor: palette.canvas },
  affectedCourse: { flex: 1, color: palette.ink, fontSize: 13, fontWeight: '800' },
  affectedTime: { color: palette.blueDark, fontSize: 12, fontWeight: '900' },
  requestLocked: { backgroundColor: palette.goldPale, borderColor: '#EED16C' },
  dayRequestState: { flexDirection: 'row', alignItems: 'flex-start', justifyContent: 'space-between', gap: 12 },
  requestStateCopy: { flex: 1 },
  requestLockedHelp: { color: palette.amber, fontSize: 12, lineHeight: 18, marginTop: 13 },
  permissionRejected: { color: palette.red, fontSize: 13, lineHeight: 19, marginBottom: 12 },
  permissionDetail: { color: palette.muted, fontSize: 13, lineHeight: 19, marginTop: 5 },
  reasonSuggestions: { gap: 8, paddingBottom: 13 },
  reasonChip: { minHeight: 37, justifyContent: 'center', paddingHorizontal: 13, borderRadius: radius.pill, backgroundColor: palette.canvas, borderWidth: 1, borderColor: palette.line },
  reasonChipActive: { backgroundColor: palette.bluePale, borderColor: palette.blue },
  reasonChipText: { color: palette.muted, fontSize: 12, fontWeight: '800' },
  reasonChipTextActive: { color: palette.blueDark },
  permissionInput: { minHeight: 104, padding: 14, borderWidth: 1, borderColor: palette.line, borderRadius: radius.medium, backgroundColor: palette.canvas, color: palette.ink, fontSize: 15, lineHeight: 21, textAlignVertical: 'top' },
  reviewCardActive: { borderColor: '#A9C4FF', backgroundColor: '#F8FAFF' },
  permissionReview: { gap: 12 },
  reviewRow: { flexDirection: 'row', justifyContent: 'space-between', gap: 12, paddingBottom: 10, borderBottomWidth: StyleSheet.hairlineWidth, borderBottomColor: palette.line },
  reviewReason: { gap: 6, padding: 13, borderRadius: radius.small, backgroundColor: palette.bluePale },
  permissionFinalActions: { flexDirection: 'row', gap: 10, marginTop: 2 },
  permissionBack: { minHeight: 48, flex: 1, alignItems: 'center', justifyContent: 'center', borderRadius: radius.small, borderWidth: 1, borderColor: palette.line, backgroundColor: '#FFFFFF' },
  permissionBackText: { color: palette.ink, fontSize: 13, fontWeight: '900' },
  permissionSend: { minHeight: 48, flex: 1.35, flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 8, paddingHorizontal: 14, borderRadius: radius.small, backgroundColor: palette.blue },
  permissionSendText: { color: '#FFFFFF', fontSize: 13, fontWeight: '900' },
  reviewReady: { alignItems: 'center', gap: 12, paddingVertical: 7 },
  permissionReviewButton: { minHeight: 48, width: '100%', alignItems: 'center', justifyContent: 'center', borderRadius: radius.small, backgroundColor: palette.blue },
  permissionButtonDisabled: { opacity: 0.42 },
  historyRequestTop: { flexDirection: 'row', alignItems: 'flex-start', justifyContent: 'space-between', gap: 12 },
});
