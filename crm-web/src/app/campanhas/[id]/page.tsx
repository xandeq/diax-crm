// Server component wrapper — required for static export with dynamic segments.
// The actual UI is in CampaignReportClient (use client).
// generateStaticParams returns a placeholder so static export doesn't fail;
// the real ID is read client-side at runtime.
import CampaignReportClient from './CampaignReportClient';

export function generateStaticParams() {
  // Return a placeholder segment so Next.js static export generates the shell page.
  // The client component reads the real `id` from params at runtime.
  return [{ id: 'placeholder' }];
}

export default function CampaignReportPage({ params }: { params: { id: string } }) {
  return <CampaignReportClient params={params} />;
}
