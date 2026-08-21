export function RecordMetric({ label, value, detail, tone = "blue" }: { label: string; value: number; detail: string; tone?: string }) {
  return <article className={`panel record-metric tone-${tone}`}><span>{label}</span><strong>{value.toLocaleString()}</strong><small>{detail}</small></article>;
}
