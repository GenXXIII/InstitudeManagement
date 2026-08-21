import Image from "next/image";
import { Icon } from "@/components/icon";
import type { Field } from "../management-types";
import { cropPhoto4x6 } from "../management-utils";

export function EditorField({ field, value, options, onChange }: { field: Field; value: string; options: { id: string; label: string }[]; onChange: (value: string) => void }) {
  if (field.type === "photo") return <label className="photo-upload-field"><span>{field.label}</span><div className="photo-upload-preview">{value ? <Image unoptimized width={120} height={180} src={value} alt="4 by 6 preview"/> : <div><Icon name="users" size={24}/><small>4 × 6</small></div>}</div><input type="file" accept="image/jpeg,image/png,image/webp" required={field.required && !value} onChange={async event => { const file = event.target.files?.[0]; if (file) onChange(await cropPhoto4x6(file)); }}/><b>Choose photo</b><small>JPG, PNG or WebP · cropped to 4×6</small></label>;
  if (field.type === "checkbox") return <label className="editor-checkbox"><input type="checkbox" checked={value === "true"} onChange={event => onChange(String(event.target.checked))}/><i/><span>{field.label}</span></label>;
  return <label className="editor-field"><span>{field.label}</span>{field.type === "select" ? <select required={field.required} value={value} onChange={event => onChange(event.target.value)}><option value="">Select {field.label.toLowerCase()}</option>{options.map(option => <option key={option.id} value={option.id}>{option.label}</option>)}</select> : <input required={field.required} type={field.type ?? "text"} step={field.key === "score" ? ".1" : undefined} min={field.key === "score" ? "0" : undefined} max={field.key === "score" ? "100" : undefined} value={value} onChange={event => onChange(event.target.value)}/>}</label>;
}
