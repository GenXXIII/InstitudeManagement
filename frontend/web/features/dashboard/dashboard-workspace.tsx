"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import { Icon } from "@/components/icon";
import { ActivityList, ErrorPage, LoadingPage } from "@/components/page-primitives";
import { dashboardApi } from "./dashboard-api";
import type { Dashboard, DashboardRange } from "./dashboard-types";

const reportingRanges: Array<{ value: DashboardRange; label: string }> = [
  { value: "daily", label: "Daily" },
  { value: "weekly", label: "Weekly" },
  { value: "monthly", label: "Monthly" },
  { value: "yearly", label: "Yearly" },
  { value: "all", label: "All time" },
];
const metricIcons = ["users", "teacher", "book", "calendar", "grade"] as const;

export default function DashboardPage() {
  const [range, setRange] = useState<DashboardRange>("monthly");
  const [data, setData] = useState<Dashboard>();
  const [error, setError] = useState(false);
  const load = useCallback(async (selectedRange: DashboardRange) => {
    try { setData(await dashboardApi.get(selectedRange)); setError(false); }
    catch { setError(true); }
  }, []);
  useEffect(() => {
  const timer = setTimeout(() => {
    void load(range);
  }, 0);
  return () => clearTimeout(timer);
}, [load, range]);
  if (error && !data) return <ErrorPage retry={() => void load(range)}/>;
  if (!data) return <LoadingPage/>;

  const changeTone = data.attendanceChange > 0 ? "positive" : data.attendanceChange < 0 ? "negative" : "neutral";
  return <div className="dashboard-page dashboard-viewport-page">
    <header className="dashboard-header">
      <div><span>Administrator overview</span><h1>Institude Dashboard</h1><p>Important actions first, followed by today’s delivery and summarized institute performance.</p></div>
      <div className="dashboard-header-actions">
        <label className="dashboard-period-control"><span>Reporting period</span><select aria-label="Dashboard reporting period" value={range} onChange={event => setRange(event.target.value as DashboardRange)}>{reportingRanges.map(option => <option value={option.value} key={option.value}>{option.label}</option>)}</select></label>
        <div className="dashboard-updated"><span>Last updated</span><strong>{formatGeneratedAt(data.generatedAt)}</strong></div>
      </div>
    </header>

    <main className="dashboard-sections">
      <DashboardSection eyebrow="Priority" title="What needs attention now" detail="Administrator actions and today’s academic delivery appear first.">
        <div className="dashboard-priority-strip">
          <PrioritySignal icon="bell" label="Needs review" value={data.attention.length ? `${data.attention.length} recent` : "All clear"} detail="Unread institute notices" tone={data.attention.length ? "red" : "green"}/>
          <PrioritySignal icon="calendar" label="Today’s classes" value={data.todaySchedule.length.toString()} detail="Scheduled periods shown" tone="blue"/>
          <PrioritySignal icon="check" label="Attendance" value={`${data.attendanceRate.toFixed(1)}%`} detail={`${signed(data.attendanceChange)} points vs previous`} tone={changeTone}/>
          <PrioritySignal icon="grade" label="Average grade" value={data.averageGrade.toFixed(1)} detail={data.rangeLabel} tone="violet"/>
        </div>
        <div className="dashboard-section-grid dashboard-priority-grid">
          <article className="dashboard-card dashboard-attention-card">
            <CardHeading kicker="Action required" title="Open attention" detail="Unread institute notices in this reporting period." link="/announce" linkLabel="Open notices"/>
            <ActivityList items={data.attention} empty="Nothing needs attention in this period."/>
          </article>
          <article className="dashboard-card dashboard-schedule-card">
            <CardHeading kicker="Today’s delivery" title="Class schedule" detail="The next six non-cancelled timetable periods." link="/operation/timetable" linkLabel="Full timetable"/>
            <div className="dashboard-schedule-list">{data.todaySchedule.length ? data.todaySchedule.map(item => <div key={`${item.label}-${item.value}`}><time>{item.label}</time><span><strong>{item.value}</strong><small>{item.detail}</small></span><b className={`table-status ${item.status.toLowerCase()}`}>{item.status}</b></div>) : <EmptyMessage text="No timetable periods scheduled today."/>}</div>
          </article>
        </div>
      </DashboardSection>

      <DashboardSection eyebrow="Institute summary" title="People and academic capacity" detail="Current institute totals and activity for the selected reporting period.">
        <section className="dashboard-metric-grid">{data.metrics.map((metric, index) => <article className={`dashboard-metric tone-${metric.tone || "blue"}`} key={metric.label}><span><Icon name={metricIcons[index % metricIcons.length]} size={19}/></span><div><small>{metric.label}</small><strong>{metric.value}</strong><p>{metric.detail}</p></div></article>)}</section>
      </DashboardSection>

      <DashboardSection eyebrow="Student participation" title="Attendance performance" detail="Participation trend and recorded outcomes are grouped together.">
        <div className="dashboard-section-grid dashboard-attendance-grid">
          <article className="dashboard-card dashboard-trend-card">
            <CardHeading kicker="Attendance trend" title={`${data.rangeLabel} participation`} detail="Present and late entries as a percentage of all attendance records." link="/record/students" linkLabel="Open records"/>
            <div className="dashboard-trend-summary"><strong>{data.attendanceRate.toFixed(1)}%</strong><span className={changeTone}>{signed(data.attendanceChange)} points</span></div>
            <AttendanceChart points={data.attendanceTrend}/>
          </article>
          <article className="dashboard-card dashboard-breakdown-card">
            <CardHeading kicker="Attendance detail" title="Recorded outcomes" detail={`All attendance in the ${data.rangeLabel.toLowerCase()} window.`}/>
            <div className="dashboard-status-list">{data.liveStatus.map(item => <div key={item.label}><span className={`dashboard-status-dot status-${item.status.toLowerCase()}`}/><div><strong>{item.label}</strong><small>{item.detail}</small></div><b>{item.value}</b></div>)}</div>
          </article>
        </div>
      </DashboardSection>

      <DashboardSection eyebrow="Academic overview" title="Results and department coverage" detail="Academic results stay separate from institute structure.">
        <div className="dashboard-section-grid dashboard-academic-grid">
          <article className="dashboard-card dashboard-grade-card">
            <CardHeading kicker="Academic results" title="Grade distribution" detail={`Scores recorded in the ${data.rangeLabel.toLowerCase()} window.`}/>
            <div className="dashboard-grade-average"><strong>{data.averageGrade.toFixed(1)}</strong><span>Average score</span></div>
            <div className="dashboard-grade-bars">{data.gradeDistribution.map(item => <div key={item.label}><span className="dashboard-grade-track"><i style={{ height: `${Math.max(Number(item.value), 3)}%` }}/></span><b>{Number(item.value).toFixed(0)}%</b><small>{item.label}</small></div>)}</div>
          </article>
          <article className="dashboard-card dashboard-department-card">
            <CardHeading kicker="Institute structure" title="Department overview" detail="Current semester student and course coverage." link="/management/departments" linkLabel="Manage departments"/>
            <div className="dashboard-department-list">{data.departmentStatus.length ? data.departmentStatus.map(item => <div key={item.label}><span>{item.label}</span><div><strong>{item.value}</strong><small>{item.detail}</small></div><b>{item.status}</b></div>) : <EmptyMessage text="No active departments found."/>}</div>
          </article>
        </div>
      </DashboardSection>

      <DashboardSection eyebrow="Audit trail" title="Recent institute activity" detail="The latest recorded changes are kept apart from performance summaries.">
        <article className="dashboard-card dashboard-activity-card">
          <CardHeading kicker="Workflow evidence" title="Recent activity" detail={`Latest changes inside the ${data.rangeLabel.toLowerCase()} window.`} link="/records" linkLabel="View history"/>
          <ActivityList items={data.activity} empty="No activity was recorded in this period."/>
        </article>
      </DashboardSection>
    </main>
  </div>;
}

function DashboardSection({ eyebrow, title, detail, children }: { eyebrow: string; title: string; detail: string; children: React.ReactNode }) {
  return <section className="dashboard-section"><header className="dashboard-section-heading"><div><span>{eyebrow}</span><h2>{title}</h2></div><p>{detail}</p></header>{children}</section>;
}

function PrioritySignal({ icon, label, value, detail, tone }: { icon: "bell" | "calendar" | "check" | "grade"; label: string; value: string; detail: string; tone: string }) {
  return <article className={`dashboard-priority-signal tone-${tone}`}><span><Icon name={icon} size={18}/></span><div><small>{label}</small><strong>{value}</strong><p>{detail}</p></div></article>;
}

function CardHeading({ kicker, title, detail, link, linkLabel }: { kicker: string; title: string; detail: string; link?: string; linkLabel?: string }) {
  return <header className="dashboard-card-heading"><div><span>{kicker}</span><h2>{title}</h2><p>{detail}</p></div>{link && <Link href={link}>{linkLabel}<Icon name="arrow" size={13}/></Link>}</header>;
}

function AttendanceChart({ points }: { points: Dashboard["attendanceTrend"] }) {
  if (!points.length) return <EmptyMessage text="No attendance trend is available for this period."/>;
  const singlePoint = points.length === 1;
  const path = singlePoint
    ? `M 0 ${chartY(points[0].value)} L 640 ${chartY(points[0].value)}`
    : points.map((point, index) => `${index ? "L" : "M"} ${chartX(index, points.length)} ${chartY(point.value)}`).join(" ");
  const area = `${path} L 640 190 L 0 190 Z`;
  return <div className="dashboard-chart"><div className="dashboard-chart-grid"><i/><i/><i/><i/></div><svg viewBox="0 0 640 200" preserveAspectRatio="none" aria-label="Attendance percentage trend"><defs><linearGradient id="dashboardAttendanceFill" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stopColor="#3679ef" stopOpacity=".24"/><stop offset="1" stopColor="#3679ef" stopOpacity="0"/></linearGradient></defs><path className="dashboard-chart-area" d={area}/><path className="dashboard-chart-line" d={path}/>{points.map((point, index) => <circle cx={chartX(index, points.length)} cy={chartY(point.value)} r="4" key={`${point.label}-${index}`}/>)}</svg><div className="dashboard-chart-labels" style={{ gridTemplateColumns: `repeat(${points.length}, minmax(0, 1fr))` }}>{points.map((point, index) => <span key={`${point.label}-${index}`}><b>{point.label}</b><small>{Number(point.value).toFixed(0)}%</small></span>)}</div></div>;
}

function EmptyMessage({ text }: { text: string }) { return <div className="dashboard-empty"><Icon name="archive" size={18}/><span>{text}</span></div>; }
function chartX(index: number, count: number) { return count < 2 ? 320 : index * 640 / (count - 1); }
function chartY(value: number) { return 180 - Math.max(0, Math.min(100, Number(value))) * 1.55; }
function signed(value: number) { return `${value > 0 ? "+" : ""}${Number(value).toFixed(1)}`; }
function formatGeneratedAt(value: string) {
  const date = new Date(value);
  return Number.isNaN(date.valueOf()) ? "Just now" : new Intl.DateTimeFormat("en-GB", { day: "2-digit", month: "short", hour: "2-digit", minute: "2-digit" }).format(date);
}
