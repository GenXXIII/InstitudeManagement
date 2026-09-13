import Ionicons from '@expo/vector-icons/Ionicons';
import { Tabs } from 'expo-router';
import type { MobileRole } from '@/features/auth/auth-context';
import { PortalProvider } from '@/features/portal/portal-context';
import { palette } from '@/constants/theme';

type TabDefinition = { name: string; label: string; icon: keyof typeof Ionicons.glyphMap; selectedIcon: keyof typeof Ionicons.glyphMap };

const tabs: TabDefinition[] = [
  { name: 'index', label: 'Home', icon: 'home-outline', selectedIcon: 'home' },
  { name: 'schedule', label: 'Schedule', icon: 'calendar-outline', selectedIcon: 'calendar' },
  { name: 'attendance', label: 'Attendance', icon: 'checkmark-circle-outline', selectedIcon: 'checkmark-circle' },
  { name: 'results', label: 'Results', icon: 'ribbon-outline', selectedIcon: 'ribbon' },
  { name: 'profile', label: 'Account', icon: 'person-circle-outline', selectedIcon: 'person-circle' },
];

export function RoleTabs({ role }: { role: MobileRole }) {
  return <PortalProvider role={role}><Tabs screenOptions={{
    headerShown: false,
    tabBarActiveTintColor: palette.blue,
    tabBarInactiveTintColor: '#8997AA',
    tabBarLabelStyle: { fontSize: 9, fontWeight: '700', paddingTop: 1 },
    tabBarStyle: { height: 78, paddingTop: 7, backgroundColor: '#FFFFFF', borderTopColor: palette.line },
  }}>{tabs.map(tab => <Tabs.Screen name={tab.name} key={tab.name} options={{ title: role === 'teacher' && tab.name === 'results' ? 'Gradebook' : tab.label, tabBarIcon: ({ color, focused }) => <Ionicons name={focused ? tab.selectedIcon : tab.icon} size={21} color={color}/> }}/>)}</Tabs></PortalProvider>;
}
