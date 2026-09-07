import { Icon } from "@/components/icon";

export function CodeRecommendation({ code, onUse }: { code: string; onUse: () => void }) {
  return <section className="code-recommendation" role="alert" aria-live="polite">
    <span className="code-recommendation-icon"><Icon name="check" size={18}/></span>
    <div>
      <small>First available code</small>
      <strong>{code}</strong>
      <p>The code you entered is already assigned. This is the earliest free sequence in the current code format.</p>
    </div>
    <button type="button" onClick={onUse}>Use {code}<Icon name="arrow" size={13}/></button>
  </section>;
}
