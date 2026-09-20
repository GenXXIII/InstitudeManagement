import Ionicons from '@expo/vector-icons/Ionicons';
import { useCallback, useEffect, useMemo, useState } from 'react';
import { ActivityIndicator, Modal, Pressable, StyleSheet, Text, View } from 'react-native';
import QRCode from 'react-native-qrcode-svg';
import { Card, EmptyBlock, PortalPage, portalStyles, SectionHeading } from '@/components/portal-ui';
import { palette, radius } from '@/constants/theme';
import type { StudentFinanceOptions, StudentPayment } from '../portal-types';
import { usePortal } from '../portal-context';

type FinanceTab = 'pay' | 'history';

export function FinanceScreen() {
  const portal = usePortal();
  const [tab, setTab] = useState<FinanceTab>('pay');
  const [successMessage, setSuccessMessage] = useState('');
  const payments = useMemo(() => [...portal.payments].sort((left, right) =>
    `${right.academicYear}-${right.semester}-${right.declaredAtUtc}`.localeCompare(`${left.academicYear}-${left.semester}-${left.declaredAtUtc}`, undefined, { numeric: true })), [portal.payments]);
  const payable = payments.filter(payment => payment.status !== 'Paid' && payment.status !== 'Cancelled');
  const history = payments.filter(payment => payment.status === 'Paid' || payment.status === 'Cancelled');

  const showPaid = useCallback((payment: StudentPayment) => {
    setSuccessMessage(`${payment.title} was confirmed successfully.`);
  }, []);

  return <PortalPage title="Finance" subtitle="Generate a secure QR for the exact payment amount. Finance marks it Paid only after Bakong confirms the transaction." eyebrow="Student Finance">
    <View style={styles.tabs}>
      <TabButton label="Pay" icon="qr-code-outline" active={tab === 'pay'} onPress={() => setTab('pay')}/>
      <TabButton label="History" icon="time-outline" active={tab === 'history'} onPress={() => setTab('history')}/>
    </View>
    {successMessage ? <View style={styles.successBanner}><Ionicons name="checkmark-circle" size={20} color={palette.green}/><Text>{successMessage}</Text><Pressable accessibilityLabel="Dismiss success message" onPress={() => setSuccessMessage('')}><Ionicons name="close" size={18} color={palette.green}/></Pressable></View> : null}
    {tab === 'pay' ? <>
      <SectionHeading title="Pay" detail={`${payable.length} ${payable.length === 1 ? 'payment' : 'payments'}`}/>
      {payable.length ? <View style={portalStyles.stack}>{payable.map(payment => <PaymentCard payment={payment} options={portal.financeOptions} onPaid={showPaid} key={payment.id}/>)}</View>
        : <EmptyBlock icon="checkmark-circle-outline" title="No payment due" detail="New payment declarations will appear here when Finance announces them."/>}
    </> : <>
      <SectionHeading title="History" detail={`${history.length} ${history.length === 1 ? 'record' : 'records'}`}/>
      {history.length ? <View style={portalStyles.stack}>{history.map(payment => <HistoryCard payment={payment} key={payment.id}/>)}</View>
        : <EmptyBlock icon="time-outline" title="No payment history" detail="Paid and cancelled payment records will appear here."/>}
    </>}
  </PortalPage>;
}

function TabButton({ label, icon, active, onPress }: { label: string; icon: keyof typeof Ionicons.glyphMap; active: boolean; onPress: () => void }) {
  return <Pressable accessibilityRole="tab" accessibilityState={{ selected: active }} onPress={onPress} style={({ pressed }) => [styles.tab, active && styles.tabActive, pressed && styles.pressed]}>
    <Ionicons name={icon} size={17} color={active ? 'white' : palette.muted}/><Text style={[styles.tabText, active && styles.tabTextActive]}>{label}</Text>
  </Pressable>;
}

function PaymentCard({ payment, options, onPaid }: { payment: StudentPayment; options: StudentFinanceOptions; onPaid: (payment: StudentPayment) => void }) {
  const portal = usePortal();
  const [showQr, setShowQr] = useState(false);
  const [generating, setGenerating] = useState(false);
  const [checking, setChecking] = useState(false);
  const [message, setMessage] = useState('');
  const coverage = payment.paymentPlan === 'Year' ? 'Full year · Semester 1 + Semester 2' : `50% installment · ${payment.semester}`;
  const provider = options.paymentProviders.find(item => options.dynamicQrBank.toLowerCase().includes(item.name.toLowerCase())) ?? options.paymentProviders[0];
  const receiverBank = options.dynamicQrBank || provider?.name || 'Bakong KHQR';
  const receiverName = options.dynamicQrAccountName || provider?.accountName || 'Account name not configured';
  const receiverAccount = options.dynamicQrAccountCode || provider?.accountCode || 'Account not configured';
  const canPay = payment.isDeclared && !payment.isExpired && options.bakongEnabled && options.bakongConfigured;

  const checkPayment = useCallback(async (silent = false) => {
    if (!payment.qrPayload || checking) return;
    setChecking(true);
    if (!silent) setMessage('Checking with Bakong…');
    try {
      const updated = await portal.verifyFinancePayment(payment.studentId, payment.id);
      if (updated.status === 'Paid') {
        setShowQr(false);
        onPaid(updated);
      }
    } catch (reason) {
      if (!silent) setMessage(reason instanceof Error ? reason.message : 'The bank has not confirmed this payment yet.');
    } finally { setChecking(false); }
  }, [checking, onPaid, payment.id, payment.qrPayload, payment.studentId, portal]);

  useEffect(() => {
    if (!showQr || !payment.qrPayload || payment.isQrExpired || payment.status === 'Paid') return;
    let active = true;
    let timer: ReturnType<typeof setTimeout>;
    const poll = async () => {
      await checkPayment(true);
      if (active) timer = setTimeout(() => void poll(), 6000);
    };
    timer = setTimeout(() => void poll(), 3500);
    return () => { active = false; clearTimeout(timer); };
  }, [checkPayment, payment.isQrExpired, payment.qrPayload, payment.status, showQr]);

  async function openPaymentQr() {
    setMessage('');
    if (!canPay) {
      setMessage(payment.isExpired ? 'This declaration has expired. Ask Finance for help.' : 'Finance must finish the dynamic KHQR setup before you can pay.');
      return;
    }
    if (payment.qrPayload && !payment.isQrExpired) { setShowQr(true); return; }
    setGenerating(true);
    try {
      await portal.generateFinanceQr(payment.studentId, payment.id);
      setShowQr(true);
    } catch (reason) {
      setMessage(reason instanceof Error ? reason.message : 'Could not generate the payment QR.');
    } finally { setGenerating(false); }
  }

  return <Card style={styles.paymentCard}>
    <View style={styles.declarationTop}><View style={styles.planPill}><Ionicons name={payment.paymentPlan === 'Year' ? 'layers-outline' : 'calendar-outline'} size={13} color={palette.blue}/><Text style={styles.planText}>Pay as {payment.paymentPlan}</Text></View><Text style={styles.createdText}>{formatDate(payment.declaredAtUtc)}</Text></View>
    <Text style={styles.declarationTitle}>{payment.title}</Text>
    <Text style={styles.coverage}>{coverage}</Text>
    <Text style={styles.amount}>{money(payment.balance, payment.currency)}</Text>
    <Text style={styles.amountLabel}>Exact amount encoded in your payment QR</Text>
    {payment.latePenaltyDays > 0 ? <Text style={styles.penalty}>Late punishment: {payment.latePenaltyDays} days · {money(payment.latePenaltyAmount, payment.currency)}</Text> : null}
    <View style={styles.paymentMeta}><View><Text style={styles.metaLabel}>Due</Text><Text style={styles.metaValue}>{formatDate(payment.dueOn)}</Text></View><View><Text style={styles.metaLabel}>Declaration expires</Text><Text style={styles.metaValue}>{formatDateTime(payment.expiresAtUtc)}</Text></View></View>
    <View style={styles.receiverPreview}><View style={styles.bankIcon}><Ionicons name="business-outline" size={20} color={palette.blue}/></View><View style={styles.bankCopy}><Text style={styles.bankName}>{receiverBank}</Text><Text style={styles.bankAccount}>{receiverName}</Text><Text style={styles.bankCode}>{receiverAccount}</Text></View></View>
    <Pressable accessibilityRole="button" disabled={generating} onPress={() => void openPaymentQr()} style={({ pressed }) => [styles.payButton, pressed && styles.pressed, generating && styles.disabled]}>
      {generating ? <ActivityIndicator color="white"/> : <Ionicons name="qr-code-outline" size={20} color="white"/>}<Text style={styles.payButtonText}>{generating ? 'Creating secure QR…' : 'Pay with QR'}</Text>
    </Pressable>
    {message ? <Text style={styles.message}>{message}</Text> : null}
    <PaymentQrModal visible={showQr} payment={payment} receiverBank={receiverBank} receiverName={receiverName} receiverAccount={receiverAccount} checking={checking} message={message} onCheck={() => void checkPayment()} onClose={() => { setShowQr(false); setMessage(''); }}/>
  </Card>;
}

function PaymentQrModal({ visible, payment, receiverBank, receiverName, receiverAccount, checking, message, onCheck, onClose }: { visible: boolean; payment: StudentPayment; receiverBank: string; receiverName: string; receiverAccount: string; checking: boolean; message: string; onCheck: () => void; onClose: () => void }) {
  return <Modal visible={visible} transparent animationType="fade" onRequestClose={onClose}>
    <View style={styles.modalBackdrop}><View style={styles.modalCard}>
      <View style={styles.modalHeader}><View><Text style={styles.modalEyebrow}>Secure dynamic KHQR</Text><Text style={styles.modalTitle}>{payment.title}</Text></View><Pressable accessibilityLabel="Close payment QR" onPress={onClose} style={styles.closeButton}><Ionicons name="close" size={21} color={palette.ink}/></Pressable></View>
      <View style={styles.receiverDetails}><Detail label="Bank" value={receiverBank}/><Detail label="Account name" value={receiverName}/><Detail label="Account" value={receiverAccount}/><Detail label="Price" value={money(payment.balance, payment.currency)}/></View>
      <View style={styles.qrFrame}>{payment.qrPayload ? <QRCode value={payment.qrPayload} size={214} quietZone={8} backgroundColor="white" color="#0B1423"/> : <ActivityIndicator size="large" color={palette.blue}/>}</View>
      <Text style={styles.qrReference}>Reference: {payment.financialAccountCode}</Text>
      <Text style={styles.qrExpiry}>{payment.qrExpiresAtUtc ? `QR expires ${formatDateTime(payment.qrExpiresAtUtc)}` : 'Preparing QR expiry…'}</Text>
      <View style={styles.waiting}><ActivityIndicator size="small" color={palette.blue}/><Text>Waiting for bank confirmation. Payment becomes Paid automatically.</Text></View>
      {message ? <Text style={styles.modalMessage}>{message}</Text> : null}
      <Pressable accessibilityRole="button" disabled={checking || !payment.qrPayload} onPress={onCheck} style={({ pressed }) => [styles.checkButton, pressed && styles.pressed, checking && styles.disabled]}><Ionicons name="refresh" size={18} color={palette.blue}/><Text>{checking ? 'Checking…' : 'Check payment now'}</Text></Pressable>
    </View></View>
  </Modal>;
}

function Detail({ label, value }: { label: string; value: string }) {
  return <View style={styles.detailRow}><Text style={styles.detailLabel}>{label}</Text><Text style={styles.detailValue}>{value}</Text></View>;
}

function HistoryCard({ payment }: { payment: StudentPayment }) {
  const paid = payment.status === 'Paid';
  return <Card style={[styles.historyCard, paid && styles.historyCardPaid]}>
    <View style={styles.historyTop}><View style={[styles.historyIcon, paid ? styles.historyIconPaid : styles.historyIconMuted]}><Ionicons name={paid ? 'checkmark-circle-outline' : 'time-outline'} size={21} color={paid ? palette.green : palette.muted}/></View><View style={styles.historyCopy}><Text style={styles.historyTitle}>{payment.title}</Text><Text style={styles.historyPeriod}>{payment.academicYear} · {payment.paymentPlan === 'Year' ? 'Full year' : payment.semester}</Text></View><Text style={[styles.historyStatus, paid && styles.historyStatusPaid]}>{payment.status}</Text></View>
    <View style={styles.historyAmount}><Text>{paid ? money(payment.totalPaid, payment.currency) : money(payment.totalDue, payment.currency)}</Text><Text>{formatDate(payment.paidAtUtc ?? payment.expiresAtUtc)}</Text></View>
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
  tabs: { flexDirection: 'row', gap: 8, padding: 5, borderRadius: radius.medium, backgroundColor: '#E8ECF3' },
  tab: { minHeight: 43, flex: 1, flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 7, borderRadius: radius.small },
  tabActive: { backgroundColor: palette.blue }, tabText: { color: palette.muted, fontSize: 12, fontWeight: '800' }, tabTextActive: { color: 'white' },
  successBanner: { flexDirection: 'row', alignItems: 'center', gap: 8, padding: 11, borderRadius: radius.small, backgroundColor: palette.greenPale },
  paymentCard: { gap: 13, borderLeftWidth: 4, borderLeftColor: palette.blue },
  declarationTop: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: 9 },
  planPill: { flexDirection: 'row', alignItems: 'center', gap: 5, paddingHorizontal: 9, paddingVertical: 5, borderRadius: radius.pill, backgroundColor: palette.bluePale },
  planText: { color: palette.blue, fontSize: 9, fontWeight: '800' }, createdText: { color: palette.muted, fontSize: 9, fontWeight: '600' },
  declarationTitle: { color: palette.ink, fontSize: 18, lineHeight: 23, fontWeight: '800' }, coverage: { color: palette.blue, fontSize: 11, fontWeight: '700', marginTop: -8 },
  amount: { color: palette.blueDark, fontSize: 29, lineHeight: 34, fontWeight: '800' }, amountLabel: { color: palette.muted, fontSize: 10, fontWeight: '600', marginTop: -9 },
  penalty: { padding: 9, borderRadius: radius.small, backgroundColor: palette.redPale, color: palette.red, fontSize: 10, fontWeight: '800' },
  paymentMeta: { flexDirection: 'row', justifyContent: 'space-between', gap: 12, paddingTop: 12, borderTopWidth: StyleSheet.hairlineWidth, borderTopColor: palette.line },
  metaLabel: { color: palette.muted, fontSize: 8, fontWeight: '700', textTransform: 'uppercase' }, metaValue: { color: palette.ink, fontSize: 10, fontWeight: '700', marginTop: 3 },
  receiverPreview: { minHeight: 68, flexDirection: 'row', alignItems: 'center', gap: 10, padding: 10, borderWidth: 1, borderColor: palette.line, borderRadius: radius.medium, backgroundColor: '#FAFBFD' },
  bankIcon: { width: 42, height: 42, alignItems: 'center', justifyContent: 'center', borderRadius: 11, backgroundColor: palette.bluePale },
  bankCopy: { flex: 1, minWidth: 0 }, bankName: { color: palette.ink, fontSize: 13, fontWeight: '900' }, bankAccount: { color: palette.muted, fontSize: 10, fontWeight: '700', marginTop: 3 }, bankCode: { color: palette.blue, fontSize: 9, fontWeight: '700', marginTop: 2 },
  payButton: { minHeight: 48, flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 9, borderRadius: radius.small, backgroundColor: palette.blue },
  payButtonText: { color: 'white', fontSize: 13, fontWeight: '900' }, message: { color: palette.red, fontSize: 10, fontWeight: '700', textAlign: 'center' }, disabled: { opacity: 0.55 },
  modalBackdrop: { flex: 1, alignItems: 'center', justifyContent: 'center', padding: 18, backgroundColor: 'rgba(9, 17, 31, 0.72)' },
  modalCard: { width: '100%', maxWidth: 380, gap: 13, padding: 18, borderRadius: 22, backgroundColor: 'white' },
  modalHeader: { flexDirection: 'row', alignItems: 'flex-start', justifyContent: 'space-between', gap: 10 }, modalEyebrow: { color: palette.blue, fontSize: 9, fontWeight: '900', textTransform: 'uppercase' }, modalTitle: { maxWidth: 275, color: palette.ink, fontSize: 18, lineHeight: 23, fontWeight: '900', marginTop: 3 },
  closeButton: { width: 35, height: 35, alignItems: 'center', justifyContent: 'center', borderRadius: 18, backgroundColor: '#EFF2F6' },
  receiverDetails: { gap: 7, padding: 11, borderRadius: radius.small, backgroundColor: '#F5F7FA' }, detailRow: { flexDirection: 'row', justifyContent: 'space-between', gap: 12 }, detailLabel: { color: palette.muted, fontSize: 9, fontWeight: '700' }, detailValue: { flex: 1, color: palette.ink, fontSize: 10, fontWeight: '800', textAlign: 'right' },
  qrFrame: { minHeight: 230, alignItems: 'center', justifyContent: 'center', alignSelf: 'center', padding: 8, borderWidth: 1, borderColor: palette.line, borderRadius: 16, backgroundColor: 'white' },
  qrReference: { color: palette.ink, fontSize: 10, fontWeight: '800', textAlign: 'center' }, qrExpiry: { color: palette.muted, fontSize: 9, fontWeight: '700', textAlign: 'center', marginTop: -8 },
  waiting: { flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 8, padding: 10, borderRadius: radius.small, backgroundColor: palette.bluePale },
  modalMessage: { color: palette.muted, fontSize: 10, lineHeight: 15, textAlign: 'center' },
  checkButton: { minHeight: 44, flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 8, borderWidth: 1, borderColor: palette.blue, borderRadius: radius.small },
  historyCard: { gap: 12, borderLeftWidth: 4, borderLeftColor: palette.muted }, historyCardPaid: { borderLeftColor: palette.green }, historyTop: { flexDirection: 'row', alignItems: 'center', gap: 10 },
  historyIcon: { width: 40, height: 40, alignItems: 'center', justifyContent: 'center', borderRadius: 11 }, historyIconPaid: { backgroundColor: palette.greenPale }, historyIconMuted: { backgroundColor: '#EFF1F4' },
  historyCopy: { flex: 1 }, historyTitle: { color: palette.ink, fontSize: 13, fontWeight: '800' }, historyPeriod: { color: palette.muted, fontSize: 10, marginTop: 3 },
  historyStatus: { color: palette.muted, fontSize: 10, fontWeight: '800' }, historyStatusPaid: { color: palette.green }, historyAmount: { flexDirection: 'row', justifyContent: 'space-between', paddingTop: 10, borderTopWidth: StyleSheet.hairlineWidth, borderTopColor: palette.line },
  pressed: { opacity: 0.72 },
});
