import Ionicons from '@expo/vector-icons/Ionicons';
import { useState } from 'react';
import { Image, Pressable, StyleSheet, Text, View } from 'react-native';
import { Card, EmptyBlock, PortalPage, SectionHeading } from '@/components/portal-ui';
import { palette, radius } from '@/constants/theme';
import type { MobileRole } from '@/features/auth/auth-context';
import type { Announcement } from '../portal-types';
import { usePortal } from '../portal-context';

type NotificationFilter = 'all' | 'unread' | 'read';

export function NotificationsScreen({ role }: { role: MobileRole }) {
  const portal = usePortal();
  const [filter, setFilter] = useState<NotificationFilter>('all');
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const ordered = [...portal.announcements].sort((a, b) => b.createAt.localeCompare(a.createAt));
  const unread = ordered.filter(item => !item.isRead).length;
  const read = ordered.length - unread;
  const visible = filter === 'unread' ? ordered.filter(item => !item.isRead) : filter === 'read' ? ordered.filter(item => item.isRead) : ordered;
  const selected = ordered.find(item => item.id === selectedId);
  const accent = palette.blue;
  const filterCounts = { all: ordered.length, unread, read };

  function openNotification(item: Announcement) {
    setSelectedId(item.id);
    if (!item.isRead) void portal.markAnnouncementRead(item.id);
  }

  if (selected) return <NotificationDetail item={selected} role={role} onBack={() => setSelectedId(null)}/>;

  return <PortalPage title="Notifications" subtitle="Institute announcements, attendance notices, results, and urgent updates for your account.">
    <View style={styles.filters}>
      {(['all', 'unread', 'read'] as const).map(value => <Pressable key={value} onPress={() => setFilter(value)} style={[styles.filter, filter === value && { backgroundColor: accent, borderColor: accent }]}><Text style={[styles.filterText, filter === value && styles.filterTextActive]}>{`${value[0].toUpperCase()}${value.slice(1)} (${filterCounts[value]})`}</Text></Pressable>)}
    </View>
    <SectionHeading title={`${filter[0].toUpperCase()}${filter.slice(1)} notifications`} detail={`${visible.length} items`}/>
    {visible.length ? (
      <View style={styles.inboxList}>{visible.map(item => <NotificationRow item={item} onPress={() => openNotification(item)} key={item.id}/>)}</View>
    ) : (
      <EmptyBlock icon="notifications-off-outline" title={filter === 'all' ? 'No notifications' : filter === 'unread' ? 'No unread notifications' : 'No read notifications'} detail={filter === 'all' ? 'New institute announcements will appear here.' : filter === 'unread' ? 'Opening a message moves it out of Unread.' : 'Messages you open will appear here.'}/>
    )}
  </PortalPage>;
}

function NotificationRow({ item, onPress }: { item: Announcement; onPress: () => void }) {
  return <Pressable accessibilityRole="button" accessibilityLabel={`${item.isRead ? 'Read' : 'Unread'} notification: ${item.title}`} accessibilityHint="Shows the full notification message" onPress={onPress} style={({ pressed }) => [styles.messageRow, item.isRead ? styles.readRow : styles.unreadRow, pressed && styles.pressed]}>
      <View style={styles.notificationTop}>
        <View style={styles.notificationIcon}><Image source={require('../../../../assets/images/ink-logo.png')} style={styles.notificationLogo} resizeMode="contain"/></View>
        <View style={styles.notificationTitleBlock}><Text style={[styles.notificationSender, item.isRead && styles.readSender]}>Institute of New Khmer</Text><Text style={[styles.notificationTitle, item.isRead && styles.readTitle]} numberOfLines={1}>{item.title}</Text><Text style={[styles.notificationMessage, !item.isRead && styles.unreadMessage]} numberOfLines={1}>{item.message}</Text></View>
        <View style={styles.notificationAside}><NotificationTypeBadge type={item.type} compact/><View style={styles.dateGroup}><Text style={[styles.notificationDate, !item.isRead && styles.unreadDate]}>{formatDate(item.createAt)}</Text>{!item.isRead ? <View style={styles.unreadDot}/> : null}</View></View>
      </View>
  </Pressable>;
}

function NotificationDetail({ item, role, onBack }: { item: Announcement; role: MobileRole; onBack: () => void }) {
  return <PortalPage title="Message" subtitle="Read the complete institute notification." eyebrow={`${role} inbox`}>
    <Pressable accessibilityRole="button" accessibilityLabel="Back to notifications" onPress={onBack} style={({ pressed }) => [styles.backButton, pressed && styles.pressed]}>
      <Ionicons name="chevron-back" size={18} color={palette.blue}/><Text style={styles.backText}>Notifications</Text>
    </Pressable>
    <Card style={styles.detailCard}>
      <View style={styles.detailSenderRow}>
        <View style={styles.detailIcon}><Image source={require('../../../../assets/images/ink-logo.png')} style={styles.detailLogo} resizeMode="contain"/></View>
        <View style={styles.detailSenderCopy}><Text style={styles.detailSender}>Institute of New Khmer</Text></View>
        <NotificationTypeBadge type={item.type}/>
      </View>
      <View style={styles.detailContent}>
        <Text selectable style={styles.detailSubject}>{item.title}</Text>
        <Text selectable style={styles.detailMessage}>{item.message}</Text>
      </View>
      <View style={styles.detailTimestamp}><Text selectable style={styles.detailDate}>{formatDateTime(item.createAt)}</Text></View>
    </Card>
  </PortalPage>;
}

function NotificationTypeBadge({ type, compact = false }: { type: Announcement['type']; compact?: boolean }) {
  const tone = notificationTone(type);
  return <View style={[styles.typeBadge, compact && styles.typeBadgeCompact, { backgroundColor: tone.background, borderColor: tone.border }]}>
    <Ionicons name={notificationIcon(type)} size={compact ? 14 : 16} color={tone.foreground}/><Text style={[styles.typeBadgeText, compact && styles.typeBadgeTextCompact, { color: tone.foreground }]}>{type}</Text>
  </View>;
}

function notificationTone(type: Announcement['type']) {
  if (type === 'Emergency') return { background: palette.redPale, border: '#F1C5CC', foreground: palette.red };
  if (type === 'Attendance') return { background: palette.greenPale, border: '#CDEBDD', foreground: palette.green };
  if (type === 'Result') return { background: palette.violetPale, border: '#D9D1FF', foreground: palette.violet };
  if (type === 'Finance') return { background: palette.goldPale, border: '#EBD9A2', foreground: '#9B6812' };
  return { background: palette.bluePale, border: '#CFE0FF', foreground: palette.blue };
}

function notificationIcon(type: Announcement['type']): keyof typeof Ionicons.glyphMap {
  if (type === 'Emergency') return 'warning-outline';
  if (type === 'Result') return 'ribbon-outline';
  if (type === 'Attendance') return 'checkmark-circle-outline';
  if (type === 'Finance') return 'card-outline';
  return 'notifications-outline';
}

function formatDate(value: string) {
  const date = new Date(value);
  return Number.isNaN(date.valueOf()) ? value : new Intl.DateTimeFormat('en-GB', { day: '2-digit', month: 'short', year: 'numeric' }).format(date);
}

function formatDateTime(value: string) {
  const date = new Date(value);
  if (Number.isNaN(date.valueOf())) return value;
  const datePart = new Intl.DateTimeFormat('en-GB', { day: '2-digit', month: '2-digit', year: 'numeric' }).format(date);
  const timePart = new Intl.DateTimeFormat('en-GB', { hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false }).format(date);
  return `${datePart} · ${timePart}`;
}

const styles = StyleSheet.create({
  filters: { flexDirection: 'row', gap: 8, padding: 5, borderRadius: radius.large, backgroundColor: '#FFFFFF', borderWidth: 1, borderColor: palette.line },
  filter: { minHeight: 43, flex: 1, alignItems: 'center', justifyContent: 'center', paddingHorizontal: 11, borderRadius: radius.medium, borderWidth: 1, borderColor: 'transparent', backgroundColor: '#FFFFFF' },
  filterText: { color: palette.muted, fontSize: 13, fontWeight: '800', textTransform: 'capitalize' },
  filterTextActive: { color: 'white' },
  inboxList: { gap: 10 },
  messageRow: { minHeight: 92, paddingHorizontal: 14, paddingVertical: 13, borderRadius: radius.medium, borderWidth: 1, borderColor: palette.line },
  unreadRow: { backgroundColor: palette.bluePale, borderColor: '#C8DAFF' },
  readRow: { backgroundColor: palette.panel },
  dateGroup: { flexDirection: 'row', alignItems: 'center', justifyContent: 'flex-end', gap: 7 },
  notificationTop: { flexDirection: 'row', alignItems: 'center', gap: 11 },
  notificationIcon: { width: 43, height: 47, alignItems: 'center', justifyContent: 'center', borderRadius: 14, backgroundColor: '#FFFFFF' },
  notificationLogo: { width: 38, height: 42 },
  notificationTitleBlock: { flex: 1 },
  notificationAside: { alignItems: 'flex-end', gap: 6 },
  notificationSender: { color: palette.blue, fontSize: 11, lineHeight: 14, fontWeight: '900', textTransform: 'uppercase', letterSpacing: 0.45 },
  notificationTitle: { color: palette.ink, fontSize: 16, fontWeight: '900', marginTop: 2 },
  notificationDate: { color: palette.muted, fontSize: 11, fontWeight: '700' },
  readSender: { color: '#4B5568', fontWeight: '600' },
  readTitle: { color: '#4B5568', fontWeight: '600' },
  unreadDate: { color: palette.blue, fontWeight: '800' },
  unreadDot: { width: 7, height: 7, borderRadius: 4, backgroundColor: palette.blue },
  notificationMessage: { color: palette.muted, fontSize: 13, lineHeight: 18, marginTop: 4 },
  unreadMessage: { color: '#46516A', fontWeight: '600' },
  typeBadge: { minHeight: 34, flexDirection: 'row', alignItems: 'center', gap: 7, paddingHorizontal: 11, paddingVertical: 6, borderRadius: radius.small, borderWidth: 1 },
  typeBadgeText: { fontSize: 13, fontWeight: '800' },
  typeBadgeCompact: { minHeight: 27, gap: 5, paddingHorizontal: 8, paddingVertical: 4 },
  typeBadgeTextCompact: { fontSize: 12 },
  backButton: { alignSelf: 'flex-start', minHeight: 44, flexDirection: 'row', alignItems: 'center', gap: 4, paddingHorizontal: 12, borderRadius: radius.pill, backgroundColor: palette.bluePale },
  backText: { color: palette.blue, fontSize: 13, fontWeight: '900' },
  detailCard: { padding: 20, borderTopWidth: 6, borderTopColor: palette.gold },
  detailSenderRow: { flexDirection: 'row', alignItems: 'center', gap: 11 },
  detailIcon: { width: 42, height: 47, alignItems: 'center', justifyContent: 'center' },
  detailLogo: { width: 42, height: 47 },
  detailSenderCopy: { flex: 1 },
  detailSender: { color: palette.ink, fontSize: 15, fontWeight: '900' },
  detailContent: { gap: 10, marginTop: 24 },
  detailSubject: { color: palette.ink, fontSize: 23, lineHeight: 30, fontWeight: '900' },
  detailMessage: { color: palette.ink, fontSize: 16, lineHeight: 25 },
  detailTimestamp: { alignSelf: 'flex-end', marginTop: 24 },
  detailDate: { color: palette.muted, fontSize: 12, fontWeight: '600', textAlign: 'right' },
  pressed: { opacity: 0.68 },
});
