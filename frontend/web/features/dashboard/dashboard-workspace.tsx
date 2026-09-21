"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import { Icon } from "@/components/icon";
import { ErrorPage, LoadingPage } from "@/components/page-primitives";
import { dashboardApi } from "./dashboard-api";
import type { Dashboard, DashboardRange } from "./dashboard-types";

const reportingRanges: Array<{ value: DashboardRange; label: string }> = [
  { value: "daily", label: "Today" },
  { value: "weekly", label: "This week" },
  { value: "monthly", label: "This month" },
  { value: "yearly", label: "This year" },
  { value: "all", label: "All time" },
];

const metricIcons = ["users", "teacher", "book", "room", "building"] as const;

export default function DashboardPage() {
  const [range, setRange] = useState<DashboardRange>("monthly");
  const [data, setData] = useState<Dashboard>();
  const [error, setError] = useState(false);
  const [isRefreshing, setIsRefreshing] = useState(false);

  const load = useCallback(async (selectedRange: DashboardRange) => {
    setIsRefreshing(true);
    try {
      setData(await dashboardApi.get(selectedRange));
      setError(false);
    } catch {
      setError(true);
    } finally {
      setIsRefreshing(false);
    }
  }, []);

  useEffect(() => {
    const timer = setTimeout(() => void load(range), 0);
    return () => clearTimeout(timer);
  }, [load, range]);

  if (error && !data) return <ErrorPage retry={() => void load(range)}/>;
  if (!data) return <LoadingPage/>;

  const changeTone = data.attendanceChange > 0 ? "positive" : data.attendanceChange < 0 ? "negative" : "neutral";

  return <div className={`dashboard-page${isRefreshing ? " is-refreshing" : ""}`} aria-busy={isRefreshing}>
    <header className="dashboard-hero">
      <div className="dashboard-hero-orb dashboard-hero-orb-one"/>
      <div className="dashboard-hero-orb dashboard-hero-orb-two"/>
      <div className="dashboard-hero-copy">
        <div className="dashboard-hero-eyebrow"><i/>Institute command center</div>
        <h1>Institude Dashboard</h1>
        <p>Institute performance, financial health, and academic outcomes in one clear view.</p>
        <div className="dashboard-hero-meta">
          <span><Icon name="calendar" size={15}/>{formatPeriod(data.periodStart, data.periodEnd)}</span>
          <span><Icon name="pulse" size={15}/>{data.rangeLabel} reporting</span>
        </div>
      </div>
      <div className="dashboard-hero-controls">
        <label className="dashboard-period-control">
          <span>Reporting period</span>
          <select aria-label="Dashboard reporting period" value={range} onChange={event => setRange(event.target.value as DashboardRange)}>
            {reportingRanges.map(option => <option value={option.value} key={option.value}>{option.label}</option>)}
          </select>
        </label>
        <div className="dashboard-updated"><span>Data refreshed</span><strong>{formatGeneratedAt(data.generatedAt)}</strong></div>
      </div>
    </header>

    <section className="dashboard-metric-grid" aria-label="Institute summary">
      {data.metrics.map((metric, index) => <article className={`dashboard-metric tone-${metric.tone || "blue"}`} key={metric.label}>
        <div className="dashboard-metric-top">
          <span className="dashboard-metric-icon"><Icon name={metricIcons[index % metricIcons.length]} size={20}/></span>
          <small>{String(index + 1).padStart(2, "0")}</small>
        </div>
        <strong>{metric.value}</strong>
        <h2>{metric.label}</h2>
        <p>{metric.detail}</p>
      </article>)}
    </section>

    <main className="dashboard-bento">
      <article className="dashboard-card dashboard-finance-card">
        <CardHeading kicker="Financial overview" title="Current semester finance" detail={data.finance.periodLabel} link="/finance" linkLabel="Open finance"/>
        <div className="dashboard-finance-primary">
          <span><Icon name="finance" size={21}/></span>
          <div><small>Total collected</small><strong>{money(data.finance.collected, data.finance.currency)}</strong></div>
        </div>
        <div className="dashboard-finance-progress">
          <div><span>Collection progress</span><strong>{data.finance.collectionRate.toFixed(1)}%</strong></div>
          <i><b style={{ width: `${Math.max(0, Math.min(100, data.finance.collectionRate))}%` }}/></i>
        </div>
        <div className="dashboard-finance-values">
          <div><span>Total due</span><strong>{money(data.finance.totalDue, data.finance.currency)}</strong></div>
          <div><span>Outstanding</span><strong>{money(data.finance.outstanding, data.finance.currency)}</strong></div>
        </div>
        <div className="dashboard-finance-accounts">
          <span><b>{data.finance.paidAccounts}</b> paid accounts</span>
          <span><b>{data.finance.openAccounts}</b> open accounts</span>
        </div>
      </article>

      <article className="dashboard-card dashboard-trend-card">
        <CardHeading kicker="Participation" title="Attendance trend" detail={`${data.rangeLabel} attendance performance`} link="/record/students" linkLabel="View records"/>
        <div className="dashboard-trend-summary">
          <div><strong>{data.attendanceRate.toFixed(1)}%</strong><span>attendance rate</span></div>
          <span className={changeTone}>{signed(data.attendanceChange)} pts <small>vs previous</small></span>
        </div>
        <AttendanceChart points={data.attendanceTrend}/>
        <div className="dashboard-status-list" aria-label="Attendance breakdown">
          {data.liveStatus.map(item => <div key={item.label}>
            <span className={`dashboard-status-dot status-${statusClass(item.status)}`}/>
            <span><strong>{item.label}</strong></span>
            <b>{item.value}</b>
          </div>)}
        </div>
      </article>

      <article className="dashboard-card dashboard-grade-card">
        <CardHeading kicker="Academic results" title="Grade distribution" detail={`Scores recorded for ${data.rangeLabel.toLowerCase()}`}/>
        <div className="dashboard-grade-summary"><strong>{data.averageGrade.toFixed(1)}</strong><span>Average score</span></div>
        <div className="dashboard-grade-bars">
          {data.gradeDistribution.map(item => <div key={item.label}>
            <b>{Number(item.value).toFixed(0)}%</b>
            <span className="dashboard-grade-track"><i style={{ height: `${Math.max(Number(item.value), 2)}%` }}/></span>
            <small>{item.label}</small>
          </div>)}
        </div>
      </article>

      <article className="dashboard-card dashboard-department-card">
        <CardHeading kicker="Institute structure" title="Department coverage" detail="Current student, course, and leadership coverage" link="/management/departments" linkLabel="Manage departments"/>
        <div className="dashboard-department-summary">
          {data.departmentStatus.length ? data.departmentStatus.map(item => <div key={item.label}>
            <span className="dashboard-department-code">{item.label}</span>
            <strong>{item.value}</strong>
            <small>{item.detail}</small>
            <span className="dashboard-department-owner"><Icon name="teacher" size={13}/>{item.status}</span>
          </div>) : <EmptyMessage text="No active departments found."/>}
        </div>
      </article>
    </main>
  </div>;
}

function CardHeading({ kicker, title, detail, link, linkLabel }: { kicker: string; title: string; detail: string; link?: string; linkLabel?: string }) {
  return <header className="dashboard-card-heading">
    <div><span>{kicker}</span><h2>{title}</h2><p>{detail}</p></div>
    {link && <Link href={link}>{linkLabel}<Icon name="arrow" size={13}/></Link>}
  </header>;
}

function AttendanceChart({ points }: { points: Dashboard["attendanceTrend"] }) {
  if (!points.length) return <EmptyMessage text="No attendance trend is available for this period."/>;
  const singlePoint = points.length === 1;
  const path = singlePoint
    ? `M 0 ${chartY(points[0].value)} L 640 ${chartY(points[0].value)}`
    : points.map((point, index) => `${index ? "L" : "M"} ${chartX(index, points.length)} ${chartY(point.value)}`).join(" ");
  const area = `${path} L 640 190 L 0 190 Z`;

  return <div className="dashboard-chart">
    <div className="dashboard-chart-scale"><span>100</span><span>75</span><span>50</span><span>25</span><span>0</span></div>
    <div className="dashboard-chart-grid"><i/><i/><i/><i/><i/></div>
    <svg viewBox="0 0 640 200" preserveAspectRatio="none" aria-label="Attendance percentage trend">
      <defs><linearGradient id="dashboardAttendanceFill" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stopColor="#5eead4" stopOpacity=".28"/><stop offset="1" stopColor="#5eead4" stopOpacity="0"/></linearGradient></defs>
      <path className="dashboard-chart-area" d={area}/>
      <path className="dashboard-chart-line" d={path}/>
      {points.map((point, index) => <circle cx={chartX(index, points.length)} cy={chartY(point.value)} r="4" key={`${point.label}-${index}`}/>)}
    </svg>
    <div className="dashboard-chart-labels" style={{ gridTemplateColumns: `repeat(${points.length}, minmax(0, 1fr))` }}>
      {points.map((point, index) => <span key={`${point.label}-${index}`}><b>{point.label}</b><small>{Number(point.value).toFixed(0)}%</small></span>)}
    </div>
  </div>;
}

function EmptyMessage({ text }: { text: string }) {
  return <div className="dashboard-empty"><Icon name="archive" size={18}/><span>{text}</span></div>;
}

function chartX(index: number, count: number) { return count < 2 ? 320 : index * 640 / (count - 1); }
function chartY(value: number) { return 180 - Math.max(0, Math.min(100, Number(value))) * 1.55; }
function signed(value: number) { return `${value > 0 ? "+" : ""}${Number(value).toFixed(1)}`; }
function statusClass(value: string) { return value.toLowerCase().replace(/[^a-z0-9]+/g, "-"); }
function money(value: number, currency: string) { return new Intl.NumberFormat("en-US", { style: "currency", currency, maximumFractionDigits: currency === "KHR" ? 0 : 2 }).format(value); }

function formatPeriod(start: string, end: string) {
  const startDate = new Date(start);
  const endDate = new Date(end);
  if (Number.isNaN(startDate.valueOf()) || Number.isNaN(endDate.valueOf())) return "Current academic period";
  const formatter = new Intl.DateTimeFormat("en-GB", { day: "2-digit", month: "short" });
  return `${formatter.format(startDate)} - ${formatter.format(endDate)}`;
}

function formatGeneratedAt(value: string) {
  const date = new Date(value);
  return Number.isNaN(date.valueOf()) ? "Just now" : new Intl.DateTimeFormat("en-GB", { day: "2-digit", month: "short", hour: "2-digit", minute: "2-digit" }).format(date);
}
