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
      <View style={styles.skyOrb}/><View style={styles.goldOrb}/>
      <View style={styles.hero}><View style={styles.logoShell}><View style={styles.logoGlow}/><Image source={require('../../assets/images/ink-logo.png')} style={styles.logo} resizeMode="contain"/></View><Text style={styles.kicker}>INSTITUDE OF NEW KHMER</Text><Text style={styles.title}>Welcome to your campus</Text><Text style={styles.subtitle}>A bright, simple place for classes, results, payments, and the work that matters today.</Text></View>
      <View style={styles.form}>
        <View><Text style={styles.formTitle}>Welcome back</Text><Text style={styles.formSubtitle}>Sign in with your Teacher or Student Public ID.</Text></View>
        <View style={styles.field}><Ionicons name="id-card-outline" size={18} color={palette.muted}/><TextInput style={styles.input} value={publicId} onChangeText={setPublicId} placeholder="Public ID" placeholderTextColor="#98A6BA" autoCapitalize="characters" autoCorrect={false} textContentType="username"/></View>
        <View style={styles.field}><Ionicons name="lock-closed-outline" size={18} color={palette.muted}/><TextInput style={styles.input} value={password} onChangeText={setPassword} placeholder="Password" placeholderTextColor="#98A6BA" secureTextEntry={secure} textContentType="password"/><Pressable hitSlop={12} onPress={() => setSecure(value => !value)}><Ionicons name={secure ? 'eye-outline' : 'eye-off-outline'} size={18} color={palette.muted}/></Pressable></View>
        {error ? <Text style={styles.error}>{error}</Text> : null}
        <Pressable style={({ pressed }) => [styles.submit, pressed && styles.pressed]} onPress={() => void submit()} disabled={submitting}><Text style={styles.submitText}>{submitting ? 'Signing in…' : 'Enter my portal'}</Text><Ionicons name="arrow-forward" size={18} color="white"/></Pressable>
        <View style={styles.demo}><Text style={styles.demoTitle}>Account linking</Text><View style={styles.accountHelp}><Ionicons name="information-circle-outline" size={17} color={palette.blue}/><Text style={styles.accountHelpText}>Use the exact Teacher or Student Public ID from Web Management. The initial password for every account is <Text style={styles.demoRole}>1234</Text>.</Text></View></View>
      </View>
      <View style={styles.server}><View style={styles.serverDot}/><Text numberOfLines={1}>Institute API · {getApiBaseUrl()}</Text></View>
    </KeyboardAvoidingView>
  </SafeAreaView>;
}

const styles = StyleSheet.create({
  safe: { flex: 1, backgroundColor: palette.canvas },
  keyboard: { flex: 1, width: '100%', maxWidth: 540, alignSelf: 'center', overflow: 'hidden', paddingHorizontal: 24, justifyContent: 'center' },
  skyOrb: { position: 'absolute', width: 250, height: 250, borderRadius: 125, top: -100, right: -105, backgroundColor: palette.skyPale },
  goldOrb: { position: 'absolute', width: 160, height: 160, borderRadius: 80, bottom: -70, left: -80, backgroundColor: palette.goldPale },
  hero: { alignItems: 'flex-start', marginBottom: 26 },
  logoShell: { width: 72, height: 76, alignItems: 'center', justifyContent: 'center' },
  logoGlow: { position: 'absolute', width: 66, height: 66, borderRadius: 22, backgroundColor: '#FFFFFF', transform: [{ rotate: '7deg' }], ...shadow },
  logo: { width: 62, height: 68 },
  kicker: { color: palette.blue, fontSize: 12, letterSpacing: 1.6, fontWeight: '900', marginTop: 15 },
  title: { maxWidth: 390, color: palette.ink, fontSize: 34, lineHeight: 40, fontWeight: '900', letterSpacing: -1, marginTop: 8 },
  subtitle: { maxWidth: 400, color: palette.muted, fontSize: 15, lineHeight: 23, marginTop: 9 },
  form: { backgroundColor: 'white', borderRadius: radius.large, borderWidth: 1, borderColor: '#D7E5FA', padding: 23, gap: 15, ...shadow },
  formTitle: { color: palette.ink, fontSize: 22, fontWeight: '900' },
  formSubtitle: { color: palette.muted, fontSize: 13, lineHeight: 19, marginTop: 4 },
  field: { minHeight: 54, flexDirection: 'row', alignItems: 'center', paddingHorizontal: 15, gap: 11, backgroundColor: '#FFFFFF', borderWidth: 1, borderColor: '#CDD3DF', borderRadius: radius.small },
  input: { flex: 1, color: palette.ink, fontSize: 16, paddingVertical: 14 },
  error: { color: palette.red, fontSize: 14, lineHeight: 20 },
  submit: { minHeight: 54, borderRadius: radius.small, backgroundColor: palette.blue, flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 9, marginTop: 2 },
  submitText: { color: 'white', fontWeight: '900', fontSize: 15 },
  pressed: { opacity: 0.8 },
  demo: { borderTopWidth: 1, borderTopColor: palette.line, marginTop: 5, paddingTop: 10 },
  demoTitle: { color: palette.blueDark, fontSize: 11, textTransform: 'uppercase', letterSpacing: 0.9, fontWeight: '900', marginBottom: 6 },
  accountHelp: { minHeight: 42, flexDirection: 'row', alignItems: 'flex-start', gap: 8, paddingTop: 4 },
  accountHelpText: { flex: 1, color: palette.muted, fontSize: 13, lineHeight: 20 },
  demoRole: { color: palette.ink, fontWeight: '700' },
  server: { alignSelf: 'center', flexDirection: 'row', alignItems: 'center', gap: 6, maxWidth: '90%', marginTop: 16 },
  serverDot: { width: 7, height: 7, borderRadius: 7, backgroundColor: palette.green },
});
