import Ionicons from '@expo/vector-icons/Ionicons';
import { useState } from 'react';
import { Image, KeyboardAvoidingView, Platform, Pressable, StyleSheet, Text, TextInput, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { palette, radius, shadow } from '@/constants/theme';
import { useAuth } from '@/features/auth/auth-context';
import { getApiBaseUrl } from '@/features/portal/portal-api';

export default function SignInScreen() {
  const { signIn } = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [secure, setSecure] = useState(true);
  const [error, setError] = useState('');
  const [submitting, setSubmitting] = useState(false);

  async function submit() {
    setSubmitting(true);
    setError('');
    try { await signIn(email, password); }
    catch (reason) { setError(reason instanceof Error ? reason.message : 'Could not sign in.'); }
    finally { setSubmitting(false); }
  }

  function fillAccount(accountEmail: string) {
    setEmail(accountEmail);
    setPassword('1234');
    setError('');
  }

  return <SafeAreaView style={styles.safe}>
    <KeyboardAvoidingView style={styles.keyboard} behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
      <View style={styles.hero}><View style={styles.logoShell}><Image source={require('../../assets/images/ink-logo.png')} style={styles.logo} resizeMode="contain"/></View><Text style={styles.kicker}>INSTITUDE OF NEW KHMER</Text><Text style={styles.title}>Your institute, in your pocket.</Text><Text style={styles.subtitle}>Teacher and Student have separate mobile workspaces. Administrator stays in the web management system.</Text></View>
      <View style={styles.form}>
        <Text style={styles.formTitle}>Sign in</Text>
        <View style={styles.field}><Ionicons name="mail-outline" size={18} color={palette.muted}/><TextInput style={styles.input} value={email} onChangeText={setEmail} placeholder="Email address" placeholderTextColor="#98A6BA" keyboardType="email-address" autoCapitalize="none" autoCorrect={false} textContentType="username"/></View>
        <View style={styles.field}><Ionicons name="lock-closed-outline" size={18} color={palette.muted}/><TextInput style={styles.input} value={password} onChangeText={setPassword} placeholder="Password" placeholderTextColor="#98A6BA" secureTextEntry={secure} textContentType="password"/><Pressable hitSlop={12} onPress={() => setSecure(value => !value)}><Ionicons name={secure ? 'eye-outline' : 'eye-off-outline'} size={18} color={palette.muted}/></Pressable></View>
        {error ? <Text style={styles.error}>{error}</Text> : null}
        <Pressable style={({ pressed }) => [styles.submit, pressed && styles.pressed]} onPress={() => void submit()} disabled={submitting}><Text style={styles.submitText}>{submitting ? 'Signing in…' : 'Continue'}</Text><Ionicons name="arrow-forward" size={18} color="white"/></Pressable>
        <View style={styles.demo}><Text style={styles.demoTitle}>Demo accounts · password 1234</Text><Pressable style={styles.demoRow} onPress={() => fillAccount('teacher@gmail.com')}><Ionicons name="school-outline" size={17} color={palette.blue}/><Text><Text style={styles.demoRole}>Teacher</Text>  teacher@gmail.com</Text><Text style={styles.use}>Use</Text></Pressable><Pressable style={styles.demoRow} onPress={() => fillAccount('studnet@gmail.com')}><Ionicons name="person-outline" size={17} color={palette.violet}/><Text><Text style={styles.demoRole}>Student</Text>  studnet@gmail.com</Text><Text style={styles.use}>Use</Text></Pressable></View>
      </View>
      <View style={styles.server}><View style={styles.serverDot}/><Text numberOfLines={1}>Institute API · {getApiBaseUrl()}</Text></View>
    </KeyboardAvoidingView>
  </SafeAreaView>;
}

const styles = StyleSheet.create({
  safe: { flex: 1, backgroundColor: palette.canvas },
  keyboard: { flex: 1, paddingHorizontal: 20, justifyContent: 'center' },
  hero: { alignItems: 'center', marginBottom: 22 },
  logoShell: { width: 80, height: 80, borderRadius: 25, padding: 10, backgroundColor: 'white', borderWidth: 1, borderColor: palette.line, ...shadow },
  logo: { width: '100%', height: '100%' },
  kicker: { color: palette.blue, fontSize: 10, letterSpacing: 1.6, fontWeight: '900', marginTop: 15 },
  title: { color: palette.ink, fontSize: 27, lineHeight: 32, textAlign: 'center', fontWeight: '900', letterSpacing: -0.7, marginTop: 6 },
  subtitle: { maxWidth: 330, color: palette.muted, fontSize: 12, lineHeight: 18, textAlign: 'center', marginTop: 7 },
  form: { backgroundColor: 'white', borderRadius: radius.large, borderWidth: 1, borderColor: palette.line, padding: 18, gap: 11, ...shadow },
  formTitle: { color: palette.ink, fontSize: 18, fontWeight: '900', marginBottom: 2 },
  field: { minHeight: 50, flexDirection: 'row', alignItems: 'center', paddingHorizontal: 14, gap: 10, backgroundColor: '#F8FBFF', borderWidth: 1, borderColor: palette.line, borderRadius: radius.medium },
  input: { flex: 1, color: palette.ink, fontSize: 14, paddingVertical: 13 },
  error: { color: palette.red, fontSize: 11, lineHeight: 16 },
  submit: { minHeight: 50, borderRadius: radius.medium, backgroundColor: palette.blue, flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 8, marginTop: 1 },
  submitText: { color: 'white', fontWeight: '900', fontSize: 14 },
  pressed: { opacity: 0.8 },
  demo: { borderTopWidth: 1, borderTopColor: palette.line, marginTop: 5, paddingTop: 10 },
  demoTitle: { color: palette.muted, fontSize: 9, textTransform: 'uppercase', letterSpacing: 0.7, fontWeight: '800', marginBottom: 5 },
  demoRow: { minHeight: 35, flexDirection: 'row', alignItems: 'center', gap: 7 },
  demoRole: { color: palette.ink, fontWeight: '800' },
  use: { marginLeft: 'auto', color: palette.blue, fontSize: 11, fontWeight: '800' },
  server: { alignSelf: 'center', flexDirection: 'row', alignItems: 'center', gap: 6, maxWidth: '90%', marginTop: 16 },
  serverDot: { width: 7, height: 7, borderRadius: 7, backgroundColor: palette.green },
});
