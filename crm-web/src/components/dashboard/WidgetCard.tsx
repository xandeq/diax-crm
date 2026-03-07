import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { HelpCircle, Loader2 } from "lucide-react";
import { ReactNode } from "react";

interface WidgetCardProps {
  title: string;
  icon?: ReactNode;
  children: ReactNode;
  isLoading?: boolean;
  error?: string | null;
  className?: string;
  action?: ReactNode;
  infoTooltip?: string;
}

export function WidgetCard({
  title,
  icon,
  children,
  isLoading = false,
  error = null,
  className = "",
  action,
  infoTooltip
}: WidgetCardProps) {
  return (
    <Card className={`widget-enter ${className}`.trim()}>
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
        <div className="flex items-center gap-2">
            {icon && <div className="text-muted-foreground">{icon}</div>}
            <CardTitle className="text-sm font-medium">{title}</CardTitle>
            {infoTooltip && (
              <div className="group relative flex items-center">
                <HelpCircle className="h-4 w-4 text-muted-foreground hover:text-foreground cursor-help transition-colors" />
                <div className="pointer-events-none absolute left-1/2 -top-2 z-50 flex w-max max-w-[250px] -translate-x-1/2 -translate-y-full flex-col items-center opacity-0 transition-opacity group-hover:opacity-100">
                  <div className="rounded bg-popover text-popover-foreground shadow-md px-3 py-2 text-xs border">
                    {infoTooltip}
                  </div>
                  <div className="-mt-px h-2 w-2 rotate-45 border-r border-b bg-popover" />
                </div>
              </div>
            )}
        </div>
      </CardHeader>
      <CardContent>
        {isLoading ? (
          <div className="flex h-20 items-center justify-center">
            <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
          </div>
        ) : error ? (
          <div className="flex h-20 items-center justify-center text-sm text-destructive">
            {error}
          </div>
        ) : (
          <div className="space-y-4">
            {children}
            {action && (
              <div className="flex justify-center">
                {action}
              </div>
            )}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
