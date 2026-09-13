import { Platform } from 'react-native';

export const palette = {
  ink: '#17223B',
  muted: '#71809F',
  blue: '#2768F4',
  blueDark: '#1554DF',
  bluePale: '#EDF5FF',
  canvas: '#F4F9FF',
  panel: '#FFFFFF',
  line: '#E1EAF5',
  green: '#219B70',
  greenPale: '#E8F8F1',
  amber: '#D9951F',
  amberPale: '#FFF5DE',
  red: '#D94E5D',
  redPale: '#FFF0F2',
  violet: '#7055E8',
} as const;

export const radius = { small: 10, medium: 16, large: 24, pill: 999 } as const;
export const shadow = Platform.select({
  web: { boxShadow: '0 8px 18px rgba(52,91,141,0.10)' },
  default: {
    shadowColor: '#345B8D',
    shadowOffset: { width: 0, height: 8 },
    shadowOpacity: 0.1,
    shadowRadius: 18,
    elevation: 3,
  },
}) ?? {};
