import { redirect } from 'next/navigation';

export default function NewIncomePage() {
    redirect('/finance/transactions/new');
}
