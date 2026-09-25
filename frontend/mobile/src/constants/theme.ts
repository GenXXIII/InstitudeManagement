import { Platform } from 'react-native';

export const palette = {
  ink: '#10233E',
  muted: '#61718A',
  blue: '#1648E8',
  blueDark: '#102FA9',
  bluePale: '#EDF3FF',
  sky: '#2B9FDF',
  skyPale: '#E8F7FE',
  gold: '#C58A00',
  goldPale: '#FFF5CF',
  canvas: '#F5F8FE',
  panel: '#FFFFFF',
  line: '#DCE6F4',
  green: '#168267',
  greenPale: '#E7F7F1',
  amber: '#A46A00',
  amberPale: '#FFF3DB',
  red: '#D74645',
  redPale: '#FDEDED',
  violet: '#6755B8',
  violetDark: '#4D3D91',
  violetPale: '#F0EEFF',
} as const;

export const radius = { small: 12, medium: 18, large: 26, pill: 999 } as const;
export const shadow = Platform.select({
  web: { boxShadow: '0 12px 34px rgba(16,47,169,0.10)' },
  default: {
    shadowColor: '#102FA9',
    shadowOffset: { width: 0, height: 8 },
    shadowOpacity: 0.10,
    shadowRadius: 18,
    elevation: 3,
  },
}) ?? {};
