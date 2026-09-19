import Ionicons from '@expo/vector-icons/Ionicons';
import { CameraView, useCameraPermissions, type BarcodeScanningResult } from 'expo-camera';
import { useMemo, useState } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import QRCode from 'react-native-qrcode-svg';
import { Card, EmptyBlock, MetricCard, PortalPage, portalStyles, SectionHeading } from '@/components/portal-ui';
import { palette, radius } from '@/constants/theme';
import type { StudentPayment } from '../portal-types';
import { usePortal } from '../portal-context';

export function FinanceScreen() {
  const portal = usePortal();
  const [cameraPermission, requestCameraPermission] = useCameraPermissions();
  const [scanning, setScanning] = useState<StudentPayment>();
  const [submittingId, setSubmittingId] = useState('');
  const [error, setError] = useState('');
  const payments = useMemo(() => [...portal.payments].sort((left, right) => {
    const priority = { Pending: 0, Partial: 0, Refunded: 0, Paid: 1, Cancelled: 2 };
    if (left.status !== right.status) return priority[left.status] - priority[right.status];
    return `${right.academicYear}-${right.semester}`.localeCompare(`${left.academicYear}-${left.semester}`, undefined, { numeric: true });
  }), [portal.payments]);
  const pending = payments.filter(payment => payment.status !== 'Paid' && payment.status !== 'Cancelled');
  const paid = payments.filter(payment => payment.status === 'Paid');

  async function confirm(payment: StudentPayment, qrPayload: string) {
    if (submittingId) return;
    setSubmittingId(payment.id);
    setError('');
    try {
      await portal.confirmPayment(payment.id, qrPayload);
      setScanning(undefined);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Could not confirm the simulated payment.');
    } finally {
      setSubmittingId('');
    }
  }

  async function openScanner(payment: StudentPayment) {
    setError('');
    if (!cameraPermission?.granted) {
      const permission = await requestCameraPermission();
      if (!permission.granted) {
        setError('Camera permission is required because payment can only be confirmed by scanning the institute payment QR.');
        return;
      }
    }
    setScanning(payment);
  }

  function scan(result: BarcodeScanningResult) {
    if (!scanning || submittingId) return;
    void confirm(scanning, result.data);
  }

  if (scanning) return <PortalPage title="Scan payment QR" subtitle={`${scanning.academicYear} · ${scanning.semester}`} eyebrow="Student Finance">
    <Card style={styles.scannerCard}>
      <View style={styles.cameraFrame}>
        <CameraView style={StyleSheet.absoluteFill} facing="back" barcodeScannerSettings={{ barcodeTypes: ['qr'] }} onBarcodeScanned={submittingId ? undefined : scan}/>
        <View style={styles.scanGuide}><View/><View/><View/><View/></View>
      </View>
      <Text style={styles.scanTitle}>{submittingId ? 'Confirming payment…' : 'Point the camera at the Administrator Finance QR'}</Text>
      <Text style={styles.scanDetail}>Only the QR for {scanning.paymentCode} will be accepted.</Text>
      {error ? <Text style={styles.error}>{error}</Text> : null}
      <Pressable disabled={Boolean(submittingId)} onPress={() => setScanning(undefined)} style={({ pressed }) => [styles.secondaryButton, pressed && styles.pressed]}><Text style={styles.secondaryButtonText}>Cancel scan</Text></Pressable>
    </Card>
  </PortalPage>;

  return <PortalPage title="Finance" subtitle="Pay a Bakong KHQR with your banking app, then verify it here. Enrollment advances when Finance reports Paid." eyebrow="Student Finance">
    <View style={portalStyles.grid}>
      <MetricCard icon="time-outline" label="Pending" value={pending.length} tone="amber"/>
      <MetricCard icon="checkmark-circle-outline" label="Paid" value={paid.length} tone="green"/>
    </View>
    {error ? <Text style={styles.error}>{error}</Text> : null}
    <SectionHeading title="Payment declarations" detail={`${payments.length} cards`}/>
    {payments.length ? <View style={portalStyles.stack}>{payments.map(payment => <PaymentCard payment={payment} busy={submittingId === payment.id} onPay={() => payment.qrProvider === 'Bakong KHQR' ? void confirm(payment, payment.qrPayload) : void openScanner(payment)} key={payment.id}/>)}</View> : <EmptyBlock icon="card-outline" title="No payment declared" detail="A payment card appears here only after Administrator declares its title, plan, amount, dates, and QR."/>}
    <Card style={styles.policyCard}>
      <View style={styles.policyIcon}><Ionicons name="shield-checkmark-outline" size={20} color={palette.blue}/></View>
      <View style={styles.policyCopy}><Text style={styles.policyTitle}>Bakong verification</Text><Text style={styles.policyDetail}>For Bakong KHQR, pay from a Bakong-compatible banking app first, then tap Verify. The API accepts only the matching recipient, amount, currency, and unused transaction.</Text></View>
    </Card>
  </PortalPage>;
}

function PaymentCard({ payment, busy, onPay }: { payment: StudentPayment; busy: boolean; onPay: () => void }) {
  const paid = payment.status === 'Paid';
  const cancelled = payment.status === 'Cancelled';
  const expired = payment.isExpired || payment.isQrExpired;
  const statusColor = paid ? palette.green : cancelled || expired ? palette.red : payment.status === 'Refunded' ? '#755BC4' : '#9B6812';
  const displayStatus = expired ? 'Expired' : payment.status;
  const coverage = payment.paymentPlan === 'Year' ? 'Semester 1 + Semester 2' : payment.semester;
  return <Card style={[styles.paymentCard, paid && styles.paymentCardPaid, (cancelled || expired) && styles.paymentCardCancelled]}>
    <View style={styles.declarationTop}><View style={styles.planPill}><Ionicons name={payment.paymentPlan === 'Year' ? 'layers-outline' : 'calendar-outline'} size={13} color={palette.blue}/><Text style={styles.planText}>{payment.paymentPlan}</Text></View><Text style={styles.createdText}>{payment.qrProvider} · {formatDate(payment.declaredAtUtc)}</Text></View>
    <Text style={styles.declarationTitle}>{payment.title}</Text>
    <Text style={styles.studentName}>{payment.studentName}</Text>
    <View style={styles.paymentHeader}>
      <View style={[styles.paymentIcon, paid ? styles.paymentIconPaid : styles.paymentIconPending]}><Ionicons name={paid ? 'checkmark-circle-outline' : 'card-outline'} size={22} color={paid ? palette.green : palette.gold}/></View>
      <View style={styles.paymentHeading}><Text style={styles.paymentPeriod}>{coverage}</Text><Text style={styles.paymentCode}>{payment.paymentCode} · {payment.paymentPlan === 'Year' ? payment.academicYear : 'Semester payment'}</Text></View>
      <View style={[styles.statusBadge, paid ? styles.statusPaid : styles.statusPending]}><Text style={[styles.statusText, { color: statusColor }]}>{displayStatus}</Text></View>
    </View>
    <Text style={styles.amount}>{money(payment.balance, payment.currency)}</Text>
    <Text style={styles.amountLabel}>{paid ? `Paid ${money(payment.totalPaid, payment.currency)}` : `Remaining of ${money(payment.totalDue, payment.currency)}`}</Text>
    {!paid && !cancelled && !expired && payment.qrPayload ? <View style={styles.qrTicket}>
      <View style={styles.qrTicketHeading}><View><Text style={styles.qrEyebrow}>{payment.qrProvider}</Text><Text style={styles.qrTitle}>Scan to pay</Text></View><Ionicons name="shield-checkmark-outline" size={24} color={palette.blue}/></View>
      <View style={styles.qrFrame}><QRCode value={payment.qrPayload} size={156} color={palette.ink} backgroundColor="white"/></View>
      <Text style={styles.qrAmount}>{money(payment.balance, payment.currency)}</Text>
      <Text style={styles.qrDetail}>{payment.financialAccountCode} · {coverage}</Text>
      <Text style={styles.qrHint}>{payment.qrProvider === 'Bakong KHQR' ? 'Open a Bakong-compatible banking app and scan this KHQR.' : 'This test QR can be confirmed only by the signed-in student account.'}</Text>
    </View> : null}
    <View style={styles.paymentMeta}><View><Text style={styles.metaLabel}>Due</Text><Text style={styles.metaValue}>{formatDate(payment.dueOn)}</Text></View><View><Text style={styles.metaLabel}>{payment.qrProvider === 'Bakong KHQR' ? 'Bakong QR expires' : 'Request expires'}</Text><Text style={styles.metaValue}>{formatDateTime(payment.qrExpiresAtUtc ?? payment.expiresAtUtc)}</Text></View></View>
    {paid ? <View style={styles.confirmedLine}><Ionicons name="person-circle-outline" size={16} color={palette.green}/><Text>{payment.confirmationMethod || 'Payment completed'}{payment.paidAtUtc ? ` · ${formatDate(payment.paidAtUtc)}` : ''}</Text></View> : cancelled ? <View style={styles.cancelledLine}><Text>This financial account was cancelled. No QR payment is available.</Text></View> : <View style={styles.paymentActions}>
      <Pressable disabled={busy || expired} onPress={onPay} style={({ pressed }) => [styles.primaryButton, pressed && styles.pressed, (busy || expired) && styles.disabled]}><Ionicons name={payment.qrProvider === 'Bakong KHQR' ? 'shield-checkmark-outline' : 'qr-code-outline'} size={17} color="white"/><Text style={styles.primaryButtonText}>{expired ? 'QR expired — contact Finance' : busy ? 'Confirming…' : payment.qrProvider === 'Bakong KHQR' ? 'Verify Bakong payment' : 'Scan administrator QR to pay'}</Text></Pressable>
    </View>}
  </Card>;
}

function money(value: number, currency: string) {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency, maximumFractionDigits: currency === 'KHR' ? 0 : 2 }).format(value);
}

function formatDate(value: string) {
  const date = new Date(value);
  return Number.isNaN(date.valueOf()) ? value : new Intl.DateTimeFormat('en-GB', { day: '2-digit', month: 'short', year: 'numeric' }).format(date);
}

function formatDateTime(value: string) {
  const date = new Date(value);
  return Number.isNaN(date.valueOf()) ? value : new Intl.DateTimeFormat('en-GB', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' }).format(date);
}

const styles = StyleSheet.create({
  error: { padding: 11, borderRadius: radius.small, backgroundColor: palette.redPale, color: palette.red, fontSize: 12, lineHeight: 18, fontWeight: '700' },
  paymentCard: { gap: 14, borderLeftWidth: 4, borderLeftColor: palette.gold },
  paymentCardPaid: { borderLeftColor: palette.green },
  paymentCardCancelled: { borderLeftColor: palette.red },
  declarationTop: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: 9 },
  planPill: { flexDirection: 'row', alignItems: 'center', gap: 5, paddingHorizontal: 9, paddingVertical: 5, borderRadius: radius.pill, backgroundColor: palette.bluePale },
  planText: { color: palette.blue, fontSize: 9, fontWeight: '800' },
  createdText: { color: palette.muted, fontSize: 9, fontWeight: '600' },
  declarationTitle: { color: palette.ink, fontSize: 18, lineHeight: 23, fontWeight: '800' },
  studentName: { color: palette.blue, fontSize: 11, fontWeight: '700', marginTop: -8 },
  paymentHeader: { flexDirection: 'row', alignItems: 'center', gap: 10 },
  paymentIcon: { width: 43, height: 43, borderRadius: 12, alignItems: 'center', justifyContent: 'center' },
  paymentIconPending: { backgroundColor: palette.goldPale },
  paymentIconPaid: { backgroundColor: palette.greenPale },
  paymentHeading: { flex: 1 },
  paymentPeriod: { color: palette.ink, fontSize: 14, fontWeight: '800' },
  paymentCode: { color: palette.muted, fontSize: 10, fontWeight: '700', marginTop: 3 },
  statusBadge: { paddingHorizontal: 9, paddingVertical: 6, borderRadius: radius.pill },
  statusPending: { backgroundColor: palette.goldPale },
  statusPaid: { backgroundColor: palette.greenPale },
  statusText: { fontSize: 10, fontWeight: '800' },
  amount: { color: palette.blueDark, fontSize: 29, lineHeight: 34, fontWeight: '800' },
  amountLabel: { color: palette.muted, fontSize: 10, fontWeight: '600', marginTop: -10 },
  qrTicket: { alignItems: 'center', gap: 8, padding: 14, borderWidth: 1, borderColor: '#D8E4F2', borderRadius: radius.medium, backgroundColor: '#F8FBFF' },
  qrTicketHeading: { width: '100%', flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
  qrEyebrow: { color: palette.blue, fontSize: 9, fontWeight: '900', textTransform: 'uppercase', letterSpacing: 0.8 },
  qrTitle: { marginTop: 2, color: palette.ink, fontSize: 16, fontWeight: '800' },
  qrFrame: { marginTop: 3, padding: 11, borderRadius: 14, backgroundColor: 'white', borderWidth: 1, borderColor: '#E3EAF3' },
  qrAmount: { color: palette.ink, fontSize: 20, fontWeight: '900' },
  qrDetail: { color: palette.blue, fontSize: 10, fontWeight: '800' },
  qrHint: { maxWidth: 260, color: palette.muted, fontSize: 9, lineHeight: 14, fontWeight: '600', textAlign: 'center' },
  paymentMeta: { flexDirection: 'row', justifyContent: 'space-between', gap: 12, paddingTop: 12, borderTopWidth: StyleSheet.hairlineWidth, borderTopColor: palette.line },
  metaLabel: { color: palette.muted, fontSize: 8, fontWeight: '700', textTransform: 'uppercase' },
  metaValue: { color: palette.ink, fontSize: 10, fontWeight: '700', marginTop: 3 },
  paymentActions: { flexDirection: 'row', gap: 9 },
  primaryButton: { minHeight: 45, flex: 1, flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 7, borderRadius: radius.small, backgroundColor: palette.blue },
  primaryButtonText: { color: 'white', fontSize: 12, fontWeight: '800' },
  secondaryButton: { minHeight: 45, flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 7, paddingHorizontal: 20, borderRadius: radius.small, borderWidth: 1, borderColor: '#CADAF4', backgroundColor: palette.bluePale },
  secondaryButtonText: { color: palette.blue, fontSize: 12, fontWeight: '800' },
  confirmedLine: { flexDirection: 'row', alignItems: 'center', gap: 7, padding: 10, borderRadius: radius.small, backgroundColor: palette.greenPale },
  cancelledLine: { padding: 10, borderRadius: radius.small, backgroundColor: palette.redPale },
  policyCard: { flexDirection: 'row', alignItems: 'flex-start', gap: 11, backgroundColor: palette.bluePale, shadowOpacity: 0, elevation: 0 },
  policyIcon: { width: 38, height: 38, borderRadius: 10, alignItems: 'center', justifyContent: 'center', backgroundColor: 'white' },
  policyCopy: { flex: 1 },
  policyTitle: { color: palette.ink, fontSize: 13, fontWeight: '800' },
  policyDetail: { color: palette.muted, fontSize: 11, lineHeight: 17, marginTop: 4 },
  scannerCard: { alignItems: 'center', padding: 16 },
  cameraFrame: { width: '100%', aspectRatio: 1, overflow: 'hidden', borderRadius: radius.medium, backgroundColor: '#071329' },
  scanGuide: { position: 'absolute', top: 56, right: 56, bottom: 56, left: 56, borderWidth: 2, borderColor: 'rgba(255,255,255,.85)', borderRadius: 18 },
  scanTitle: { marginTop: 18, color: palette.ink, fontSize: 15, fontWeight: '800', textAlign: 'center' },
  scanDetail: { marginTop: 5, marginBottom: 14, color: palette.muted, fontSize: 11, textAlign: 'center' },
  pressed: { opacity: 0.7 },
  disabled: { opacity: 0.5 },
});
