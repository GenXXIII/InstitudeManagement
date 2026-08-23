"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { dashboardApi } from "@/features/dashboard/dashboard-api";
import type { Dashboard } from "./dashboard-types";
import { ActivityList, ErrorPage, LoadingPage, MetricCards, PageHeading } from "@/components/page-primitives";

export default function DashboardPage() {
  const [data, setData] = useState<Dashboard>(); const [error, setError] = useState(false);
  const load = useCallback(() => { dashboardApi.get().then(setData).catch(() => setError(true)); }, []);
  useEffect(load, [load]);
  if (error) return <ErrorPage retry={load}/>; if (!data) return <LoadingPage/>;
  const now = new Date();
  return <div className="viewport-data-page dashboard-viewport-page">
    <PageHeading eyebrow="Institute overview" title="Institude of New Khmer" description={now.toLocaleDateString("en-US", { weekday: "long", day: "numeric", month: "long", year: "numeric" })} actions={<><span className="live-pill"><i/> System online</span><button className="button secondary" onClick={load}>Refresh</button></>}/>
    <MetricCards metrics={data.metrics}/>
    <section className="dashboard-grid">
      <article className="panel attendance-panel">
        <div className="panel-head"><div><span className="panel-kicker">Today’s attendance</span><h2>{data.attendanceRate}%</h2></div><span className="positive">↗ {data.attendanceChange}%</span></div>
        <div className="chart-area"><div className="chart-lines"><i/><i/><i/><i/></div><svg viewBox="0 0 600 170" preserveAspectRatio="none" aria-label="Attendance trend"><defs><linearGradient id="fill" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stopColor="#2869f7" stopOpacity=".2"/><stop offset="1" stopColor="#2869f7" stopOpacity="0"/></linearGradient></defs><path className="area" d={areaPath(data.attendanceTrend)}/><path className="line" d={linePath(data.attendanceTrend)}/></svg><div className="chart-labels">{data.attendanceTrend.map(point => <span key={point.label}>{point.label}:00</span>)}</div></div>
      </article>
      <article className="panel schedule-panel"><div className="panel-title"><div><span className="panel-kicker">Live institute</span><h3>Today’s schedule</h3></div><Link href="/operation/timetable">View all</Link></div><div className="schedule-list">{data.todaySchedule.map((item, index) => <div key={item.label}><span className={`schedule-icon s${index}`}>{index === 0 ? "▶" : index === 1 ? "↗" : index === 2 ? "✓" : "◷"}</span><div><strong>{item.label}</strong><small>{item.detail}</small></div><b>{item.value}</b></div>)}</div></article>
      <article className="panel live-panel"><div className="panel-title"><div><span className="panel-kicker">Current state</span><h3>Live campus status</h3></div><span className="mini-live"><i/> Live</span></div><div className="status-grid">{data.liveStatus.map(item => <div key={item.label}><span><i/>{item.label}</span><strong>{item.value}</strong><small>{item.detail}</small></div>)}</div></article>
      <article className="panel attention-panel"><div className="panel-title"><div><span className="panel-kicker">Requires action</span><h3>Attention</h3></div><span className="count-badge">{data.attention.length}</span></div><ActivityList items={data.attention}/></article>
      <article className="panel activity-panel"><div className="panel-title"><div><span className="panel-kicker">Just now</span><h3>Live activity</h3></div><Link href="/records/students">History</Link></div><ActivityList items={data.activity}/></article>
      <article className="panel department-panel"><div className="panel-title"><div><span className="panel-kicker">Across the institute</span><h3>Department status</h3></div><Link href="/management/departments">Analyze</Link></div>{data.departmentStatus.map((item, index) => <div className="progress-row" key={item.label}><div><strong>{item.label}</strong><span>{item.detail}</span></div><div className="progress-track"><i style={{width: `${95 - index * 2}%`}}/></div><b>{item.value}</b></div>)}</article>
      <article className="panel grade-panel"><div className="panel-title"><div><span className="panel-kicker">Academic status</span><h3>Grade distribution</h3></div><strong className="average">{data.averageGrade} <small>avg.</small></strong></div><div className="bars">{data.gradeDistribution.map(item => <div key={item.label}><span>{item.label}</span><i style={{height: `${Math.max(item.value * 2, 24)}px`}}/><b>{item.value}%</b></div>)}</div></article>
    </section>
  </div>;
}

function linePath(points: { value: number }[]) { return points.map((point, index) => `${index ? "L" : "M"} ${chartX(index, points.length)} ${chartY(point.value)}`).join(" "); }
function areaPath(points: { value: number }[]) { return points.length ? `${linePath(points)} L 600 170 L 0 170 Z` : ""; }
function chartX(index: number, count: number) { return count < 2 ? 0 : index * 600 / (count - 1); }
function chartY(value: number) { return 160 - Math.max(0, Math.min(100, value)) * 1.35; }
