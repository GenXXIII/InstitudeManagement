"use client";

import { useEffect, useState } from "react";
import { Icon } from "@/components/icon";

export function AttendanceIdentityQr({ name, role, publicId, payload }: { name: string; role: "Student" | "Teacher"; publicId: string; payload: string }) {
  const [open, setOpen] = useState(false);
  const [imageUrl, setImageUrl] = useState("");

  useEffect(() => {
    if (!open || !payload) return;
    let active = true;
    void import("qrcode")
      .then(({ default: QRCode }) => QRCode.toDataURL(payload, { width: 320, margin: 2, errorCorrectionLevel: "M" }))
      .then(value => { if (active) setImageUrl(value); });
    return () => { active = false; };
  }, [open, payload]);

  return <>
    <button type="button" className="attendance-qr-trigger" disabled={!payload} onClick={() => setOpen(true)}>
      <span>QR</span>
      <small>{publicId || "Not assigned"}</small>
    </button>
    {open && <div className="modal-backdrop attendance-qr-backdrop" onMouseDown={event => { if (event.target === event.currentTarget) setOpen(false); }}>
      <section className="modal attendance-qr-modal" role="dialog" aria-modal="true" aria-label={`${role} attendance QR for ${name}`}>
        <div className="modal-head"><div><span className="eyebrow">{role} attendance</span><h2>{name}</h2><p>Public ID {publicId}</p></div><button type="button" className="icon-button" onClick={() => setOpen(false)} aria-label="Close attendance QR"><Icon name="close"/></button></div>
        <div className="attendance-qr-image">{imageUrl ? <>
          {/* Data URLs are already final QR assets and do not benefit from Next Image processing. */}
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img src={imageUrl} width={280} height={280} alt={`${role} attendance QR for ${name}`}/>
        </> : <span>Preparing attendance QR…</span>}</div>
        <div className="attendance-qr-help"><strong>Scan from the {role} mobile portal</strong><span>{role === "Teacher" ? "Tap Start Class Now, then scan this QR to confirm attendance and start the class." : "After the Teacher starts class, tap Study Now and scan this QR to become Present and begin learning."}</span></div>
        <div className="modal-actions"><button type="button" className="button primary" onClick={() => setOpen(false)}>Done</button></div>
      </section>
    </div>}
  </>;
}
