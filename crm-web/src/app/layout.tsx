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
        <div className="container">
          <header style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
            <strong>CRM</strong>
            <nav style={{ display: 'flex', gap: 12 }}>
              <a href="/">Início</a>
              <a href="/login/">Login</a>
              <a href="/dashboard/">Dashboard</a>
              <a href="/leads/">Leads</a>
            </nav>
          </header>
          {children}
        </div>
      </body>
    </html>
  );
}
