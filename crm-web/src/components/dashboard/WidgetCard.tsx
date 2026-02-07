import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Loader2 } from "lucide-react";
import { ReactNode } from "react";

interface WidgetCardProps {
  title: string;
  icon?: ReactNode;
  children: ReactNode;
  isLoading?: boolean;
  error?: string | null;
  className?: string;
  action?: ReactNode;
}

export function WidgetCard({
  title,
  icon,
  children,
  isLoading = false,
  error = null,
  className = "",
  action
}: WidgetCardProps) {
  return (
    <Card className={className}>
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
        <div className="flex items-center gap-2">
            {icon && <div className="text-muted-foreground">{icon}</div>}
            <CardTitle className="text-sm font-medium">{title}</CardTitle>
        </div>
        {action}
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
          children
        )}
      </CardContent>
    </Card>
  );
}
