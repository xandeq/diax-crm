'use client';

import { Logo } from '@/components/Logo';
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { useAuth } from '@/contexts/AuthContext';
import { login } from '@/services/auth';
import { zodResolver } from '@hookform/resolvers/zod';
import { AlertCircle, Loader2, Lock, Mail } from 'lucide-react';
import { useRouter } from 'next/navigation';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import * as z from 'zod';

// Schema de validação
const loginSchema = z.object({
  email: z.string().email('Digite um email válido'),
  password: z.string().min(6, 'A senha deve ter no mínimo 6 caracteres'),
});

type LoginFormValues = z.infer<typeof loginSchema>;

export default function LoginPage() {
  const router = useRouter();
  const { login: authLogin } = useAuth();
  const [serverError, setServerError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      email: '',
      password: '',
    },
  });

  async function onSubmit(data: LoginFormValues) {
    setIsLoading(true);
    setServerError(null);

    try {
      const response = await login(data.email, data.password);
      if (!response?.accessToken) {
        throw new Error('Token não retornado pelo login.');
      }
      authLogin(response.accessToken);
      router.push('/dashboard/');
    } catch (err) {
      if (err instanceof Error) {
        setServerError(err.message);
      } else {
        setServerError('Ocorreu um erro inesperado. Tente novamente.');
      }
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <div className="flex min-h-[80vh] items-center justify-center py-12 px-4 sm:px-6 lg:px-8">
      <Card className="w-full max-w-md">
        <CardHeader className="space-y-1 items-center text-center">
          <Logo variant="icon" width={60} height={60} className="mb-4" />
          <CardTitle className="text-2xl font-bold">Bem-vindo de volta</CardTitle>
          <CardDescription>
            Entre com suas credenciais para acessar o painel
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <div className="space-y-2">
              <div className="relative">
                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-muted-foreground">
                  <Mail className="h-5 w-5" />
                </div>
                <Input
                  id="email"
                  type="email"
                  placeholder="seu@email.com"
                  disabled={isLoading}
                  className={`pl-10 ${errors.email ? 'border-destructive focus-visible:ring-destructive' : ''}`}
                  {...register('email')}
                />
              </div>
              {errors.email && (
                <p className="text-sm text-destructive flex items-center gap-1">
                  <AlertCircle className="h-3 w-3" /> {errors.email.message}
                </p>
              )}
            </div>

            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <label htmlFor="password" className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70 text-foreground">
                  Senha
                </label>
                <a href="#" className="text-sm font-medium text-primary hover:underline">
                  Esqueceu a senha?
                </a>
              </div>
              <div className="relative">
                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-muted-foreground">
                  <Lock className="h-5 w-5" />
                </div>
                <Input
                  id="password"
                  type="password"
                  placeholder="••••••••"
                  disabled={isLoading}
                  className={`pl-10 ${errors.password ? 'border-destructive focus-visible:ring-destructive' : ''}`}
                  {...register('password')}
                />
              </div>
              {errors.password && (
                <p className="text-sm text-destructive flex items-center gap-1">
                  <AlertCircle className="h-3 w-3" /> {errors.password.message}
                </p>
              )}
            </div>

            {serverError && (
              <div className="p-3 rounded-md bg-destructive/10 text-destructive text-sm flex items-center gap-2">
                <AlertCircle className="h-4 w-4" />
                {serverError}
              </div>
            )}

            <Button type="submit" className="w-full" disabled={isLoading}>
              {isLoading ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Entrando...
                </>
              ) : (
                'Entrar'
              )}
            </Button>
          </form>
        </CardContent>
        <CardFooter className="flex justify-center">
          <div className="text-sm text-muted-foreground">
            Não tem uma conta?{' '}
            <a href="#" className="font-medium text-primary hover:underline">
              Criar conta
            </a>
          </div>
        </CardFooter>
      </Card>
    </div>
  );
}
