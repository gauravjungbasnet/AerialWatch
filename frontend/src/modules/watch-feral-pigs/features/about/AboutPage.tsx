import { useModelInfo } from './hooks';
import { Card, PageHeader, SkeletonRows } from '@shared/controls';
import { CitationBlock } from '../../components';

export function AboutPage() {
  const info = useModelInfo();

  return (
    <>
      <PageHeader title="About the research" />
      <div className="grid max-w-5xl gap-6">
        <Card title="The study">
          <div className="space-y-4 text-sm text-gray-700 dark:text-gray-300">
            <CitationBlock />
            <p>
              CDU researchers put GPS collars on feral pigs and flew a drone at four times of day in the wet and dry
              seasons, then checked how many collared pigs actually appeared in the images. Pigs were hardest to see
              mid-afternoon and easiest in the early evening. Aerial Watch uses the published model to plan flights and
              to correct counts.
            </p>
          </div>
        </Card>

        <div className="grid gap-6 md:grid-cols-2">
          <Card title="Study conditions">
            {info.isLoading && <SkeletonRows rows={3} />}
            <ul className="list-disc space-y-1 pl-5 text-sm text-gray-600 dark:text-gray-400">
              {info.data?.studyConditions.map((s) => (
                <li key={s}>{s}</li>
              ))}
            </ul>
          </Card>
          <Card title="Our assumptions">
            {info.isLoading && <SkeletonRows rows={3} />}
            <ul className="list-disc space-y-1 pl-5 text-sm text-gray-600 dark:text-gray-400">
              {info.data?.ourAssumptions.map((s) => (
                <li key={s}>{s}</li>
              ))}
            </ul>
            {info.data && <p className="mt-3 text-xs text-gray-500">Model {info.data.modelVersion}</p>}
          </Card>
        </div>

        <Card title="Related work">
          <p className="text-sm text-gray-700 dark:text-gray-300">
            Combining AI detection with detection-probability correction has been done for waterfowl (Viegut et al.
            2024, <i>Drones</i> 8(2):54). Aerial Watch applies the idea to NT feral pigs using CDU's model.
          </p>
        </Card>

        <Card title="Disclaimer">
          <p className="text-sm text-gray-700 dark:text-gray-300">
            Independent student project for CDU IT Code Fair 2026. Not affiliated with or endorsed by the researchers,
            CDU or any ranger organisation. AI models are trained on open datasets and, with permission, NACAS survey
            images.
          </p>
        </Card>
      </div>
    </>
  );
}
