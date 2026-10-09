const fs = require('node:fs'), vm = require('node:vm'), assert = require('node:assert/strict');
const source = fs.readFileSync(require('node:path').join(__dirname, '../journal-interactions.js'), 'utf8');
const fn = source.slice(source.indexOf('function getPublicDoi('), source.indexOf('const citations ='));
const ctx = vm.createContext({}); vm.runInContext(fn, ctx);
for (const doi of ['10.58810/yersin.2026.a0f8bd', '10.1038/example', '10.59876/huit.jsc.1']) {
  assert.equal(ctx.getPublicDoi(doi), '', 'Unverified DOI must never be published');
  assert.equal(ctx.getPublicDoi(doi, false), '');
  assert.equal(ctx.getPublicDoi(doi, true), doi, 'Verified DOI must not be blocked by its prefix');
}
assert.equal(ctx.getPublicDoi('https://doi.org/10.1234/test', true), '10.1234/test');
for (const bad of ['', 'garbage', '10.1234/test with spaces', '10.1234/<script>']) assert.equal(ctx.getPublicDoi(bad, true), '');
for (const name of ['article-detail.html', 'archives.html']) {
  const html = fs.readFileSync(require('node:path').join(__dirname, '..', name), 'utf8');
  assert.match(html, /getPublicDoi\([^)]*doiVerified\)/, `${name} must use server verification`);
}
assert.match(source, /getPublicDoi\(art.maDOI, art.doiVerified\)/, 'Citations must use verified DOI');
console.log('PASS DOI: unverified hidden, verified prefixes allowed, unsafe values rejected, consistent archive/detail/citations');
