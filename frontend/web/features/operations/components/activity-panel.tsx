import { ActivityList } from "@/components/page-primitives";
import type { Activity } from "@/lib/types/presentation-types";

export function ActivityPanel({ title, kicker, items }: { title: string; kicker: string; items: Activity[] }) {
  return <article className="panel"><div className="panel-title"><div><span className="panel-kicker">{kicker}</span><h3>{title}</h3></div></div><ActivityList items={items}/></article>;
}
