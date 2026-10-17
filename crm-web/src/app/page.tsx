"use client";

import { Loader2 } from "lucide-react";
import { useRouter } from "next/navigation";
import { useEffect } from "react";

// A raiz não tem conteúdo próprio: o AuthGuard já manda quem não está logado pro /login.
export default function HomePage() {
  const router = useRouter();

  useEffect(() => {
    router.replace("/dashboard/");
  }, [router]);

  return (
    <div className="flex min-h-[60vh] items-center justify-center text-muted-foreground">
      <Loader2 className="h-6 w-6 animate-spin" aria-label="Abrindo o dashboard" />
    </div>
  );
}
