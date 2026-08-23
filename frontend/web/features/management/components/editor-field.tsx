import Image from "next/image";
import { Icon } from "@/components/icon";
import { SearchableSelect, type SearchableOption } from "@/components/searchable-select";
import type { Field } from "../management-types";
import { cropPhoto4x6 } from "../management-utils";

export function EditorField({ field, value, options, createOption, error, onChange }: { field: Field; value: string; options: SearchableOption[]; createOption?: SearchableOption; error?: string; onChange: (value: string) => void }) {
  const errorMessage = error ? <small className="field-error">{error}</small> : null;
  if (field.readOnly) {
    const displayValue = field.source ? options.find(option => option.id === value)?.label ?? value : value;
    return <label className="editor-field"><span>{field.label}</span><input value={displayValue} readOnly/>{errorMessage}</label>;
  }
  if (field.type === "photo") return <label className={`photo-upload-field ${error ? "invalid" : ""}`}><span>{field.label}</span><div className="photo-upload-preview">{value ? <Image unoptimized width={120} height={180} src={value} alt="4 by 6 preview"/> : <div><Icon name="users" size={24}/><small>4 x 6</small></div>}</div><input aria-invalid={Boolean(error)} type="file" accept="image/jpeg,image/png,image/webp" onChange={async event => { const file = event.target.files?.[0]; if (file) onChange(await cropPhoto4x6(file)); }}/><b>Choose photo</b><small>JPG, PNG or WebP - cropped to 4 x 6</small>{errorMessage}</label>;
  if (field.type === "checkbox") return <label className={`editor-checkbox ${error ? "invalid" : ""}`}><input aria-invalid={Boolean(error)} type="checkbox" checked={value === "true"} onChange={event => onChange(String(event.target.checked))}/><i/><span>{field.label}</span>{errorMessage}</label>;
  if (field.type === "select" && field.source) return <div className={`editor-field relationship-editor-field ${error ? "invalid" : ""}`}><span>{field.label}</span><SearchableSelect ariaInvalid={Boolean(error)} value={value} options={[...(createOption ? [createOption] : []), { id: "", label: `Select ${field.label.toLowerCase()}` }, ...options]} placeholder={`Type to find ${field.label.toLowerCase()}...`} ariaLabel={field.label} onChange={onChange}/>{errorMessage}</div>;
  return <label className={`editor-field ${error ? "invalid" : ""}`}><span>{field.label}</span>{field.type === "select"
    ? <select aria-invalid={Boolean(error)} value={value} onChange={event => onChange(event.target.value)}><option value="">Select {field.label.toLowerCase()}</option>{options.map(option => <option key={option.id} value={option.id}>{option.label}</option>)}</select>
    : <input aria-invalid={Boolean(error)} type={field.type ?? "text"} step={field.key === "score" ? ".1" : undefined} min={field.key === "score" ? "0" : undefined} max={field.key === "score" ? "100" : undefined} value={value} onChange={event => onChange(event.target.value)}/>}
    {errorMessage}</label>;
}
