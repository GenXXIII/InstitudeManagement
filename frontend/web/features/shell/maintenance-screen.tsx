"use client";

import { useRouter } from "next/navigation";

type MaintenanceScreenProps = {
  instituteName: string;
  logoUrl: string;
  message: string;
};

export function MaintenanceScreen({ instituteName, logoUrl, message }: MaintenanceScreenProps) {
  const router = useRouter();

  return <div className="maintenance-page">
    <div className="ambient ambient-one"/><div className="ambient ambient-two"/>
    <section className="maintenance-card">
      {/* Settings may reference the API or a user-managed CDN. */}
      {/* eslint-disable-next-line @next/next/no-img-element */}
      <img src={logoUrl} alt="Institude of New Khmer logo"/>
      <span>System maintenance</span>
      <h1>{instituteName}</h1>
      <p>{message}</p>
      <div><i/><strong>Business services are temporarily unavailable</strong></div>
      <button className="button primary" type="button" onClick={() => router.push("/settings/maintenance")}>Open Maintenance Control</button>
    </section>
  </div>;
}
