import { ButtonLink } from '../controls';

/** Windmill 404 page. */
export function NotFoundPage({ home }: { home: string }) {
  return (
    <div className="flex flex-col items-center py-16 text-center">
      <p className="text-6xl font-semibold text-gray-700 dark:text-gray-200">404</p>
      <p className="mt-2 text-gray-700 dark:text-gray-300">Page not found.</p>
      <ButtonLink to={home} variant="link" className="mt-2">
        Go to app home
      </ButtonLink>
    </div>
  );
}
