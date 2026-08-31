import { workflowCode, workflowStageLabel, workflowStages, type WorkflowCodeResource, type WorkflowCodeStage } from "@/lib/workflow-code";

export function WorkflowCodeFlow({ sourceCode, resource, currentStage = "history" }: { sourceCode: string; resource: WorkflowCodeResource; currentStage?: WorkflowCodeStage }) {
  const currentIndex = workflowStages.indexOf(currentStage);
  return <section className="workflow-code-flow" aria-label="Record relationship flow">
    <header><span>Relationship flow</span><strong>One source, five linked stages</strong></header>
    <div>{workflowStages.map((stage, index) => <div className={`${index <= currentIndex ? "reached" : "future"} ${stage === currentStage ? "current" : ""}`} key={stage}><small>{workflowStageLabel(stage)}</small><strong>{workflowCode(sourceCode, resource, stage)}</strong>{index < workflowStages.length - 1 && <i aria-hidden="true">→</i>}</div>)}</div>
  </section>;
}
