import { Badge } from '@/components/ui/badge';

interface StatusBadgeProps {
  status: string;
  statusDescription: string;
}

export function StatusBadge({ status, statusDescription }: StatusBadgeProps) {
  const variant = status === 'Published' ? 'default' :
                  status === 'Draft' ? 'secondary' :
                  'outline';

  return (
    <Badge variant={variant}>
      {statusDescription}
    </Badge>
  );
}
