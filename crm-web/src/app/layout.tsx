import { AuthGuard } from '@/components/AuthGuard';
import { Header } from '@/components/Header';
import { AuthProvider } from '@/contexts/AuthContext';
import { Calistoga, Inter, JetBrains_Mono } from "next/font/google";
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
    <html lang="pt-BR" className={`${inter.variable} ${calistoga.variable} ${jetbrainsMono.variable}`}>
      <body className="font-sans antialiased bg-background text-foreground min-h-screen flex flex-col">
        <AuthProvider>
          <AuthGuard>
            <div className="w-full max-w-6xl mx-auto px-6 flex-1 flex flex-col">
              <Header />
              <main className="flex-1">
                {children}
              </main>
              <Toaster />
            </div>
          </AuthGuard>
        </AuthProvider>
      </body>
    </html>
  );
}
