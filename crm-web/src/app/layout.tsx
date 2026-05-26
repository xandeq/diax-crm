import { AppShell } from '@/components/AppShell';
import { AuthGuard } from '@/components/AuthGuard';
import { QueryProvider } from '@/components/QueryProvider';
import { AuthProvider } from '@/contexts/AuthContext';
import { Calistoga, Inter, JetBrains_Mono, Plus_Jakarta_Sans } from "next/font/google";
import { Toaster } from 'sonner';
import './globals.css';

const inter = Inter({
  subsets: ["latin"],
  variable: "--font-inter",
  display: "swap",
});

const calistoga = Calistoga({
  weight: "400",
  subsets: ["latin"],
  variable: "--font-calistoga",
  display: "swap",
});

const plusJakartaSans = Plus_Jakarta_Sans({
  subsets: ["latin"],
  weight: ["400", "500", "600", "700", "800"],
  variable: "--font-jakarta",
  display: "swap",
});

const jetbrainsMono = JetBrains_Mono({
  subsets: ["latin"],
  variable: "--font-mono",
  display: "swap",
});

export const metadata = {
  title: 'CRM',
  description: 'Painel administrativo DIAX CRM',
  icons: {
    icon: "/images/logo_logo.png",
    shortcut: "/images/logo_logo.png",
    apple: "/images/logo_logo.png",
  },
};

export default function RootLayout({
  children
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="pt-BR" className={`${inter.variable} ${calistoga.variable} ${jetbrainsMono.variable} ${plusJakartaSans.variable}`}>
      <body suppressHydrationWarning className="font-sans antialiased" style={{ background: '#0F1A14', overflow: 'hidden' }}>
        <QueryProvider>
          <AuthProvider>
            <AuthGuard>
              <AppShell>
                {children}
              </AppShell>
              <Toaster theme="dark" />
            </AuthGuard>
          </AuthProvider>
        </QueryProvider>
      </body>
    </html>
  );
}
