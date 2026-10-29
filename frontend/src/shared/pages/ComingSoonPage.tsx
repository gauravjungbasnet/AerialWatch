import { Card, PageHeader } from '../controls';

export function ComingSoonPage({ name }: { name: string }) {
  return (
    <>
      <PageHeader title={name} />
      <Card title="Coming soon">
        <p className="text-sm text-gray-600 dark:text-gray-300">
          This app is planned. Survey tools are not available yet.
        </p>
      </Card>
    </>
  );
}
