"use client";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { useAuth } from "@/contexts/AuthContext";
import { ArrowRight, LayoutDashboard } from "lucide-react";
import Link from "next/link";

export default function HomePage() {
  const { isAuthenticated, isLoading } = useAuth();

  return (
    <div className="flex flex-col items-center justify-center min-h-[80vh] text-center space-y-8">
      <Badge variant="section">CRM Platform</Badge>

      <h1 className="text-5xl md:text-7xl font-serif tracking-tight">
        Painel <span className="gradient-text">CRM</span>
      </h1>

      <p className="text-xl text-muted-foreground max-w-2xl mx-auto">
        Este é o frontend do CRM (Next.js export estático) consumindo a API em{' '}
        <code className="bg-muted px-2 py-1 rounded text-sm font-mono">
          {process.env.NEXT_PUBLIC_API_BASE_URL ?? 'N/A'}
        </code>.
      </p>

      <div className="flex flex-wrap gap-4 justify-center">
        {!isLoading && !isAuthenticated && (
          <Button asChild size="lg">
            <Link href="/login/">
              Entrar <ArrowRight className="ml-2 h-4 w-4" />
            </Link>
          </Button>
        )}
        <Button asChild variant="outline" size="lg">
          <Link href="/dashboard/">
            <LayoutDashboard className="mr-2 h-4 w-4" />
            Ir ao dashboard
          </Link>
        </Button>
      </div>
    </div>
  );
}
