import Ionicons from '@expo/vector-icons/Ionicons';
import { useRouter } from 'expo-router';
import type { PropsWithChildren, ReactNode } from 'react';
import { ActivityIndicator, Image, Pressable, RefreshControl, ScrollView, StyleSheet, Text, View, type StyleProp, type ViewStyle } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { palette, radius, shadow } from '@/constants/theme';
import { usePortal } from '@/features/portal/portal-context';
import type { ScheduleItem } from '@/features/portal/portal-types';

export function PortalPage({ children, showChildrenWhenUnavailable = false }: PropsWithChildren<{ title: string; subtitle?: string; eyebrow?: string; showChildrenWhenUnavailable?: boolean }>) {
  const router = useRouter();
  const portal = usePortal();
  const values = portal.profile?.values;
  const unread = portal.announcements.filter(item => !item.isRead).length;
  const supportedPhoto = values?.photoDataUrl && (/^data:image\/(png|jpeg|webp);/i.test(values.photoDataUrl) || /^https?:\/\//i.test(values.photoDataUrl));
  const notificationsRoute = portal.role === 'teacher' ? '/(teacher)/notifications' : '/(student)/notifications';
  const profileRoute = portal.role === 'teacher' ? '/(teacher)/profile' : '/(student)/profile';

  return <SafeAreaView style={styles.safeArea} edges={['top']}>
    <View style={styles.topbar}>
      <View style={styles.brandMark}><View style={styles.brandHalo}/><Image source={require('../../assets/images/ink-logo.png')} style={styles.brandLogo} resizeMode="contain"/></View>
      <View style={styles.topbarCopy}><Text style={styles.institute}>Institute of New Khmer</Text><View style={styles.roleLine}><View style={[styles.connectionDot, portal.error ? styles.connectionOffline : undefined]}/><Text style={styles.role}>{portal.role} portal</Text></View></View>
      <View style={styles.topbarActions}>
        <Pressable accessibilityRole="button" accessibilityLabel="Open notifications" onPress={() => router.push(notificationsRoute)} style={({ pressed }) => [styles.actionButton, pressed && styles.pressed]}><Ionicons name="notifications-outline" size={19} color={palette.blueDark}/>{unread ? <View style={styles.notificationBadge}><Text style={styles.notificationBadgeText}>{Math.min(unread, 9)}</Text></View> : null}</Pressable>
        <Pressable accessibilityRole="button" accessibilityLabel="Open profile" onPress={() => router.push(profileRoute)} style={({ pressed }) => [styles.profileButton, pressed && styles.pressed]}>{supportedPhoto ? <Image source={{ uri: values?.photoDataUrl }} style={styles.profilePhoto}/> : <Ionicons name={portal.role === 'teacher' ? 'school-outline' : 'person-outline'} size={19} color={palette.blueDark}/>}</Pressable>
      </View>
    </View>
    <ScrollView contentContainerStyle={styles.content} refreshControl={<RefreshControl refreshing={portal.loading} onRefresh={() => void portal.refresh()} tintColor={palette.blue}/>}>
      {portal.loading && !portal.profile ? <View style={styles.stateCard}><ActivityIndicator color={palette.blue}/><Text>Connecting to institute data…</Text></View> : null}
      {portal.error ? <View style={[styles.stateCard, styles.errorCard]}><Ionicons name="cloud-offline-outline" size={24} color={palette.red}/><Text style={styles.stateTitle}>Connection unavailable</Text><Text style={styles.stateCopy}>{portal.error}</Text><Pressable disabled={portal.loading} onPress={() => void portal.refresh()} style={styles.retryButton}><Ionicons name="refresh-outline" size={15} color="white"/><Text style={styles.retryText}>{portal.loading ? 'Connecting…' : 'Retry connection'}</Text></Pressable></View> : null}
      {!portal.loading && !portal.error && !portal.profile ? <View style={styles.stateCard}><Ionicons name="person-add-outline" size={26} color={palette.blue}/><Text style={styles.stateTitle}>Profile not linked yet</Text><Text style={styles.stateCopy}>Ask Administrator to verify that an active {portal.role} profile exists for the Public ID used to sign in.</Text></View> : null}
      {showChildrenWhenUnavailable || (portal.profile && !portal.error) ? children : null}
    </ScrollView>
  </SafeAreaView>;
}

export function Card({ children, style }: PropsWithChildren<{ style?: StyleProp<ViewStyle> }>) {
  return <View style={[styles.card, style]}>{children}</View>;
}

export function SectionHeading({ title, detail, actionLabel, onAction }: { title: string; detail?: string; actionLabel?: string; onAction?: () => void }) {
  return <View style={styles.sectionHeading}><View><Text style={styles.sectionTitle}>{title}</Text>{detail ? <Text style={styles.sectionDetail}>{detail}</Text> : null}</View>{actionLabel && onAction ? <Pressable accessibilityRole="button" onPress={onAction} hitSlop={8} style={({ pressed }) => pressed && styles.pressed}><Text style={styles.sectionAction}>{actionLabel}</Text></Pressable> : null}</View>;
}

export function MetricCard({ icon, label, value, tone = 'blue' }: { icon: keyof typeof Ionicons.glyphMap; label: string; value: string | number; tone?: 'blue' | 'green' | 'amber' | 'violet' }) {
  const metricTone = tone === 'green' ? { color: palette.green, backgroundColor: palette.greenPale } : tone === 'amber' ? { color: palette.gold, backgroundColor: palette.goldPale } : tone === 'violet' ? { color: palette.violet, backgroundColor: palette.violetPale } : { color: palette.blue, backgroundColor: palette.bluePale };
  return <Card style={styles.metric}><View style={[styles.metricIcon, { backgroundColor: metricTone.backgroundColor }]}><Ionicons name={icon} size={18} color={metricTone.color}/></View><Text style={styles.metricValue}>{value}</Text><Text style={styles.metricLabel}>{label}</Text></Card>;
}

export function ScheduleCard({ item }: { item: ScheduleItem }) {
  const value = item.values;
  return <Card style={styles.scheduleCard}>
    <View style={styles.scheduleHeader}><View style={styles.scheduleTime}><Ionicons name="time-outline" size={15} color={palette.blue}/><Text style={styles.time}>{value.startsAt}–{value.endsAt}</Text></View><StatusPill value={value.status}/></View>
    <Text style={styles.scheduleTitle} numberOfLines={1}>{value.course}</Text>
    <Text style={styles.scheduleCode}>{value.courseCode} · {value.enrollmentCode}</Text>
    <View style={styles.scheduleDetails}><View style={styles.scheduleMeta}><Ionicons name="location-outline" size={14} color={palette.muted}/><Text style={styles.scheduleMetaText}>{value.classroom}{value.building ? ` · ${value.building}` : ''}</Text></View><View style={styles.scheduleMeta}><Ionicons name="person-outline" size={14} color={palette.muted}/><Text style={styles.scheduleMetaText}>{value.teacher} · Year {value.yearLevel}</Text></View></View>
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
  muted: { color: palette.muted, fontSize: 14, lineHeight: 21 },
});

const styles = StyleSheet.create({
  safeArea: { flex: 1, width: '100%', maxWidth: 620, alignSelf: 'center', backgroundColor: palette.canvas },
  topbar: { minHeight: 82, paddingHorizontal: 18, flexDirection: 'row', alignItems: 'center', backgroundColor: palette.panel, borderBottomWidth: StyleSheet.hairlineWidth, borderBottomColor: palette.line },
  brandMark: { width: 48, height: 52, alignItems: 'center', justifyContent: 'center' },
  brandHalo: { position: 'absolute', width: 45, height: 45, borderRadius: 15, backgroundColor: palette.bluePale, transform: [{ rotate: '8deg' }] },
  brandLogo: { width: 42, height: 48 },
  topbarCopy: { flex: 1, marginLeft: 11 },
  institute: { color: palette.ink, fontSize: 15, fontWeight: '900', letterSpacing: -0.25 },
  roleLine: { flexDirection: 'row', alignItems: 'center', gap: 7, marginTop: 4 },
  role: { color: palette.blueDark, fontSize: 11, textTransform: 'uppercase', letterSpacing: 0.9, fontWeight: '800' },
  topbarActions: { flexDirection: 'row', alignItems: 'center', gap: 9 },
  actionButton: { width: 44, height: 44, borderRadius: 15, alignItems: 'center', justifyContent: 'center', backgroundColor: palette.bluePale, borderWidth: 1, borderColor: '#D5E2FF' },
  profileButton: { width: 44, height: 44, borderRadius: 15, alignItems: 'center', justifyContent: 'center', overflow: 'hidden', backgroundColor: palette.goldPale, borderWidth: 1, borderColor: '#F1D66C' },
  profilePhoto: { width: '100%', height: '100%' },
  notificationBadge: { position: 'absolute', top: -2, right: -2, minWidth: 17, height: 17, paddingHorizontal: 4, borderRadius: 9, alignItems: 'center', justifyContent: 'center', backgroundColor: palette.gold, borderWidth: 2, borderColor: palette.panel },
  notificationBadgeText: { color: '#FFFFFF', fontSize: 10, fontWeight: '900' },
  connectionDot: { width: 7, height: 7, borderRadius: 4, backgroundColor: palette.green },
  connectionOffline: { backgroundColor: palette.red },
  content: { paddingHorizontal: 18, paddingTop: 18, paddingBottom: 40, gap: 20 },
  card: { backgroundColor: palette.panel, borderRadius: radius.medium, borderWidth: 1, borderColor: palette.line, padding: 18, ...shadow },
  stateCard: { minHeight: 160, padding: 26, borderRadius: radius.medium, borderWidth: 1, borderColor: palette.line, backgroundColor: palette.panel, alignItems: 'center', justifyContent: 'center', gap: 10 },
  errorCard: { backgroundColor: '#FFFAFB', borderColor: '#F5CED4' },
  stateTitle: { color: palette.ink, fontSize: 16, fontWeight: '800', textAlign: 'center' },
  stateCopy: { color: palette.muted, fontSize: 13, lineHeight: 20, textAlign: 'center' },
  retryButton: { minHeight: 48, marginTop: 8, paddingHorizontal: 18, flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 8, borderRadius: radius.small, backgroundColor: palette.blue },
  retryText: { color: '#FFFFFF', fontSize: 14, fontWeight: '900' },
  sectionHeading: { minHeight: 46, flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginTop: 5 },
  sectionTitle: { color: palette.ink, fontSize: 20, fontWeight: '900', letterSpacing: -0.4 },
  sectionDetail: { color: palette.muted, fontSize: 13, fontWeight: '600', marginTop: 3 },
  sectionAction: { color: palette.blue, fontSize: 14, fontWeight: '900' },
  metric: { width: '48.5%', minHeight: 120, padding: 15, shadowOpacity: 0, elevation: 0 },
  metricIcon: { width: 38, height: 38, borderRadius: 10, alignItems: 'center', justifyContent: 'center' },
  metricValue: { color: palette.ink, fontWeight: '800', fontSize: 24, marginTop: 14 },
  metricLabel: { color: palette.muted, fontSize: 14, fontWeight: '700', marginTop: 4 },
  scheduleCard: { borderLeftWidth: 4, borderLeftColor: palette.blue, shadowOpacity: 0, elevation: 0 },
  scheduleHeader: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: 8 },
  scheduleTime: { flexDirection: 'row', alignItems: 'center', gap: 6 },
  time: { color: palette.blueDark, fontWeight: '900', fontSize: 14 },
  scheduleTitle: { color: palette.ink, fontSize: 18, lineHeight: 23, fontWeight: '900', marginTop: 14 },
  scheduleCode: { color: palette.blue, fontSize: 13, fontWeight: '800', marginTop: 5 },
  scheduleDetails: { gap: 7, marginTop: 12, paddingTop: 11, borderTopWidth: StyleSheet.hairlineWidth, borderTopColor: palette.line },
  scheduleMeta: { flexDirection: 'row', alignItems: 'center', gap: 6 },
  scheduleMetaText: { flex: 1, color: palette.muted, fontSize: 14, lineHeight: 20 },
  pill: { overflow: 'hidden', borderRadius: radius.pill, paddingHorizontal: 11, paddingVertical: 6, textTransform: 'capitalize', fontSize: 12, fontWeight: '900' },
  empty: { minHeight: 122, alignItems: 'center', justifyContent: 'center', gap: 7, shadowOpacity: 0, elevation: 0 },
  identity: { flexDirection: 'row', alignItems: 'center', gap: 11 },
  avatar: { width: 52, height: 60, borderRadius: radius.small, backgroundColor: '#F1F3F6' },
  avatarFallback: { width: 52, height: 52, borderRadius: 26, alignItems: 'center', justifyContent: 'center', backgroundColor: palette.bluePale, borderWidth: 1, borderColor: '#D6DCFA' },
  identityCopy: { flex: 1 },
  identityName: { color: palette.ink, fontSize: 17, fontWeight: '900' },
  identityDetail: { color: palette.muted, fontSize: 14, marginTop: 5 },
  pressed: { opacity: 0.68 },
});
