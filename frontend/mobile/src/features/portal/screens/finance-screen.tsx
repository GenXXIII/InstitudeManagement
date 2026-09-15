import Ionicons from '@expo/vector-icons/Ionicons';
import { CameraView, useCameraPermissions, type BarcodeScanningResult } from 'expo-camera';
import { useMemo, useState } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
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

  return <PortalPage title="Finance" subtitle="Scan the Administrator-generated QR to pay the current balance. Enrollment advances when Finance reports Paid." eyebrow="Student Finance">
    <View style={portalStyles.grid}>
      <MetricCard icon="time-outline" label="Pending" value={pending.length} tone="amber"/>
      <MetricCard icon="checkmark-circle-outline" label="Paid" value={paid.length} tone="green"/>
    </View>
    {error ? <Text style={styles.error}>{error}</Text> : null}
    <SectionHeading title="Semester payments" detail={`${payments.length} records`}/>
    {payments.length ? <View style={portalStyles.stack}>{payments.map(payment => <PaymentCard payment={payment} busy={submittingId === payment.id} onScan={() => void openScanner(payment)} key={payment.id}/>)}</View> : <EmptyBlock icon="card-outline" title="No payment record" detail="A semester payment appears after Administrator completes Student Enrollment."/>}
    <Card style={styles.policyCard}>
      <View style={styles.policyIcon}><Ionicons name="shield-checkmark-outline" size={20} color={palette.blue}/></View>
      <View style={styles.policyCopy}><Text style={styles.policyTitle}>Student QR scan only</Text><Text style={styles.policyDetail}>This is a fake finance workflow. Administrator generates the QR, and only the matching student scan can confirm payment.</Text></View>
    </Card>
  </PortalPage>;
}

function PaymentCard({ payment, busy, onScan }: { payment: StudentPayment; busy: boolean; onScan: () => void }) {
  const paid = payment.status === 'Paid';
  const cancelled = payment.status === 'Cancelled';
  const statusColor = paid ? palette.green : cancelled ? palette.red : payment.status === 'Refunded' ? '#755BC4' : '#9B6812';
  return <Card style={[styles.paymentCard, paid && styles.paymentCardPaid, cancelled && styles.paymentCardCancelled]}>
    <View style={styles.paymentHeader}>
      <View style={[styles.paymentIcon, paid ? styles.paymentIconPaid : styles.paymentIconPending]}><Ionicons name={paid ? 'checkmark-circle-outline' : 'card-outline'} size={22} color={paid ? palette.green : palette.gold}/></View>
      <View style={styles.paymentHeading}><Text style={styles.paymentPeriod}>{payment.academicYear} · {payment.semester}</Text><Text style={styles.paymentCode}>{payment.paymentCode}</Text></View>
      <View style={[styles.statusBadge, paid ? styles.statusPaid : styles.statusPending]}><Text style={[styles.statusText, { color: statusColor }]}>{payment.status}</Text></View>
    </View>
    <Text style={styles.amount}>{money(payment.amountDue, payment.currency)}</Text>
    <View style={styles.paymentMeta}><Text>Due {formatDate(payment.dueOn)}</Text><Text>Timetable {payment.timetableStatus.toLowerCase()}</Text></View>
    {paid ? <View style={styles.confirmedLine}><Ionicons name="person-circle-outline" size={16} color={palette.green}/><Text>{payment.confirmationMethod || 'Payment completed'}{payment.paidAtUtc ? ` · ${formatDate(payment.paidAtUtc)}` : ''}</Text></View> : cancelled ? <View style={styles.cancelledLine}><Text>This financial account was cancelled. No QR payment is available.</Text></View> : <View style={styles.paymentActions}>
      <Pressable disabled={busy} onPress={onScan} style={({ pressed }) => [styles.primaryButton, pressed && styles.pressed, busy && styles.disabled]}><Ionicons name="qr-code-outline" size={17} color="white"/><Text style={styles.primaryButtonText}>{busy ? 'Confirming…' : 'Scan QR to pay'}</Text></Pressable>
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

const styles = StyleSheet.create({
  error: { padding: 11, borderRadius: radius.small, backgroundColor: palette.redPale, color: palette.red, fontSize: 12, lineHeight: 18, fontWeight: '700' },
  paymentCard: { gap: 14, borderLeftWidth: 4, borderLeftColor: palette.gold },
  paymentCardPaid: { borderLeftColor: palette.green },
  paymentCardCancelled: { borderLeftColor: palette.red },
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
  paymentMeta: { flexDirection: 'row', justifyContent: 'space-between', paddingTop: 12, borderTopWidth: StyleSheet.hairlineWidth, borderTopColor: palette.line },
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
