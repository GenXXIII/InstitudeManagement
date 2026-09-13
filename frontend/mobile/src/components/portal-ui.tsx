import Ionicons from '@expo/vector-icons/Ionicons';
import type { PropsWithChildren, ReactNode } from 'react';
import { ActivityIndicator, Image, RefreshControl, ScrollView, StyleSheet, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { palette, radius, shadow } from '@/constants/theme';
import { usePortal } from '@/features/portal/portal-context';
import type { ScheduleItem } from '@/features/portal/portal-types';

export function PortalPage({ title, subtitle, children }: PropsWithChildren<{ title: string; subtitle: string }>) {
  const portal = usePortal();
  const values = portal.profile?.values;
  const code = values && ('teacherCode' in values ? values.teacherCode : values.studentCode);

  return <SafeAreaView style={styles.safeArea} edges={['top']}>
    <View style={styles.topbar}>
      <View style={styles.brandMark}><Text style={styles.brandText}>INK</Text></View>
      <View style={styles.topbarCopy}><Text style={styles.institute}>Institude of New Khmer</Text><Text style={styles.role}>{portal.role} app</Text></View>
      <View style={[styles.connectionDot, portal.error ? styles.connectionOffline : undefined]}/>
    </View>
    <ScrollView contentContainerStyle={styles.content} refreshControl={<RefreshControl refreshing={portal.loading} onRefresh={() => void portal.refresh()} tintColor={palette.blue}/>}>
      <View style={styles.heading}><Text style={styles.eyebrow}>{code || `${portal.role} workspace`}</Text><Text style={styles.title}>{title}</Text><Text style={styles.subtitle}>{subtitle}</Text></View>
      {portal.loading && !portal.profile ? <View style={styles.stateCard}><ActivityIndicator color={palette.blue}/><Text>Connecting to institute data…</Text></View> : null}
      {portal.error ? <View style={[styles.stateCard, styles.errorCard]}><Ionicons name="cloud-offline-outline" size={24} color={palette.red}/><Text style={styles.stateTitle}>Connection unavailable</Text><Text style={styles.stateCopy}>{portal.error}</Text></View> : null}
      {!portal.loading && !portal.error && !portal.profile ? <View style={styles.stateCard}><Ionicons name="person-add-outline" size={26} color={palette.blue}/><Text style={styles.stateTitle}>Profile not linked yet</Text><Text style={styles.stateCopy}>Ask Administrator to create an active {portal.role} profile whose email is {portal.role === 'teacher' ? 'teacher@gmail.com' : 'studnet@gmail.com'}. The mobile workspace will link automatically.</Text></View> : null}
      {portal.profile && !portal.error ? children : null}
    </ScrollView>
  </SafeAreaView>;
}

export function Card({ children, style }: PropsWithChildren<{ style?: object }>) {
  return <View style={[styles.card, style]}>{children}</View>;
}

export function SectionHeading({ title, detail }: { title: string; detail?: string }) {
  return <View style={styles.sectionHeading}><Text style={styles.sectionTitle}>{title}</Text>{detail ? <Text style={styles.sectionDetail}>{detail}</Text> : null}</View>;
}

export function MetricCard({ icon, label, value, tone = 'blue' }: { icon: keyof typeof Ionicons.glyphMap; label: string; value: string | number; tone?: 'blue' | 'green' | 'amber' | 'violet' }) {
  const toneColor = tone === 'green' ? palette.green : tone === 'amber' ? palette.amber : tone === 'violet' ? palette.violet : palette.blue;
  return <Card style={styles.metric}><View style={[styles.metricIcon, { backgroundColor: `${toneColor}16` }]}><Ionicons name={icon} size={18} color={toneColor}/></View><Text style={styles.metricValue}>{value}</Text><Text style={styles.metricLabel}>{label}</Text></Card>;
}

export function ScheduleCard({ item }: { item: ScheduleItem }) {
  const value = item.values;
  return <Card style={styles.scheduleCard}>
    <View style={styles.timeBlock}><Text style={styles.time}>{value.startsAt}</Text><Text style={styles.timeEnd}>{value.endsAt}</Text></View>
    <View style={styles.scheduleCopy}><View style={styles.scheduleTitleRow}><Text style={styles.scheduleTitle} numberOfLines={1}>{value.course}</Text><StatusPill value={value.status}/></View><Text style={styles.scheduleCode}>{value.courseCode} · {value.enrollmentCode}</Text><View style={styles.scheduleMeta}><Ionicons name="location-outline" size={13} color={palette.muted}/><Text>{value.classroom}{value.building ? ` · ${value.building}` : ''}</Text></View><View style={styles.scheduleMeta}><Ionicons name="person-outline" size={13} color={palette.muted}/><Text>{value.teacher} · Year {value.yearLevel}</Text></View></View>
  </Card>;
}

export function StatusPill({ value }: { value: string }) {
  const normalized = value.toLowerCase();
  const tone = normalized.includes('present') || normalized.includes('active') || normalized.includes('complete')
    ? { backgroundColor: palette.greenPale, color: palette.green }
    : normalized.includes('absent') || normalized.includes('cancel')
      ? { backgroundColor: palette.redPale, color: palette.red }
      : normalized.includes('late') || normalized.includes('permission')
        ? { backgroundColor: palette.amberPale, color: palette.amber }
        : { backgroundColor: palette.bluePale, color: palette.blue };
  return <Text style={[styles.pill, tone]}>{value || 'Current'}</Text>;
}

export function EmptyBlock({ icon, title, detail }: { icon: keyof typeof Ionicons.glyphMap; title: string; detail: string }) {
  return <Card style={styles.empty}><Ionicons name={icon} size={25} color={palette.muted}/><Text style={styles.stateTitle}>{title}</Text><Text style={styles.stateCopy}>{detail}</Text></Card>;
}

export function Identity({ photo, name, detail, trailing }: { photo?: string; name: string; detail: string; trailing?: ReactNode }) {
  const supportedPhoto = photo && (/^data:image\/(png|jpeg|webp);/i.test(photo) || /^https?:\/\//i.test(photo));
  return <View style={styles.identity}>{supportedPhoto ? <Image source={{ uri: photo }} style={styles.avatar}/> : <View style={styles.avatarFallback}><Text>{initials(name)}</Text></View>}<View style={styles.identityCopy}><Text style={styles.identityName}>{name}</Text><Text style={styles.identityDetail}>{detail}</Text></View>{trailing}</View>;
}

export function initials(value: string) {
  return value.split(/\s+/).filter(Boolean).slice(0, 2).map(part => part[0]).join('').toUpperCase() || 'INK';
}

export const portalStyles = StyleSheet.create({
  grid: { flexDirection: 'row', flexWrap: 'wrap', gap: 10 },
  half: { width: '48.5%' },
  stack: { gap: 10 },
  row: { flexDirection: 'row', alignItems: 'center' },
  grow: { flex: 1 },
  muted: { color: palette.muted, fontSize: 12, lineHeight: 18 },
});

const styles = StyleSheet.create({
  safeArea: { flex: 1, backgroundColor: palette.canvas },
  topbar: { height: 58, paddingHorizontal: 18, flexDirection: 'row', alignItems: 'center', borderBottomColor: palette.line, borderBottomWidth: StyleSheet.hairlineWidth, backgroundColor: 'rgba(255,255,255,0.96)' },
  brandMark: { width: 36, height: 36, borderRadius: 12, alignItems: 'center', justifyContent: 'center', backgroundColor: palette.blue },
  brandText: { color: 'white', fontWeight: '900', fontSize: 11, letterSpacing: 0.6 },
  topbarCopy: { flex: 1, marginLeft: 10 },
  institute: { color: palette.ink, fontSize: 12, fontWeight: '800' },
  role: { color: palette.muted, fontSize: 10, textTransform: 'capitalize', marginTop: 1 },
  connectionDot: { width: 9, height: 9, borderRadius: 9, backgroundColor: palette.green },
  connectionOffline: { backgroundColor: palette.red },
  content: { padding: 18, paddingBottom: 34, gap: 14 },
  heading: { paddingTop: 4, paddingBottom: 4 },
  eyebrow: { color: palette.blue, textTransform: 'uppercase', letterSpacing: 1.2, fontWeight: '800', fontSize: 10 },
  title: { color: palette.ink, fontSize: 28, fontWeight: '900', letterSpacing: -0.7, marginTop: 6 },
  subtitle: { color: palette.muted, fontSize: 13, lineHeight: 19, marginTop: 5 },
  card: { backgroundColor: palette.panel, borderRadius: radius.medium, borderWidth: 1, borderColor: palette.line, padding: 15, ...shadow },
  stateCard: { minHeight: 160, padding: 24, borderRadius: radius.large, borderWidth: 1, borderColor: palette.line, backgroundColor: palette.panel, alignItems: 'center', justifyContent: 'center', gap: 8 },
  errorCard: { backgroundColor: '#FFFAFB', borderColor: '#F5CED4' },
  stateTitle: { color: palette.ink, fontSize: 14, fontWeight: '800', textAlign: 'center' },
  stateCopy: { color: palette.muted, fontSize: 12, lineHeight: 18, textAlign: 'center' },
  sectionHeading: { flexDirection: 'row', alignItems: 'baseline', justifyContent: 'space-between', marginTop: 5 },
  sectionTitle: { color: palette.ink, fontSize: 16, fontWeight: '900' },
  sectionDetail: { color: palette.muted, fontSize: 10, fontWeight: '700' },
  metric: { width: '48.5%', minHeight: 124 },
  metricIcon: { width: 34, height: 34, alignItems: 'center', justifyContent: 'center', borderRadius: 11 },
  metricValue: { color: palette.ink, fontWeight: '900', fontSize: 24, marginTop: 11 },
  metricLabel: { color: palette.muted, fontSize: 11, marginTop: 2 },
  scheduleCard: { flexDirection: 'row', padding: 0, overflow: 'hidden' },
  timeBlock: { width: 69, paddingVertical: 17, alignItems: 'center', backgroundColor: palette.bluePale, borderRightWidth: 1, borderRightColor: palette.line },
  time: { color: palette.blueDark, fontWeight: '900', fontSize: 14 },
  timeEnd: { color: palette.muted, fontSize: 10, marginTop: 5 },
  scheduleCopy: { flex: 1, padding: 14 },
  scheduleTitleRow: { flexDirection: 'row', alignItems: 'center', gap: 7 },
  scheduleTitle: { flex: 1, color: palette.ink, fontSize: 14, fontWeight: '900' },
  scheduleCode: { color: palette.blue, fontSize: 10, fontWeight: '700', marginTop: 3 },
  scheduleMeta: { flexDirection: 'row', alignItems: 'center', gap: 4, marginTop: 7 },
  pill: { overflow: 'hidden', borderRadius: radius.pill, paddingHorizontal: 8, paddingVertical: 4, textTransform: 'capitalize', fontSize: 9, fontWeight: '800' },
  empty: { minHeight: 130, alignItems: 'center', justifyContent: 'center', gap: 7 },
  identity: { flexDirection: 'row', alignItems: 'center', gap: 11 },
  avatar: { width: 44, height: 58, borderRadius: 11, backgroundColor: palette.bluePale },
  avatarFallback: { width: 44, height: 44, borderRadius: 14, alignItems: 'center', justifyContent: 'center', backgroundColor: palette.bluePale },
  identityCopy: { flex: 1 },
  identityName: { color: palette.ink, fontSize: 13, fontWeight: '800' },
  identityDetail: { color: palette.muted, fontSize: 10, marginTop: 3 },
});
