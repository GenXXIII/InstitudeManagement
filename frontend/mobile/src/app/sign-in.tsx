import Ionicons from '@expo/vector-icons/Ionicons';
import { useState } from 'react';
import { Image, KeyboardAvoidingView, Platform, Pressable, StyleSheet, Text, TextInput, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { palette, radius, shadow } from '@/constants/theme';
import { useAuth } from '@/features/auth/auth-context';
import { getApiBaseUrl } from '@/features/portal/portal-api';

export default function SignInScreen() {
  const { signIn } = useAuth();
  const [publicId, setPublicId] = useState('');
  const [password, setPassword] = useState('');
  const [secure, setSecure] = useState(true);
  const [error, setError] = useState('');
  const [submitting, setSubmitting] = useState(false);

  async function submit() {
    setSubmitting(true);
    setError('');
    try { await signIn(publicId, password); }
    catch (reason) { setError(reason instanceof Error ? reason.message : 'Could not sign in.'); }
    finally { setSubmitting(false); }
  }

  return <SafeAreaView style={styles.safe}>
    <KeyboardAvoidingView style={styles.keyboard} behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
      <View style={styles.hero}><View style={styles.logoShell}><Image source={require('../../assets/images/ink-logo.png')} style={styles.logo} resizeMode="contain"/></View><Text style={styles.kicker}>INSTITUDE OF NEW KHMER</Text><Text style={styles.title}>Academic mobile portal</Text><Text style={styles.subtitle}>Secure access for institute Teachers and Students.</Text></View>
      <View style={styles.form}>
        <Text style={styles.formTitle}>Sign in to your portal</Text>
        <View style={styles.field}><Ionicons name="id-card-outline" size={18} color={palette.muted}/><TextInput style={styles.input} value={publicId} onChangeText={setPublicId} placeholder="Public ID" placeholderTextColor="#98A6BA" autoCapitalize="characters" autoCorrect={false} textContentType="username"/></View>
        <View style={styles.field}><Ionicons name="lock-closed-outline" size={18} color={palette.muted}/><TextInput style={styles.input} value={password} onChangeText={setPassword} placeholder="Password" placeholderTextColor="#98A6BA" secureTextEntry={secure} textContentType="password"/><Pressable hitSlop={12} onPress={() => setSecure(value => !value)}><Ionicons name={secure ? 'eye-outline' : 'eye-off-outline'} size={18} color={palette.muted}/></Pressable></View>
        {error ? <Text style={styles.error}>{error}</Text> : null}
        <Pressable style={({ pressed }) => [styles.submit, pressed && styles.pressed]} onPress={() => void submit()} disabled={submitting}><Text style={styles.submitText}>{submitting ? 'Signing in…' : 'Continue'}</Text><Ionicons name="arrow-forward" size={18} color="white"/></Pressable>
        <View style={styles.demo}><Text style={styles.demoTitle}>Account linking</Text><View style={styles.accountHelp}><Ionicons name="information-circle-outline" size={17} color={palette.blue}/><Text style={styles.accountHelpText}>Use the exact Teacher or Student Public ID from Web Management. The initial password for every account is <Text style={styles.demoRole}>1234</Text>.</Text></View></View>
      </View>
      <View style={styles.server}><View style={styles.serverDot}/><Text numberOfLines={1}>Institute API · {getApiBaseUrl()}</Text></View>
    </KeyboardAvoidingView>
  </SafeAreaView>;
}

const styles = StyleSheet.create({
  safe: { flex: 1, backgroundColor: palette.canvas },
  keyboard: { flex: 1, width: '100%', maxWidth: 520, alignSelf: 'center', paddingHorizontal: 24, justifyContent: 'center' },
  hero: { alignItems: 'flex-start', marginBottom: 24 },
  logoShell: { width: 64, height: 70, alignItems: 'center', justifyContent: 'center' },
  logo: { width: '100%', height: '100%' },
  kicker: { color: palette.blue, fontSize: 10, letterSpacing: 1.5, fontWeight: '800', marginTop: 14 },
  title: { color: palette.ink, fontSize: 30, lineHeight: 36, fontWeight: '800', letterSpacing: -0.7, marginTop: 7 },
  subtitle: { maxWidth: 360, color: palette.muted, fontSize: 14, lineHeight: 20, marginTop: 7 },
  form: { backgroundColor: 'white', borderRadius: radius.large, borderWidth: 1, borderColor: palette.line, padding: 22, gap: 14, ...shadow },
  formTitle: { color: palette.ink, fontSize: 20, fontWeight: '800', marginBottom: 3 },
  field: { minHeight: 54, flexDirection: 'row', alignItems: 'center', paddingHorizontal: 15, gap: 11, backgroundColor: '#FFFFFF', borderWidth: 1, borderColor: '#CDD3DF', borderRadius: radius.small },
  input: { flex: 1, color: palette.ink, fontSize: 15, paddingVertical: 14 },
  error: { color: palette.red, fontSize: 12, lineHeight: 18 },
  submit: { minHeight: 54, borderRadius: radius.small, backgroundColor: palette.blue, flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 9, marginTop: 2 },
  submitText: { color: 'white', fontWeight: '800', fontSize: 15 },
  pressed: { opacity: 0.8 },
  demo: { borderTopWidth: 1, borderTopColor: palette.line, marginTop: 5, paddingTop: 10 },
  demoTitle: { color: palette.muted, fontSize: 10, textTransform: 'uppercase', letterSpacing: 0.8, fontWeight: '800', marginBottom: 6 },
  accountHelp: { minHeight: 42, flexDirection: 'row', alignItems: 'flex-start', gap: 8, paddingTop: 4 },
  accountHelpText: { flex: 1, color: palette.muted, fontSize: 12, lineHeight: 19 },
  demoRole: { color: palette.ink, fontWeight: '700' },
  server: { alignSelf: 'center', flexDirection: 'row', alignItems: 'center', gap: 6, maxWidth: '90%', marginTop: 16 },
  serverDot: { width: 7, height: 7, borderRadius: 7, backgroundColor: palette.green },
});
