import Ionicons from '@expo/vector-icons/Ionicons';
import { useState } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { Card, EmptyBlock, MetricCard, PortalPage, portalStyles, SectionHeading, StatusPill } from '@/components/portal-ui';
import { palette, radius } from '@/constants/theme';
import type { MobileRole } from '@/features/auth/auth-context';
import type { Announcement } from '../portal-types';
import { usePortal } from '../portal-context';

type NotificationFilter = 'all' | 'unread';

export function NotificationsScreen({ role }: { role: MobileRole }) {
  const portal = usePortal();
  const [filter, setFilter] = useState<NotificationFilter>('all');
  const ordered = [...portal.announcements].sort((a, b) => b.createAt.localeCompare(a.createAt));
  const unread = ordered.filter(item => !item.isRead).length;
  const emergency = ordered.filter(item => item.type === 'Emergency').length;
  const visible = filter === 'unread' ? ordered.filter(item => !item.isRead) : ordered;
  const accent = palette.blue;

  return <PortalPage title="Notifications" subtitle="Institute announcements, attendance notices, results, and urgent updates for your account.">
    <View style={portalStyles.grid}>
      <MetricCard icon="mail-unread-outline" label="Unread" value={unread}/>
      <MetricCard icon="warning-outline" label="Urgent" value={emergency} tone="amber"/>
    </View>
    <View style={styles.filters}>
      {(['all', 'unread'] as const).map(value => <Pressable key={value} onPress={() => setFilter(value)} style={[styles.filter, filter === value && { backgroundColor: accent, borderColor: accent }]}><Text style={[styles.filterText, filter === value && styles.filterTextActive]}>{value === 'all' ? `All (${ordered.length})` : `Unread (${unread})`}</Text></Pressable>)}
    </View>
    <SectionHeading title={filter === 'all' ? 'All notifications' : 'Unread notifications'} detail={`${visible.length} items`}/>
    <View style={portalStyles.stack}>{visible.length ? visible.map(item => <NotificationCard item={item} key={item.id}/>) : <EmptyBlock icon="notifications-off-outline" title={filter === 'all' ? 'No notifications' : 'You are all caught up'} detail={filter === 'all' ? 'New institute announcements will appear here.' : 'There are no unread notifications.'}/>}</View>
  </PortalPage>;
}

function NotificationCard({ item }: { item: Announcement }) {
  const urgent = item.type === 'Emergency';
  return <Card style={!item.isRead ? styles.unreadCard : undefined}>
    <View style={styles.notificationTop}>
      <View style={[styles.notificationIcon, urgent && styles.urgentIcon]}><Ionicons name={urgent ? 'warning-outline' : item.type === 'Result' ? 'ribbon-outline' : item.type === 'Attendance' ? 'checkmark-circle-outline' : 'notifications-outline'} size={18} color={urgent ? palette.red : palette.blue}/></View>
      <View style={styles.notificationTitleBlock}><Text style={styles.notificationTitle}>{item.title}</Text><Text style={styles.notificationMeta}>{item.announcementCode} · {formatDate(item.createAt)}</Text></View>
      {!item.isRead ? <Text style={styles.newBadge}>New</Text> : <StatusPill value={item.type}/>} 
    </View>
    <Text style={styles.notificationMessage}>{item.message}</Text>
    <View style={styles.notificationFooter}><Text style={styles.footerText}>{item.type}</Text><Text style={styles.footerText}>{item.isRead ? 'Read' : 'Unread'}</Text></View>
  </Card>;
}

function formatDate(value: string) {
  const date = new Date(value);
  return Number.isNaN(date.valueOf()) ? value : new Intl.DateTimeFormat('en-GB', { day: '2-digit', month: 'short', year: 'numeric' }).format(date);
}

const styles = StyleSheet.create({
  filters: { flexDirection: 'row', gap: 7 },
  filter: { minHeight: 34, justifyContent: 'center', paddingHorizontal: 13, borderRadius: radius.small, borderWidth: 1, borderColor: palette.line, backgroundColor: '#FFFFFF' },
  filterText: { color: palette.muted, fontSize: 10, fontWeight: '600', textTransform: 'capitalize' },
  filterTextActive: { color: 'white' },
  unreadCard: { borderLeftWidth: 3, borderLeftColor: palette.blue, backgroundColor: '#FBFCFE' },
  notificationTop: { flexDirection: 'row', alignItems: 'center', gap: 10 },
  notificationIcon: { width: 32, height: 32, borderRadius: radius.small, alignItems: 'center', justifyContent: 'center', borderWidth: 1, borderColor: palette.line, backgroundColor: '#FFFFFF' },
  urgentIcon: { borderColor: '#F1C5CC', backgroundColor: '#FFFFFF' },
  notificationTitleBlock: { flex: 1 },
  notificationTitle: { color: palette.ink, fontSize: 13, fontWeight: '700' },
  notificationMeta: { color: palette.muted, fontSize: 9, marginTop: 3 },
  newBadge: { overflow: 'hidden', paddingHorizontal: 7, paddingVertical: 3, borderRadius: radius.small, backgroundColor: palette.blue, color: 'white', fontSize: 8, fontWeight: '700' },
  notificationMessage: { color: palette.muted, fontSize: 12, lineHeight: 18, marginTop: 12 },
  notificationFooter: { flexDirection: 'row', justifyContent: 'space-between', marginTop: 12, paddingTop: 10, borderTopWidth: StyleSheet.hairlineWidth, borderTopColor: palette.line },
  footerText: { color: palette.muted, fontSize: 9, fontWeight: '600' },
});
