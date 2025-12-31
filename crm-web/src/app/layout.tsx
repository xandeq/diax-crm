import { Header } from '@/components/Header';
import { AuthProvider } from '@/contexts/AuthContext';
import './globals.css';

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
