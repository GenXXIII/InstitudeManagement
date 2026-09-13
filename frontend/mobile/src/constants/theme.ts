import { Platform } from 'react-native';

export const palette = {
  ink: '#17223B',
  muted: '#71809F',
  blue: '#2768F4',
  blueDark: '#1554DF',
  bluePale: '#EDF5FF',
  canvas: '#F6F7F9',
  panel: '#FFFFFF',
  line: '#E7EDF7',
  green: '#28A97B',
  greenPale: '#EAF8F4',
  amber: '#E6A52E',
  amberPale: '#FFF8E8',
  red: '#E05B67',
  redPale: '#FFF0F1',
  violet: '#7659ED',
  violetDark: '#6047D5',
  violetPale: '#F0EDFF',
} as const;

export const radius = { small: 5, medium: 8, large: 10, pill: 999 } as const;
export const shadow = Platform.select({
  web: { boxShadow: '0 1px 3px rgba(23,34,59,0.08)' },
  default: {
    shadowColor: '#17223B',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.08,
    shadowRadius: 3,
    elevation: 1,
  },
}) ?? {};
