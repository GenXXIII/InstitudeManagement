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
  const code = values && ('teacherCode' in values ? values.teacherCode : values.studentCode);

  function confirmSignOut() {
    if (Platform.OS === 'web') {
      if (window.confirm('Sign out and return to the Teacher and Student sign-in screen?')) void signOut();
      return;
    }
    Alert.alert('Sign out?', 'You will return to the Teacher and Student sign-in screen.', [{ text: 'Cancel', style: 'cancel' }, { text: 'Sign out', style: 'destructive', onPress: () => void signOut() }]);
  }

  return <PortalPage title="Profile" subtitle={`Your identity, academic details, and ${role} mobile access.`} showChildrenWhenUnavailable>
    {values ? <Card><Identity photo={values.photoDataUrl} name={values.name} detail={code || role} trailing={<StatusPill value={values.status}/>} /></Card> : null}
    <SectionHeading title="Profile details"/>
    <Card style={styles.detailCard}>
      <DetailRow icon="id-card-outline" label="Login Public ID" value={session?.publicId ?? ''}/>
      <DetailRow icon="mail-outline" label="Email" value={values?.email || 'Not recorded'}/>
      <DetailRow icon="id-card-outline" label={role === 'teacher' ? 'Teacher code' : 'Student code'} value={code || 'Not linked'}/>
      <DetailRow icon="business-outline" label="Department" value={values?.department || 'Not assigned'}/>
      {values && 'year' in values ? <DetailRow icon="school-outline" label="Academic placement" value={`Year ${values.year || '—'} · ${values.shift || 'Shift pending'}`}/> : null}
      <DetailRow icon="shield-checkmark-outline" label="Access" value={`${role[0].toUpperCase()}${role.slice(1)} only`}/>
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
  detailCard: { paddingVertical: 5 },
  detailRow: { minHeight: 57, flexDirection: 'row', alignItems: 'center', gap: 11, borderBottomWidth: StyleSheet.hairlineWidth, borderBottomColor: palette.line },
  detailIcon: { width: 28, height: 32, alignItems: 'flex-start', justifyContent: 'center' },
  detailCopy: { flex: 1 },
  detailLabel: { color: palette.muted, fontSize: 9, textTransform: 'uppercase', letterSpacing: 0.5, fontWeight: '700' },
  detailValue: { color: palette.ink, fontSize: 12, fontWeight: '700', marginTop: 3 },
  connectionTitle: { color: palette.ink, fontSize: 12, fontWeight: '700' },
  connectionUrl: { color: palette.blue, fontSize: 11, marginTop: 6 },
  connectionHelp: { color: palette.muted, fontSize: 11, lineHeight: 17, marginTop: 8 },
  signOut: { minHeight: 46, flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 8, borderRadius: radius.small, backgroundColor: '#FFFFFF', borderWidth: 1, borderColor: '#E3AEB7' },
  signOutText: { color: palette.red, fontSize: 12, fontWeight: '700', textTransform: 'capitalize' },
  pressed: { opacity: 0.7 },
});
