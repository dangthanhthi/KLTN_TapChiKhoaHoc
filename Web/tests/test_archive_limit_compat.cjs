const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

function createPage(fetch) {
  const context = vm.createContext({
    document: { addEventListener() {} },
    window: { location: { hostname: 'kltn-tap-chi-khoa-hoc.vercel.app', port: '' } },
    localStorage: { getItem: () => null },
    URL,
    URLSearchParams,
    AbortController,
    setTimeout,
    clearTimeout,
    fetch
  });
  const source = fs.readFileSync(path.join(__dirname, '..', 'journal-interactions.js'), 'utf8');
  vm.runInContext(source, context);
  return context;
}

async function main() {
  const requestedUrls = [];
  const articles = [{ maBaiBao: 41, chuyenNganh: 'Cơ khí – Chế tạo máy – Tự động hóa' }];
  const legacyApi = createPage(async url => {
    requestedUrls.push(url);
    const data = url.endsWith('limit=0') ? [] : articles;
    return { ok: true, status: 200, json: async () => data };
  });

  const result = await vm.runInContext('apiGetLatestArticles(0)', legacyApi);
  assert.equal(requestedUrls.length, 2, 'empty limit=0 response should trigger one compatibility request');
  assert.match(requestedUrls[0], /limit=0$/);
  assert.match(requestedUrls[1], /limit=1000$/);
  assert.equal(result.length, 1, 'compatibility request should restore public archive articles');
  assert.equal(result[0].maBaiBao, 41);

  const modernUrls = [];
  const modernApi = createPage(async url => {
    modernUrls.push(url);
    return { ok: true, status: 200, json: async () => articles };
  });
  const modernResult = await vm.runInContext('apiGetLatestArticles(0)', modernApi);
  assert.equal(modernUrls.length, 1, 'non-empty limit=0 response should not issue a redundant request');
  assert.equal(modernResult.length, 1);

  const boundedUrls = [];
  const boundedApi = createPage(async url => {
    boundedUrls.push(url);
    return { ok: true, status: 200, json: async () => articles };
  });
  await vm.runInContext('apiGetLatestArticles(15)', boundedApi);
  assert.equal(boundedUrls.length, 1, 'normal positive limits should remain unchanged');
  assert.match(boundedUrls[0], /limit=15$/);

  console.log('Archive API compatibility checks passed.');
}

main().catch(error => { console.error(error); process.exitCode = 1; });
