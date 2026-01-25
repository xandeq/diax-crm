import { Badge } from "@/components/ui/badge";
import { ImportStatus, ImportedTransactionStatus } from "@/services/finance";

interface ImportStatusBadgeProps {
  status: ImportStatus;
}

export function ImportStatusBadge({ status }: ImportStatusBadgeProps) {
  switch (status) {
    case ImportStatus.Pending:
      return <Badge variant="secondary" className="bg-yellow-100 text-yellow-800 border-yellow-200">Pendente</Badge>;
    case ImportStatus.Validating:
      return <Badge variant="secondary" className="bg-sky-100 text-sky-800 border-sky-200">Validando</Badge>;
    case ImportStatus.Processing:
      return <Badge variant="secondary" className="bg-blue-100 text-blue-800 border-blue-200">Processando</Badge>;
    case ImportStatus.Completed:
      return <Badge variant="secondary" className="bg-green-100 text-green-800 border-green-200">Concluído</Badge>;
    case ImportStatus.Failed:
      return <Badge variant="destructive">Erro</Badge>;
    default:
      return <Badge variant="outline">{status}</Badge>;
  }
}

interface TransactionStatusBadgeProps {
  status: ImportedTransactionStatus;
}

export function TransactionStatusBadge({ status }: TransactionStatusBadgeProps) {
  switch (status) {
    case ImportedTransactionStatus.Pending:
      return <Badge variant="secondary" className="bg-gray-100 text-gray-800">Pendente</Badge>;
    case ImportedTransactionStatus.Matched:
      return <Badge variant="secondary" className="bg-blue-100 text-blue-800">Vínculado</Badge>;
    case ImportedTransactionStatus.Created:
      return <Badge variant="secondary" className="bg-green-100 text-green-800">Criado</Badge>;
    case ImportedTransactionStatus.Ignored:
      return <Badge variant="outline">Ignorado</Badge>;
    case ImportedTransactionStatus.Failed:
      return <Badge variant="destructive">Falhou</Badge>;
    default:
      return <Badge variant="outline">{status}</Badge>;
  }
}
