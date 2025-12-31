import './globals.css';
import { AuthProvider } from '@/contexts/AuthContext';
import { Header } from '@/components/Header';

export const metadata = {
  title: 'CRM',
  description: 'Painel administrativo DIAX CRM'
};

export default function RootLayout({
  children
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="pt-BR">
      <body>
        <AuthProvider>
          <div className="container">
            <Header />
            {children}
          </div>
        </AuthProvider>
      </body>
    </html>
  );
}
