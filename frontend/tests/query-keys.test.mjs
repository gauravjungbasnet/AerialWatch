import { readFile, readdir } from 'node:fs/promises';
import assert from 'node:assert/strict';
import test from 'node:test';
import ts from 'typescript';

// WFP query keys live in one file (docs/18 step 2). Load it as plain JS (its imports are type-only).
const keysUrl = new URL('../src/modules/watch-feral-pigs/api/keys.ts', import.meta.url);
const js = ts.transpileModule(await readFile(keysUrl, 'utf8'), {
  compilerOptions: { module: ts.ModuleKind.ESNext, target: ts.ScriptTarget.ES2020 },
}).outputText;
const keys = await import(`data:text/javascript;base64,${Buffer.from(js).toString('base64')}`);

/** Every key the module can build, with sample arguments. */
function allKeys() {
  const out = [];
  for (const [group, entries] of Object.entries(keys)) {
    if (Array.isArray(entries)) out.push([group, entries]);
    else
      for (const [name, value] of Object.entries(entries))
        out.push([`${group}.${name}`, typeof value === 'function' ? value('a', 'b', 'c', 1) : value]);
  }
  return out;
}

const startsWith = (key, prefix) => prefix.every((part, i) => key[i] === part);

test('every WFP query key starts with "wfp", so refreshing ["wfp"] covers the whole module', () => {
  for (const [name, key] of allKeys()) assert.equal(key[0], 'wfp', name);
});

test('refresh prefixes cover the queries that depend on them', () => {
  const { reviewKeys, twinKeys, surveyKeys } = keys;
  // Confirming a twin sighting refreshes reviewKeys.all, which must include that survey's twin checks (D-041).
  assert.ok(startsWith(twinKeys.photo('s', 'i', 'c', 1), reviewKeys.all('s')));
  assert.ok(startsWith(reviewKeys.images('s', 'all'), reviewKeys.queues('s')));
  assert.ok(startsWith(reviewKeys.image('s', 'i'), reviewKeys.photos('s')));
  assert.ok(startsWith(surveyKeys.list({}), surveyKeys.lists));
  assert.ok(startsWith(surveyKeys.images('s'), surveyKeys.detail('s')));
});

test('no query key is written by hand outside api/keys.ts', async () => {
  const root = new URL('../src/', import.meta.url);
  const files = (await readdir(root, { recursive: true })).filter((f) => /\.(ts|tsx)$/.test(f));
  const offenders = [];
  for (const file of files) {
    if (file.replaceAll('\\', '/').endsWith('watch-feral-pigs/api/keys.ts')) continue;
    const source = await readFile(new URL(file, root), 'utf8');
    if (/queryKey:\s*\[/.test(source) || /\[\s*'wfp'/.test(source)) offenders.push(file);
  }
  assert.deepEqual(offenders, []);
});
