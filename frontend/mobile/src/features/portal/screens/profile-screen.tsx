import Ionicons from '@expo/vector-icons/Ionicons';
import { Alert, Platform, Pressable, StyleSheet, Text, View } from 'react-native';
import { Card, Identity, PortalPage, SectionHeading, StatusPill } from '@/components/portal-ui';
import { palette, radius } from '@/constants/theme';
import type { MobileRole } from '@/features/auth/auth-context';
import { useAuth } from '@/features/auth/auth-context';
import { getApiBaseUrl } from '../portal-api';
import { usePortal } from '../portal-context';

export function ProfileScreen({ role }: { role: MobileRole }) {
  const { session, signOut } = useAuth();
  const portal = usePortal();
  const values = portal.profile?.values;

  function confirmSignOut() {
    if (Platform.OS === 'web') {
      if (window.confirm('Sign out and return to the Teacher and Student sign-in screen?')) void signOut();
      return;
    }
    Alert.alert('Sign out?', 'You will return to the Teacher and Student sign-in screen.', [{ text: 'Cancel', style: 'cancel' }, { text: 'Sign out', style: 'destructive', onPress: () => void signOut() }]);
  }

  return <PortalPage title="Profile" subtitle="Your identity and academic details." showChildrenWhenUnavailable>
    {values ? <Card><Identity photo={values.photoDataUrl} name={values.name} detail={values.department || values.email || role} trailing={<StatusPill value={values.status}/>} /></Card> : null}
    <SectionHeading title="Profile details"/>
    <Card style={styles.detailCard}>
      <DetailRow icon="id-card-outline" label="Login Public ID" value={session?.publicId ?? ''}/>
      <DetailRow icon="mail-outline" label="Email" value={values?.email || 'Not recorded'}/>
      <DetailRow icon="business-outline" label="Department" value={values?.department || 'Not assigned'}/>
      {values && 'year' in values ? <DetailRow icon="school-outline" label="Academic placement" value={`Year ${values.year || '—'} · ${values.shift || 'Shift pending'}`}/> : null}
    </Card>
    <SectionHeading title="Connection"/>
    <Card><Text style={styles.connectionTitle}>Institute API server</Text><Text selectable style={styles.connectionUrl}>{getApiBaseUrl()}</Text><Text style={styles.connectionHelp}>The iPhone and the computer running Docker must be connected to the same Wi-Fi network.</Text></Card>
    <Pressable onPress={confirmSignOut} style={({ pressed }) => [styles.signOut, pressed && styles.pressed]}><Ionicons name="log-out-outline" size={19} color={palette.red}/><Text style={styles.signOutText}>Sign out of {role}</Text></Pressable>
  </PortalPage>;
}

function DetailRow({ icon, label, value }: { icon: keyof typeof Ionicons.glyphMap; label: string; value: string }) {
  return <View style={styles.detailRow}><View style={styles.detailIcon}><Ionicons name={icon} size={17} color={palette.blue}/></View><View style={styles.detailCopy}><Text style={styles.detailLabel}>{label}</Text><Text style={styles.detailValue}>{value}</Text></View></View>;
}

const styles = StyleSheet.create({
  detailCard: { paddingVertical: 7, shadowOpacity: 0, elevation: 0 },
  detailRow: { minHeight: 64, flexDirection: 'row', alignItems: 'center', gap: 12, borderBottomWidth: StyleSheet.hairlineWidth, borderBottomColor: palette.line },
  detailIcon: { width: 30, height: 34, alignItems: 'flex-start', justifyContent: 'center' },
  detailCopy: { flex: 1 },
  detailLabel: { color: palette.muted, fontSize: 10, textTransform: 'uppercase', letterSpacing: 0.7, fontWeight: '800' },
  detailValue: { color: palette.ink, fontSize: 14, fontWeight: '700', marginTop: 4 },
  connectionTitle: { color: palette.ink, fontSize: 14, fontWeight: '800' },
  connectionUrl: { color: palette.blue, fontSize: 12, marginTop: 7 },
  connectionHelp: { color: palette.muted, fontSize: 12, lineHeight: 19, marginTop: 9 },
  signOut: { minHeight: 50, flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 8, borderRadius: radius.small, backgroundColor: '#FFFFFF', borderWidth: 1, borderColor: '#DDAEB3' },
  signOutText: { color: palette.red, fontSize: 13, fontWeight: '800', textTransform: 'capitalize' },
  pressed: { opacity: 0.7 },
});
