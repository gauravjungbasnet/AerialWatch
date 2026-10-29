import js from '@eslint/js';
import globals from 'globals';
import reactHooks from 'eslint-plugin-react-hooks';
import reactRefresh from 'eslint-plugin-react-refresh';
import tseslint from 'typescript-eslint';

/** A no-restricted-imports pattern that blocks each path and everything below it. */
const forbid = (message, ...paths) => ({ group: paths.flatMap((p) => [p, `${p}/**`]), message });

const modulePatterns = [
  forbid('Modules must not import from app/ (docs/17 §5).', '@app', '**/app'),
  forbid(
    'Modules must not import other modules; move shared code to shared/ (docs/17 §5).',
    '@modules',
    '**/modules',
    '**/watch-*',
  ),
];

export default tseslint.config(
  { ignores: ['dist'] },
  {
    extends: [js.configs.recommended, ...tseslint.configs.recommended],
    files: ['**/*.{ts,tsx}'],
    languageOptions: {
      ecmaVersion: 2020,
      globals: globals.browser,
    },
    plugins: {
      'react-hooks': reactHooks,
      'react-refresh': reactRefresh,
    },
    rules: {
      ...reactHooks.configs.recommended.rules,
      'react-refresh/only-export-components': ['warn', { allowConstantExport: true }],
    },
  },
  // Architecture boundaries (docs/17 §5 rule 11). Frontend twin of the backend ArchitectureTests.
  // Each rule covers both spellings of an import: a path alias (@app, @modules) and a relative path.
  {
    files: ['src/shared/**/*.{ts,tsx}'],
    rules: {
      'no-restricted-imports': [
        'error',
        {
          patterns: [
            forbid(
              'shared/ must not import from modules/ or app/ (docs/17 §5).',
              '@app',
              '@modules',
              '**/app',
              '**/modules',
            ),
          ],
        },
      ],
    },
  },
  {
    files: ['src/modules/**/*.{ts,tsx}'],
    rules: { 'no-restricted-imports': ['error', { patterns: modulePatterns }] },
  },
  // A feature never imports another feature (docs/18 §6). Code two features need lives at module level:
  // api/queries, api/keys, components/ or domain/. ESLint replaces rule options per block, so repeat the module rules.
  {
    files: ['src/modules/*/features/**/*.{ts,tsx}'],
    rules: {
      'no-restricted-imports': [
        'error',
        {
          patterns: [
            ...modulePatterns,
            {
              regex: '^\\.\\./[^./][^/]*/',
              message:
                'A feature must not import another feature; move the shared part to api/queries, api/keys, components/ or domain/ (docs/18 §6).',
            },
          ],
        },
      ],
    },
  },
);
