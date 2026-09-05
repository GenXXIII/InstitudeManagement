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
  const [refreshing, setRefreshing] = useState(false);
  const load = useCallback(async (selectedRange: DashboardRange) => {
    setRefreshing(true);
    try { setData(await dashboardApi.get(selectedRange)); setError(false); }
    catch { setError(true); }
    finally { setRefreshing(false); }
  }, []);
  useEffect(() => { void load(range); }, [load, range]);
  if (error && !data) return <ErrorPage retry={() => void load(range)}/>;
  if (!data) return <LoadingPage/>;

  const changeTone = data.attendanceChange > 0 ? "positive" : data.attendanceChange < 0 ? "negative" : "neutral";
  return <div className="dashboard-page dashboard-viewport-page">
    <header className="dashboard-header">
      <div><span>Institute performance center</span><h1>Institude Dashboard</h1><p>A clear view of enrollment, academic delivery, attendance, grades, and institute activity.</p></div>
      <div className="dashboard-header-actions">
        <label className="dashboard-period-control"><span>Reporting period</span><select aria-label="Dashboard reporting period" value={range} onChange={event => setRange(event.target.value as DashboardRange)}>{reportingRanges.map(option => <option value={option.value} key={option.value}>{option.label}</option>)}</select></label>
        <button className="dashboard-refresh-button" type="button" disabled={refreshing} onClick={() => void load(range)}><Icon name="pulse" size={16}/>{refreshing ? "Refreshing…" : "Refresh data"}</button>
      </div>
    </header>

    <section className="dashboard-reporting-band">
      <div><span>{data.rangeLabel} reporting window</span><strong>{periodLabel(data)}</strong><small>Generated {formatGenerated(data.generatedAt)}</small></div>
      <div className="dashboard-attendance-hero"><span>Attendance performance</span><strong>{data.attendanceRate.toFixed(1)}%</strong><small className={changeTone}>{changeLabel(data.attendanceChange, data.range)}</small></div>
      <div className="dashboard-reporting-note"><Icon name="chart" size={22}/><p><strong>One connected workflow</strong><span>Metrics combine current institute resources with activity inside the selected reporting period.</span></p></div>
    </section>

    <section className="dashboard-metric-grid">{data.metrics.map((metric, index) => <article className={`dashboard-metric tone-${metric.tone || "blue"}`} key={metric.label}><span><Icon name={metricIcons[index % metricIcons.length]} size={19}/></span><div><small>{metric.label}</small><strong>{metric.value}</strong><p>{metric.detail}</p></div></article>)}</section>

    <main className="dashboard-layout">
      <article className="dashboard-card dashboard-trend-card">
        <CardHeading kicker="Attendance trend" title={`${data.rangeLabel} participation`} detail="Present and late entries as a percentage of all attendance records." link="/record/students" linkLabel="Open records"/>
        <div className="dashboard-trend-summary"><strong>{data.attendanceRate.toFixed(1)}%</strong><span className={changeTone}>{signed(data.attendanceChange)} points</span></div>
        <AttendanceChart points={data.attendanceTrend}/>
      </article>

      <article className="dashboard-card dashboard-breakdown-card">
        <CardHeading kicker="Attendance detail" title="Recorded outcomes" detail={`All attendance in the ${data.rangeLabel.toLowerCase()} window.`}/>
        <div className="dashboard-status-list">{data.liveStatus.map(item => <div key={item.label}><span className={`dashboard-status-dot status-${item.status.toLowerCase()}`}/><div><strong>{item.label}</strong><small>{item.detail}</small></div><b>{item.value}</b></div>)}</div>
      </article>

      <article className="dashboard-card dashboard-schedule-card">
        <CardHeading kicker="Academic delivery" title="Today’s schedule" detail="The next six non-cancelled timetable periods." link="/operation/timetable" linkLabel="Full timetable"/>
        <div className="dashboard-schedule-list">{data.todaySchedule.length ? data.todaySchedule.map(item => <div key={`${item.label}-${item.value}`}><time>{item.label}</time><span><strong>{item.value}</strong><small>{item.detail}</small></span><b className={`table-status ${item.status.toLowerCase()}`}>{item.status}</b></div>) : <EmptyMessage text="No timetable periods scheduled today."/>}</div>
      </article>

      <article className="dashboard-card dashboard-department-card">
        <CardHeading kicker="Academic coverage" title="Department overview" detail="Current semester student and course coverage." link="/management/departments" linkLabel="Manage departments"/>
        <div className="dashboard-department-list">{data.departmentStatus.length ? data.departmentStatus.map(item => <div key={item.label}><span>{item.label}</span><div><strong>{item.value}</strong><small>{item.detail}</small></div><b>{item.status}</b></div>) : <EmptyMessage text="No active departments found."/>}</div>
      </article>

      <article className="dashboard-card dashboard-grade-card">
        <CardHeading kicker="Academic results" title="Grade distribution" detail={`Scores recorded in the ${data.rangeLabel.toLowerCase()} window.`}/>
        <div className="dashboard-grade-average"><strong>{data.averageGrade.toFixed(1)}</strong><span>Average score</span></div>
        <div className="dashboard-grade-bars">{data.gradeDistribution.map(item => <div key={item.label}><span className="dashboard-grade-track"><i style={{ height: `${Math.max(Number(item.value), 3)}%` }}/></span><b>{Number(item.value).toFixed(0)}%</b><small>{item.label}</small></div>)}</div>
      </article>

      <article className="dashboard-card dashboard-activity-card">
        <CardHeading kicker="Workflow evidence" title="Recent activity" detail={`Latest changes inside the ${data.rangeLabel.toLowerCase()} window.`} link="/records" linkLabel="View history"/>
        <ActivityList items={data.activity} empty="No activity was recorded in this period."/>
      </article>

      <article className="dashboard-card dashboard-attention-card">
        <CardHeading kicker="Needs review" title="Open attention" detail="Unread institute notices in this reporting period." link="/announce" linkLabel="Open notices"/>
        <ActivityList items={data.attention} empty="Nothing needs attention in this period."/>
      </article>
    </main>
  </div>;
}

function CardHeading({ kicker, title, detail, link, linkLabel }: { kicker: string; title: string; detail: string; link?: string; linkLabel?: string }) {
  return <header className="dashboard-card-heading"><div><span>{kicker}</span><h2>{title}</h2><p>{detail}</p></div>{link && <Link href={link}>{linkLabel}<Icon name="arrow" size={13}/></Link>}</header>;
}

function AttendanceChart({ points }: { points: Dashboard["attendanceTrend"] }) {
  if (!points.length) return <EmptyMessage text="No attendance trend is available for this period."/>;
  const path = points.map((point, index) => `${index ? "L" : "M"} ${chartX(index, points.length)} ${chartY(point.value)}`).join(" ");
  const area = `${path} L 640 190 L 0 190 Z`;
  return <div className="dashboard-chart"><div className="dashboard-chart-grid"><i/><i/><i/><i/></div><svg viewBox="0 0 640 200" preserveAspectRatio="none" aria-label="Attendance percentage trend"><defs><linearGradient id="dashboardAttendanceFill" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stopColor="#3679ef" stopOpacity=".24"/><stop offset="1" stopColor="#3679ef" stopOpacity="0"/></linearGradient></defs><path className="dashboard-chart-area" d={area}/><path className="dashboard-chart-line" d={path}/>{points.map((point, index) => <circle cx={chartX(index, points.length)} cy={chartY(point.value)} r="4" key={`${point.label}-${index}`}/>)}</svg><div className="dashboard-chart-labels">{points.map((point, index) => <span key={`${point.label}-${index}`}><b>{point.label}</b><small>{Number(point.value).toFixed(0)}%</small></span>)}</div></div>;
}

function EmptyMessage({ text }: { text: string }) { return <div className="dashboard-empty"><Icon name="archive" size={18}/><span>{text}</span></div>; }
function chartX(index: number, count: number) { return count < 2 ? 320 : index * 640 / (count - 1); }
function chartY(value: number) { return 180 - Math.max(0, Math.min(100, Number(value))) * 1.55; }
function signed(value: number) { return `${value > 0 ? "+" : ""}${Number(value).toFixed(1)}`; }
function changeLabel(value: number, range: DashboardRange) { return range === "all" ? "Complete institute history" : `${signed(value)} points versus the previous period`; }
function periodLabel(data: Dashboard) { return data.periodStart === "Beginning" ? `Beginning – ${displayDate(data.periodEnd)}` : `${displayDate(data.periodStart)} – ${displayDate(data.periodEnd)}`; }
function displayDate(value: string) { const date = new Date(`${value}T00:00:00`); return Number.isNaN(date.valueOf()) ? value : date.toLocaleDateString("en-US", { day: "2-digit", month: "short", year: "numeric" }); }
function formatGenerated(value: string) { const date = new Date(value); return Number.isNaN(date.valueOf()) ? "just now" : date.toLocaleString("en-US", { day: "2-digit", month: "short", hour: "2-digit", minute: "2-digit" }); }
