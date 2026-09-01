"use client";

import { useState } from "react";
import { Icon } from "@/components/icon";
import { administrationApi } from "../administration-api";
import type { SettingFieldDefinition } from "../administration-types";
import { formatCsv, parseCsv } from "../settings-codec";

export function SettingField({ definition, value, values, onChange }: { definition: SettingFieldDefinition; value: string; values: Record<string, string>; onChange: (value: string) => void }) {
  const label = <span className="administration-field-copy"><strong>{definition.label}{definition.required && <i aria-hidden>*</i>}</strong><small>{definition.description}</small></span>;

  if (definition.type === "derived") return <div className="administration-field administration-derived-field">{label}<output>{definition.derive?.(values) ?? "–"}</output></div>;
  if (definition.type === "toggle") return <label className="administration-field administration-toggle-field">{label}<span className="administration-toggle-control"><b>{value === "true" ? "Enabled" : "Disabled"}</b><input type="checkbox" checked={value === "true"} onChange={event => onChange(String(event.target.checked))}/><i/></span></label>;
  if (definition.type === "multiselect" || definition.type === "checklist") return <ChoiceList definition={definition} value={value} label={label} onChange={onChange}/>;
  if (definition.type === "asset") return <AssetLocationField definition={definition} value={value} label={label} onChange={onChange}/>;

  const control = definition.type === "textarea"
    ? <textarea rows={4} required={definition.required} readOnly={definition.readOnly} value={value} placeholder={definition.placeholder} onChange={event => onChange(event.target.value)}/>
    : definition.type === "select"
      ? <span className="administration-select-shell"><select required={definition.required} value={value} onChange={event => onChange(event.target.value)}>{!definition.required && <option value="">Not set</option>}{definition.options?.map(option => <option value={option.value} key={option.value}>{option.label}</option>)}</select><i/></span>
      : <span className="administration-input-shell"><input type={definition.type} required={definition.required} readOnly={definition.readOnly} min={definition.min} max={definition.max} step={definition.step} value={value} placeholder={definition.placeholder} onChange={event => onChange(event.target.value)}/>{definition.unit && <b>{definition.unit}</b>}</span>;

  return <label className="administration-field">{label}{control}</label>;
}

function ChoiceList({ definition, value, label, onChange }: { definition: SettingFieldDefinition; value: string; label: React.ReactNode; onChange: (value: string) => void }) {
  const selected = new Set(parseCsv(value));
  function toggle(option: string, checked: boolean) {
    if (checked) selected.add(option); else selected.delete(option);
    const ordered = definition.options?.map(item => item.value).filter(item => selected.has(item)) ?? [...selected];
    onChange(formatCsv(ordered));
  }
  return <fieldset className={`administration-field administration-choice-list ${definition.type === "checklist" ? "is-checklist" : "is-multiselect"}`}>
    <legend>{label}</legend>
    <div>{definition.options?.map(option => <label key={option.value}><input type="checkbox" checked={selected.has(option.value)} onChange={event => toggle(option.value, event.target.checked)}/><i/><span>{option.label}</span></label>)}</div>
  </fieldset>;
}

function AssetLocationField({ definition, value, label, onChange }: { definition: SettingFieldDefinition; value: string; label: React.ReactNode; onChange: (value: string) => void }) {
  const [failedSource, setFailedSource] = useState("");
  const [uploading, setUploading] = useState(false);
  const [uploadError, setUploadError] = useState("");
  const [fileName, setFileName] = useState("");
  const fallback = definition.key === "faviconUrl" ? "/icon.png" : "/branding/ink-logo.png";
  const failed = Boolean(value) && failedSource === value;
  const source = !value || failed ? fallback : value;
  async function chooseFile(event: React.ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    event.target.value = "";
    if (!file) return;
    setUploading(true);
    setUploadError("");
    try {
      const asset = await administrationApi.uploadAsset(definition.key === "faviconUrl" ? "favicon" : "logo", file);
      setFileName(asset.fileName);
      setFailedSource("");
      onChange(asset.url);
    } catch (reason) { setUploadError(reason instanceof Error ? reason.message : "The image could not be uploaded."); }
    finally { setUploading(false); }
  }
  return <div className="administration-field administration-asset-field">
    {label}
    <div className="administration-asset-control">
      <span className="administration-asset-preview">
        {/* Paths may point to the API or a user-managed CDN, so Next Image host allowlists do not apply here. */}
        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img src={source} alt={`${definition.label} preview`} onError={() => { if (source !== fallback) setFailedSource(value); }}/>
      </span>
      <div className="administration-asset-location"><span>Image location</span><div><input type="text" value={value} placeholder={fallback} onChange={event => onChange(event.target.value)}/><label className="button secondary administration-browse-button"><Icon name="folder" size={14}/>{uploading ? "Uploading..." : "Browse"}<input type="file" accept={definition.accept} disabled={uploading} onChange={chooseFile}/></label></div><small className={uploadError ? "administration-asset-error" : ""}>{uploadError || (failed ? "The configured asset could not be loaded. The existing application artwork is shown as a fallback." : fileName ? `${fileName} is ready. Apply settings to use it.` : `Choose a local file or enter ${definition.accept ?? "an image URL"}. Maximum upload size: 5 MB.`)}</small></div>
    </div>
  </div>;
}
