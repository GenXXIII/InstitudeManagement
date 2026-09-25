import Ionicons from '@expo/vector-icons/Ionicons';
import { Tabs } from 'expo-router';
import type { BottomTabBarProps } from 'expo-router/build/react-navigation/bottom-tabs';
import { useEffect, useState } from 'react';
import { Animated, Easing, Pressable, StyleSheet, Text, View } from 'react-native';
import type { MobileRole } from '@/features/auth/auth-context';
import { PortalProvider, usePortal } from '@/features/portal/portal-context';
import { palette } from '@/constants/theme';

type TabDefinition = { label: string; icon: keyof typeof Ionicons.glyphMap; selectedIcon: keyof typeof Ionicons.glyphMap };
type TabRouteDefinition = { name: string; tab: TabDefinition };

const homeTab = { label: 'Home', icon: 'home-outline', selectedIcon: 'home' } satisfies TabDefinition;
const classesTab = { label: 'Classes', icon: 'book-outline', selectedIcon: 'book' } satisfies TabDefinition;
const assessmentTab = { label: 'Assessment', icon: 'clipboard-outline', selectedIcon: 'clipboard' } satisfies TabDefinition;
const resultsTab = { label: 'Results', icon: 'ribbon-outline', selectedIcon: 'ribbon' } satisfies TabDefinition;
const notificationsTab = { label: 'Inbox', icon: 'notifications-outline', selectedIcon: 'notifications' } satisfies TabDefinition;
const financeTab = { label: 'Finance', icon: 'card-outline', selectedIcon: 'card' } satisfies TabDefinition;
const profileTab = { label: 'Profile', icon: 'person-outline', selectedIcon: 'person' } satisfies TabDefinition;

const teacherTabRoutes = [
  { name: 'index', tab: homeTab },
  { name: 'classes', tab: classesTab },
  { name: 'assessment', tab: assessmentTab },
  { name: 'notifications', tab: notificationsTab },
  { name: 'profile', tab: profileTab },
] satisfies TabRouteDefinition[];

const studentTabRoutes = [
  { name: 'index', tab: homeTab },
  { name: 'classes', tab: classesTab },
  { name: 'results', tab: resultsTab },
  { name: 'finance', tab: financeTab },
  { name: 'notifications', tab: notificationsTab },
  { name: 'profile', tab: profileTab },
] satisfies TabRouteDefinition[];

export function RoleTabs({ role }: { role: MobileRole }) {
  return <PortalProvider role={role}>{role === 'teacher' ? <TeacherTabs/> : <StudentTabs/>}</PortalProvider>;
}

function TeacherTabs() {
  const unread = useUnreadCount();
  return <Tabs screenOptions={navigatorOptions()} tabBar={props => <ExpandableTabBar {...props} tabs={teacherTabRoutes} unread={unread}/> }>
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
  return <Tabs screenOptions={navigatorOptions()} tabBar={props => <ExpandableTabBar {...props} tabs={studentTabRoutes} unread={unread}/> }>
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
    tabBarHideOnKeyboard: true,
  };
}

function tabOptions(tab: TabDefinition, unread = 0) {
  return {
    title: tab.label,
    tabBarBadge: unread || undefined,
  };
}

function ExpandableTabBar({ state, descriptors, navigation, insets, tabs, unread }: BottomTabBarProps & { tabs: readonly TabRouteDefinition[]; unread: number }) {
  return <View style={[styles.bar, { height: 48 + insets.bottom }]}>
    <View pointerEvents="none" style={styles.barSurface}/>
    <View style={styles.tabRow}>
      {tabs.map(({ name, tab }) => {
        const routeIndex = state.routes.findIndex(route => route.name === name);
        const route = state.routes[routeIndex];
        if (!route) return null;
        const focused = state.index === routeIndex;
        const options = descriptors[route.key]?.options;
        const onPress = () => {
          const event = navigation.emit({ type: 'tabPress', target: route.key, canPreventDefault: true });
          if (!focused && !event.defaultPrevented) navigation.navigate(route.name, route.params);
        };
        const onLongPress = () => navigation.emit({ type: 'tabLongPress', target: route.key });

        return <ExpandableTabButton
          accessibilityLabel={options?.tabBarAccessibilityLabel || `${tab.label} tab`}
          badge={name === 'notifications' ? unread : 0}
          focused={focused}
          key={route.key}
          onLongPress={onLongPress}
          onPress={onPress}
          tab={tab}
        />;
      })}
    </View>
  </View>;
}

function ExpandableTabButton({ tab, focused, badge, accessibilityLabel, onPress, onLongPress }: { tab: TabDefinition; focused: boolean; badge: number; accessibilityLabel: string; onPress: () => void; onLongPress: () => void }) {
  const [progress] = useState(() => new Animated.Value(focused ? 1 : 0));

  useEffect(() => {
    Animated.timing(progress, {
      toValue: focused ? 1 : 0,
      duration: 130,
      easing: Easing.out(Easing.quad),
      useNativeDriver: false,
    }).start();
  }, [focused, progress]);

  const backgroundTop = progress.interpolate({ inputRange: [0, 1], outputRange: [0, -8] });
  const iconLift = progress.interpolate({ inputRange: [0, 1], outputRange: [0, -3] });
  const iconScale = progress.interpolate({ inputRange: [0, 1], outputRange: [1, 1.06] });
  const labelWidth = progress.interpolate({ inputRange: [0, 1], outputRange: [0, Math.max(38, Math.min(72, tab.label.length * 6.8))] });
  const labelHeight = progress.interpolate({ inputRange: [0, 1], outputRange: [0, 14] });
  const labelOpacity = progress.interpolate({ inputRange: [0.35, 1], outputRange: [0, 1], extrapolate: 'clamp' });
  const labelTranslate = progress.interpolate({ inputRange: [0, 1], outputRange: [-3, 0] });

  return <Animated.View style={styles.item}>
    <Pressable
      accessibilityLabel={accessibilityLabel}
      accessibilityRole="button"
      accessibilityState={{ selected: focused }}
      onLongPress={onLongPress}
      onPress={onPress}
      style={({ pressed }) => [styles.tabButton, pressed && styles.pressed]}
    >
      <Animated.View style={[styles.activeBackground, { top: backgroundTop, opacity: progress }]}/>
      <Animated.View style={styles.focusContent}>
        <Animated.View style={[styles.iconBox, { transform: [{ translateY: iconLift }, { scale: iconScale }] }]}>
          <Ionicons name={focused ? tab.selectedIcon : tab.icon} size={24} color={focused ? palette.blue : '#74849C'}/>
          {badge ? <View style={styles.badge}><Text style={styles.badgeText}>{Math.min(badge, 9)}</Text></View> : null}
        </Animated.View>
        <Animated.View style={[styles.iconLabelClip, { width: labelWidth, height: labelHeight, opacity: labelOpacity, transform: [{ translateY: labelTranslate }] }]}>
          <Text numberOfLines={1} style={styles.iconLabel}>{tab.label}</Text>
        </Animated.View>
      </Animated.View>
    </Pressable>
  </Animated.View>;
}

const styles = StyleSheet.create({
  bar: { position: 'relative', backgroundColor: palette.canvas },
  barSurface: { position: 'absolute', top: 14, left: 10, right: 10, height: 56, borderRadius: 20, borderWidth: 1, borderColor: palette.line, backgroundColor: '#FFFFFF', shadowColor: palette.blueDark, shadowOffset: { width: 0, height: -3 }, shadowOpacity: 0.08, shadowRadius: 10, elevation: 6 },
  tabRow: { height: 48, flexDirection: 'row', alignItems: 'center', gap: 2, paddingHorizontal: 11, transform: [{ translateY: 18 }] },
  item: { minWidth: 0, flex: 1, height: 42 },
  tabButton: { width: '100%', height: 42, overflow: 'visible', alignItems: 'center', justifyContent: 'center', paddingHorizontal: 2, borderRadius: 14 },
  activeBackground: { position: 'absolute', zIndex: 1, left: -1, right: -1, bottom: -7, borderRadius: 14, borderWidth: 1, borderColor: '#C9DAFF', backgroundColor: palette.bluePale, shadowColor: palette.blueDark, shadowOffset: { width: 0, height: 2 }, shadowOpacity: 0.09, shadowRadius: 3, elevation: 2 },
  focusContent: { zIndex: 2, alignItems: 'center', justifyContent: 'center' },
  iconBox: { width: 24, height: 24, alignItems: 'center', justifyContent: 'center' },
  iconLabelClip: { overflow: 'hidden' },
  iconLabel: { color: palette.blueDark, fontSize: 11, lineHeight: 13, fontWeight: '900', textAlign: 'center' },
  badge: { position: 'absolute', top: -5, right: -7, minWidth: 17, height: 17, alignItems: 'center', justifyContent: 'center', borderRadius: 9, paddingHorizontal: 4, backgroundColor: palette.gold, borderWidth: 2, borderColor: '#FFFFFF' },
  badgeText: { color: '#FFFFFF', fontSize: 9, fontWeight: '900' },
  pressed: { opacity: 0.68 },
});
