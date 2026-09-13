import Ionicons from '@expo/vector-icons/Ionicons';
import type { PropsWithChildren, ReactNode } from 'react';
import { ActivityIndicator, Image, Pressable, RefreshControl, ScrollView, StyleSheet, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { palette, radius, shadow } from '@/constants/theme';
import { usePortal } from '@/features/portal/portal-context';
import type { ScheduleItem } from '@/features/portal/portal-types';

export function PortalPage({ title, subtitle, children, showChildrenWhenUnavailable = false }: PropsWithChildren<{ title: string; subtitle: string; showChildrenWhenUnavailable?: boolean }>) {
  const portal = usePortal();
  const values = portal.profile?.values;
  const code = values && ('teacherCode' in values ? values.teacherCode : values.studentCode);
  const accent = palette.blue;

  return <SafeAreaView style={styles.safeArea} edges={['top']}>
    <View style={styles.topbar}>
      <View style={styles.brandMark}><Image source={require('../../assets/images/ink-logo.png')} style={styles.brandLogo} resizeMode="contain"/></View>
      <View style={styles.topbarCopy}><Text style={styles.institute}>Institute of New Khmer</Text><Text style={styles.role}>{portal.role} app</Text></View>
      <View style={styles.roleBadge}><Ionicons name={portal.role === 'teacher' ? 'school-outline' : 'person-outline'} size={13} color={accent}/><Text style={styles.roleBadgeText}>{portal.role === 'teacher' ? 'Teacher' : 'Student'}</Text></View>
      <View style={[styles.connectionDot, portal.error ? styles.connectionOffline : undefined]}/>
    </View>
    <ScrollView contentContainerStyle={styles.content} refreshControl={<RefreshControl refreshing={portal.loading} onRefresh={() => void portal.refresh()} tintColor={palette.blue}/>}>
      <View style={styles.heading}><Text style={styles.eyebrow}>{code || `${portal.role} workspace`}</Text><Text style={styles.title}>{title}</Text><Text style={styles.subtitle}>{subtitle}</Text></View>
      {portal.loading && !portal.profile ? <View style={styles.stateCard}><ActivityIndicator color={palette.blue}/><Text>Connecting to institute data…</Text></View> : null}
      {portal.error ? <View style={[styles.stateCard, styles.errorCard]}><Ionicons name="cloud-offline-outline" size={24} color={palette.red}/><Text style={styles.stateTitle}>Connection unavailable</Text><Text style={styles.stateCopy}>{portal.error}</Text><Pressable disabled={portal.loading} onPress={() => void portal.refresh()} style={styles.retryButton}><Ionicons name="refresh-outline" size={15} color="white"/><Text style={styles.retryText}>{portal.loading ? 'Connecting…' : 'Retry connection'}</Text></Pressable></View> : null}
      {!portal.loading && !portal.error && !portal.profile ? <View style={styles.stateCard}><Ionicons name="person-add-outline" size={26} color={palette.blue}/><Text style={styles.stateTitle}>Profile not linked yet</Text><Text style={styles.stateCopy}>Ask Administrator to verify that an active {portal.role} profile exists for the Public ID used to sign in.</Text></View> : null}
      {showChildrenWhenUnavailable || (portal.profile && !portal.error) ? children : null}
    </ScrollView>
  </SafeAreaView>;
}

export function Card({ children, style }: PropsWithChildren<{ style?: object }>) {
  return <View style={[styles.card, style]}>{children}</View>;
}

export function SectionHeading({ title, detail }: { title: string; detail?: string }) {
  return <View style={styles.sectionHeading}><Text style={styles.sectionTitle}>{title}</Text>{detail ? <Text style={styles.sectionDetail}>{detail}</Text> : null}</View>;
}

export function MetricCard({ icon, label, value }: { icon: keyof typeof Ionicons.glyphMap; label: string; value: string | number; tone?: 'blue' | 'green' | 'amber' | 'violet' }) {
  return <Card style={styles.metric}><View style={styles.metricHeader}><Text style={styles.metricLabel}>{label}</Text><Ionicons name={icon} size={17} color={palette.blue}/></View><Text style={styles.metricValue}>{value}</Text></Card>;
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
  topbar: { minHeight: 62, paddingHorizontal: 16, flexDirection: 'row', alignItems: 'center', backgroundColor: palette.panel, borderBottomWidth: 1, borderBottomColor: palette.line },
  brandMark: { width: 38, height: 42, alignItems: 'center', justifyContent: 'center' },
  brandLogo: { width: 36, height: 40 },
  topbarCopy: { flex: 1, marginLeft: 9 },
  institute: { color: palette.ink, fontSize: 12, fontWeight: '700' },
  role: { color: palette.muted, fontSize: 10, textTransform: 'uppercase', letterSpacing: 0.5, marginTop: 2 },
  roleBadge: { minHeight: 26, flexDirection: 'row', alignItems: 'center', gap: 5, paddingHorizontal: 8, borderRadius: radius.small, borderWidth: 1, borderColor: palette.line, backgroundColor: palette.panel },
  roleBadgeText: { color: palette.blue, fontSize: 9, fontWeight: '700' },
  connectionDot: { width: 7, height: 7, marginLeft: 8, borderRadius: 4, backgroundColor: palette.green },
  connectionOffline: { backgroundColor: palette.red },
  content: { padding: 14, paddingBottom: 28, gap: 12 },
  heading: { paddingVertical: 14, paddingHorizontal: 15, borderWidth: 1, borderLeftWidth: 3, borderColor: palette.line, borderLeftColor: palette.blue, borderRadius: radius.medium, backgroundColor: palette.panel },
  eyebrow: { color: palette.blue, textTransform: 'uppercase', letterSpacing: 0.9, fontWeight: '700', fontSize: 9 },
  title: { color: palette.ink, fontSize: 23, fontWeight: '800', letterSpacing: -0.4, marginTop: 4 },
  subtitle: { color: palette.muted, fontSize: 12, lineHeight: 17, marginTop: 4 },
  card: { backgroundColor: palette.panel, borderRadius: radius.medium, borderWidth: 1, borderColor: palette.line, padding: 14, ...shadow },
  stateCard: { minHeight: 150, padding: 22, borderRadius: radius.medium, borderWidth: 1, borderColor: palette.line, backgroundColor: palette.panel, alignItems: 'center', justifyContent: 'center', gap: 8 },
  errorCard: { backgroundColor: '#FFFAFB', borderColor: '#F5CED4' },
  stateTitle: { color: palette.ink, fontSize: 14, fontWeight: '800', textAlign: 'center' },
  stateCopy: { color: palette.muted, fontSize: 12, lineHeight: 18, textAlign: 'center' },
  retryButton: { minHeight: 38, marginTop: 6, paddingHorizontal: 14, flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 7, borderRadius: radius.small, backgroundColor: palette.blue },
  retryText: { color: '#FFFFFF', fontSize: 11, fontWeight: '700' },
  sectionHeading: { flexDirection: 'row', alignItems: 'baseline', justifyContent: 'space-between', marginTop: 6, paddingBottom: 1 },
  sectionTitle: { color: palette.ink, fontSize: 14, fontWeight: '800' },
  sectionDetail: { color: palette.muted, fontSize: 10, fontWeight: '600' },
  metric: { width: '48.5%', minHeight: 96, padding: 13, borderTopWidth: 2, borderTopColor: palette.blue },
  metricHeader: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: 8 },
  metricValue: { color: palette.ink, fontWeight: '800', fontSize: 22, marginTop: 12 },
  metricLabel: { flex: 1, color: palette.muted, fontSize: 10, fontWeight: '600' },
  scheduleCard: { flexDirection: 'row', padding: 0, overflow: 'hidden', borderLeftWidth: 3, borderLeftColor: palette.blue },
  timeBlock: { width: 68, paddingVertical: 16, alignItems: 'center', backgroundColor: '#F8F9FB', borderRightWidth: 1, borderRightColor: palette.line },
  time: { color: palette.ink, fontWeight: '800', fontSize: 13 },
  timeEnd: { color: palette.muted, fontSize: 10, marginTop: 5 },
  scheduleCopy: { flex: 1, padding: 14 },
  scheduleTitleRow: { flexDirection: 'row', alignItems: 'center', gap: 7 },
  scheduleTitle: { flex: 1, color: palette.ink, fontSize: 13, fontWeight: '800' },
  scheduleCode: { color: palette.blue, fontSize: 10, fontWeight: '600', marginTop: 3 },
  scheduleMeta: { flexDirection: 'row', alignItems: 'center', gap: 4, marginTop: 7 },
  pill: { overflow: 'hidden', borderRadius: radius.small, paddingHorizontal: 7, paddingVertical: 3, textTransform: 'capitalize', fontSize: 9, fontWeight: '700' },
  empty: { minHeight: 122, alignItems: 'center', justifyContent: 'center', gap: 7, shadowOpacity: 0, elevation: 0 },
  identity: { flexDirection: 'row', alignItems: 'center', gap: 11 },
  avatar: { width: 44, height: 56, borderRadius: radius.small, backgroundColor: '#F1F3F6' },
  avatarFallback: { width: 44, height: 44, borderRadius: radius.small, alignItems: 'center', justifyContent: 'center', backgroundColor: '#F1F3F6', borderWidth: 1, borderColor: palette.line },
  identityCopy: { flex: 1 },
  identityName: { color: palette.ink, fontSize: 13, fontWeight: '700' },
  identityDetail: { color: palette.muted, fontSize: 10, marginTop: 3 },
});
