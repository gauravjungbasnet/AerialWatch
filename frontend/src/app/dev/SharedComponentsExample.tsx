import { useMemo, useRef, useState } from 'react';
import { Badge, Button, ButtonLink, Card, PageHeader, ToggleButton } from '@shared/controls';
import { ImageComparison, type ComparisonView } from '@shared/imaging/comparison/ImageComparison';
import { ImageViewer, type ImageViewerHandle, type ViewerMode } from '@shared/imaging/viewer/ImageViewer';
import { MapView } from '@shared/maps/MapView';
import { EXAMPLE_POINTS, INITIAL_MARKS, sceneImage, toAnnotations, type ExampleMark } from './exampleData';

const COLOUR = sceneImage('colour');
const HEAT = sceneImage('heat');
type Pane = 'colour' | 'heat';

/**
 * Development-only page (docs/18 step 7, /dev/shared): the shared image viewer, two-pane comparison and map driven by
 * synthetic data and an in-memory adapter. It imports nothing from `src/modules` (enforced by lint), which proves a new
 * app such as Watch Wetlands or Watch Ghost Net can reuse them without touching Watch Feral Pigs.
 */
export function SharedComponentsExample() {
  const [marks, setMarks] = useState<ExampleMark[]>(INITIAL_MARKS);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [mode, setMode] = useState<ViewerMode>('select');
  const [view, setView] = useState<ComparisonView<Pane>>('both');
  const [point, setPoint] = useState<string>();
  const viewer = useRef<ImageViewerHandle>(null);
  const annotations = useMemo(() => toAnnotations(marks), [marks]);
  const selected = marks.find((m) => m.id === selectedId);

  const pane = (src: string, alt: string, editable: boolean) => (
    <ImageViewer
      ref={editable ? viewer : undefined}
      src={src}
      alt={alt}
      annotations={annotations}
      selectedId={selectedId}
      onSelect={setSelectedId}
      mode={editable ? mode : 'select'}
      options={{ zoom: true, pan: true, selection: true, draw: editable, move: editable, resize: editable }}
      onCreate={(rect) => {
        const id = `m${Date.now()}`;
        setMarks((all) => [...all, { ...rect, id, checked: false }]);
        setSelectedId(id);
        setMode('select');
      }}
      onChangeBox={(id, rect) => setMarks((all) => all.map((m) => (m.id === id ? { ...m, ...rect } : m)))}
    />
  );

  return (
    <div className="mx-auto max-w-7xl space-y-6 p-6">
      <PageHeader
        title="Shared components example"
        description="Development only. Synthetic images and points; nothing here is survey data."
        actions={
          <ButtonLink to="/" variant="outline">
            Back to the app
          </ButtonLink>
        }
      />

      <Card bodyClassName="p-0!" title="Image viewer and comparison">
        <div className="flex flex-wrap items-center gap-2 border-b border-gray-200 p-3 dark:border-gray-700">
          <ToggleButton active={mode === 'select'} label="Select" onClick={() => setMode('select')} />
          <ToggleButton active={mode === 'draw'} label="Draw box" onClick={() => setMode('draw')} />
          <Button
            variant="outline"
            disabled={!selected}
            onClick={() =>
              selected && setMarks((all) => all.map((m) => (m.id === selected.id ? { ...m, checked: !m.checked } : m)))
            }
          >
            {selected?.checked ? 'Mark to check' : 'Mark checked'}
          </Button>
          <Button variant="ghost" onClick={() => viewer.current?.fit()}>
            Fit
          </Button>
          <span className="ml-auto text-sm text-gray-600 dark:text-gray-400">
            {marks.length} boxes · selected: {selected ? <Badge>{selected.id}</Badge> : 'none'}
          </span>
        </div>
        <ImageComparison<Pane>
          name="example-view"
          layout="workspace"
          view={view}
          onView={setView}
          panes={[
            { id: 'colour', label: 'Camera A', content: pane(COLOUR, 'Synthetic colour scene', true) },
            { id: 'heat', label: 'Camera B', content: pane(HEAT, 'Synthetic heat scene', false) },
          ]}
        />
      </Card>

      <Card title="Map" description={point ? `Selected ${point}` : 'Select a point.'}>
        <MapView
          points={EXAMPLE_POINTS}
          selectedId={point}
          onSelect={setPoint}
          label="Example map of four synthetic points"
          className="relative"
          mapClassName="z-0 h-80 w-full overflow-hidden rounded-lg border border-gray-200 dark:border-gray-700"
        />
      </Card>
    </div>
  );
}
