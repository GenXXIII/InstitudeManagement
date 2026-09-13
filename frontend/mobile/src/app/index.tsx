import { Redirect } from 'expo-router';
import { useAuth } from '@/features/auth/auth-context';

export default function Index() {
  const { session } = useAuth();
  if (!session) return <Redirect href="/sign-in"/>;
  return <Redirect href={session.role === 'teacher' ? '/(teacher)' : '/(student)'}/>;
}
