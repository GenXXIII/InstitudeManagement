import Ionicons from '@expo/vector-icons/Ionicons';
import { useState } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { PortalPage } from '@/components/portal-ui';
import { palette, radius } from '@/constants/theme';
import type { MobileRole } from '@/features/auth/auth-context';
import { AttendanceContent } from './attendance-screen';
import { ScheduleContent } from './schedule-screen';

type ClassesView = 'attendance' | 'schedule';

const views = [
  { id: 'attendance', label: 'Attendance', icon: 'checkmark-circle-outline' },
  { id: 'schedule', label: 'Schedule', icon: 'calendar-outline' },
] as const;

export function ClassesScreen({ role }: { role: MobileRole }) {
  const [view, setView] = useState<ClassesView>('attendance');
  const accent = palette.blue;

  return <PortalPage
    title="Classes"
    subtitle={role === 'teacher' ? 'Review your teaching schedule and record class attendance in one place.' : 'Review your weekly schedule and personal attendance history.'}
  >
    <View style={styles.switcher}>
      {views.map(item => {
        const active = view === item.id;
        return <Pressable
          accessibilityRole="tab"
          accessibilityState={{ selected: active }}
          key={item.id}
          onPress={() => setView(item.id)}
          style={[styles.switcherItem, active && styles.switcherItemActive]}
        >
          <Ionicons name={item.icon} size={17} color={active ? accent : palette.muted}/>
          <Text style={[styles.switcherLabel, active && styles.switcherLabelActive]}>{item.label}</Text>
        </Pressable>;
      })}
    </View>
    {view === 'schedule' ? <ScheduleContent/> : <AttendanceContent role={role}/>} 
  </PortalPage>;
}

const styles = StyleSheet.create({
  switcher: { flexDirection: 'row', padding: 4, borderWidth: 1, borderColor: palette.line, borderRadius: radius.medium, backgroundColor: '#FFFFFF' },
  switcherItem: { minHeight: 45, flex: 1, flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 8, borderRadius: radius.small },
  switcherItemActive: { backgroundColor: palette.bluePale },
  switcherLabel: { color: palette.muted, fontSize: 12, fontWeight: '700' },
  switcherLabelActive: { color: palette.blueDark, fontWeight: '800' },
});
