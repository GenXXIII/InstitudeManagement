import Ionicons from '@expo/vector-icons/Ionicons';
import { Tabs } from 'expo-router';
import { StyleSheet, View, type ColorValue } from 'react-native';
import type { MobileRole } from '@/features/auth/auth-context';
import { PortalProvider, usePortal } from '@/features/portal/portal-context';
import { palette } from '@/constants/theme';

type TabDefinition = { label: string; icon: keyof typeof Ionicons.glyphMap; selectedIcon: keyof typeof Ionicons.glyphMap };

const homeTab = { label: 'Home', icon: 'home-outline', selectedIcon: 'home' } satisfies TabDefinition;
const classesTab = { label: 'Classes', icon: 'book-outline', selectedIcon: 'book' } satisfies TabDefinition;
const assessmentTab = { label: 'Assessment', icon: 'clipboard-outline', selectedIcon: 'clipboard' } satisfies TabDefinition;
const resultsTab = { label: 'Results', icon: 'ribbon-outline', selectedIcon: 'ribbon' } satisfies TabDefinition;
const notificationsTab = { label: 'Notifications', icon: 'notifications-outline', selectedIcon: 'notifications' } satisfies TabDefinition;
const financeTab = { label: 'Finance', icon: 'card-outline', selectedIcon: 'card' } satisfies TabDefinition;
const profileTab = { label: 'Profile', icon: 'person-outline', selectedIcon: 'person' } satisfies TabDefinition;

export function RoleTabs({ role }: { role: MobileRole }) {
  return <PortalProvider role={role}>{role === 'teacher' ? <TeacherTabs/> : <StudentTabs/>}</PortalProvider>;
}

function TeacherTabs() {
  const unread = useUnreadCount();
  return <Tabs screenOptions={navigatorOptions()}>
    <Tabs.Screen name="index" options={tabOptions(homeTab)}/>
    <Tabs.Screen name="classes" options={tabOptions(classesTab)}/>
    <Tabs.Screen name="assessment" options={tabOptions(assessmentTab)}/>
    <Tabs.Screen name="notifications" options={tabOptions(notificationsTab, unread)}/>
    <Tabs.Screen name="profile" options={tabOptions(profileTab)}/>
    <Tabs.Screen name="schedule" options={{ href: null }}/>
    <Tabs.Screen name="attendance" options={{ href: null }}/>
    <Tabs.Screen name="results" options={{ href: null }}/>
  </Tabs>;
}

function StudentTabs() {
  const unread = useUnreadCount();
  return <Tabs screenOptions={navigatorOptions()}>
    <Tabs.Screen name="index" options={tabOptions(homeTab)}/>
    <Tabs.Screen name="classes" options={tabOptions(classesTab)}/>
    <Tabs.Screen name="results" options={tabOptions(resultsTab)}/>
    <Tabs.Screen name="finance" options={tabOptions(financeTab)}/>
    <Tabs.Screen name="notifications" options={tabOptions(notificationsTab, unread)}/>
    <Tabs.Screen name="profile" options={tabOptions(profileTab)}/>
    <Tabs.Screen name="schedule" options={{ href: null }}/>
    <Tabs.Screen name="attendance" options={{ href: null }}/>
  </Tabs>;
}

function useUnreadCount() {
  return usePortal().announcements.filter(item => !item.isRead).length;
}

function navigatorOptions() {
  return {
    headerShown: false,
    tabBarActiveTintColor: palette.blue,
    tabBarInactiveTintColor: '#8997AA',
    tabBarHideOnKeyboard: true,
    tabBarLabelStyle: styles.label,
    tabBarItemStyle: styles.item,
    tabBarStyle: styles.bar,
  };
}

function tabOptions(tab: TabDefinition, unread = 0) {
  return {
    title: tab.label,
    tabBarBadge: unread || undefined,
    tabBarBadgeStyle: styles.badge,
    tabBarIcon: ({ color, focused }: { color: ColorValue; focused: boolean }) => <View style={[styles.icon, focused && styles.iconActive]}><Ionicons name={focused ? tab.selectedIcon : tab.icon} size={20} color={color}/></View>,
  };
}

const styles = StyleSheet.create({
  bar: { height: 78, paddingTop: 8, paddingBottom: 8, backgroundColor: '#FFFFFF', borderTopWidth: 1, borderTopColor: palette.line },
  item: { paddingVertical: 1 },
  label: { fontSize: 10, fontWeight: '700', paddingTop: 2 },
  icon: { width: 38, height: 32, borderRadius: 10, alignItems: 'center', justifyContent: 'center' },
  iconActive: { backgroundColor: palette.bluePale },
  badge: { minWidth: 18, height: 18, borderRadius: 9, paddingHorizontal: 4, fontSize: 8, fontWeight: '800', backgroundColor: palette.gold, color: 'white' },
});
