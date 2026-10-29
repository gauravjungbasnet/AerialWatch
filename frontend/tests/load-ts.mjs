import { existsSync, readFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import ts from 'typescript';

/**
 * Loads a TypeScript module from src/ for a node:test, as CommonJS. Resolves relative imports and the @shared/@modules/
 * @app aliases; type-only imports disappear in transpiling. For pure files only (no React or Leaflet at import time).
 */
const src = fileURLToPath(new URL('../src/', import.meta.url));
const aliases = { '@shared/': 'shared', '@modules/': 'modules', '@app/': 'app' };
const cache = new Map();

export function loadTs(relativePath) {
  return load(resolve(src, relativePath));
}

function load(path) {
  if (cache.has(path)) return cache.get(path).exports;
  const module = { exports: {} };
  cache.set(path, module);
  const js = ts.transpileModule(readFileSync(path, 'utf8'), {
    compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2020 },
  }).outputText;
  const localRequire = (name) => {
    const alias = Object.keys(aliases).find((a) => name.startsWith(a));
    const base = alias ? resolve(src, aliases[alias], name.slice(alias.length)) : resolve(dirname(path), name);
    const file = [`${base}.ts`, `${base}.tsx`, resolve(base, 'index.ts')].find(existsSync);
    if (!file) throw new Error(`load-ts: cannot resolve ${name} from ${path}`);
    return load(file);
  };
  new Function('require', 'module', 'exports', js)(localRequire, module, module.exports);
  return module.exports;
}
