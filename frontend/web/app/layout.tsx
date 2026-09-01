import type { Metadata } from "next";
import { AppShell } from "@/components/app-shell";
import { AdministrationModeProvider } from "@/features/administration/administration-mode-context";
import { InstituteSettingsProvider } from "@/features/administration/institute-settings-context";
import "./globals.css";
import "@/features/administration/administration.css";

export const metadata: Metadata = {
  title: "Institude of New Khmer",
  description: "Live operations, academic performance, records, and configuration for Institude of New Khmer.",
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return <html lang="en"><body><InstituteSettingsProvider><AdministrationModeProvider><AppShell>{children}</AppShell></AdministrationModeProvider></InstituteSettingsProvider></body></html>;
}
