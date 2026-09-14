import { Platform } from 'react-native';

export const palette = {
  ink: '#172033',
  muted: '#687287',
  blue: '#243CCF',
  blueDark: '#172982',
  bluePale: '#EEF1FF',
  sky: '#3E9FCE',
  skyPale: '#EAF7FC',
  gold: '#A97900',
  goldPale: '#FFF5D6',
  canvas: '#F3F5F9',
  panel: '#FFFFFF',
  line: '#E1E5EC',
  green: '#237A62',
  greenPale: '#EAF5F1',
  amber: '#9A6B19',
  amberPale: '#FAF3E5',
  red: '#B5474F',
  redPale: '#FAECEE',
  violet: '#69569B',
  violetDark: '#51417C',
  violetPale: '#F0EDF7',
} as const;

export const radius = { small: 8, medium: 12, large: 18, pill: 999 } as const;
export const shadow = Platform.select({
  web: { boxShadow: '0 8px 24px rgba(23,32,51,0.07)' },
  default: {
    shadowColor: '#172033',
    shadowOffset: { width: 0, height: 5 },
    shadowOpacity: 0.07,
    shadowRadius: 12,
    elevation: 2,
  },
}) ?? {};
