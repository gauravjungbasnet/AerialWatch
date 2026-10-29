import type { ReactNode } from 'react';

export interface NavItem {
  to: string;
  label: string;
  icon: ReactNode;
  /** Header items render as icon links beside the theme toggle instead of in the sidebar. */
  placement?: 'sidebar' | 'header';
  isActive?: (pathname: string) => boolean;
}

export interface NavSection {
  title?: string;
  items: NavItem[];
}
