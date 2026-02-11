'use client';

import { Suspense } from 'react';
import { CreditCardDetailsClient } from './client';

export default function CreditCardDetailsPage() {
    return (
        <Suspense fallback={<div className="p-8">Carregando detalhes...</div>}>
            <CreditCardDetailsClient />
        </Suspense>
    );
}
