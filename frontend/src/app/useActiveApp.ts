import { useLocation } from 'react-router-dom';
import { getAppForPath } from './registry';

export function useActiveApp() {
  const { pathname } = useLocation();
  const app = getAppForPath(pathname);
  if (!app) throw new Error('App shell requires a registered module URL.');
  return app;
}
