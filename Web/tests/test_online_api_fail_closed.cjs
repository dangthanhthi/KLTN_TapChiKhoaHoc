const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

function storage(initial = {}) {
  const values = new Map(Object.entries(initial));
  return {
    getItem: key => values.has(key) ? values.get(key) : null,
    setItem: (key, value) => values.set(key, String(value)),
    removeItem: key => values.delete(key)
  };
}

function createPage(hostname = 'localhost', port = hostname === 'localhost' ? '5000' : '') {
  const localStorage = storage({ journal_token: 'real-jwt-token' });
  const sessionStorage = storage();
  const context = vm.createContext({
    document: { addEventListener() {} },
    window: {
      location: { hostname, port, search: '', href: `http://${hostname}${port ? ':' + port : ''}/profile.html` },
      addEventListener() {}
    },
    localStorage,
    sessionStorage,
    URL,
    URLSearchParams,
    AbortController,
    setTimeout,
    clearTimeout,
    fetch: async () => ({ ok: false, status: 503, json: async () => ({ message: 'API đang bảo trì' }) })
  });
  const source = fs.readFileSync(path.join(__dirname, '..', 'journal-interactions.js'), 'utf8');
  vm.runInContext(source, context);
  return { context, localStorage };
}

async function main() {
  const { context, localStorage } = createPage();
  for (const call of [
    'apiAssignReviewer({ maBaiBao: 1 })',
    'apiSubmitEvaluation({ maPhanCong: 1 })',
    'apiMakeDecision({ maBaiBao: 1 })',
    'apiUpdateProfile({ hoTen: "Tên mới" })',
    'apiDeleteAvatar()',
    'apiChangePassword("old", "new")'
  ]) {
    const result = await vm.runInContext(call, context);
    assert.equal(result.success, false, `${call} must not report success after HTTP 503`);
  }
  assert.equal(localStorage.getItem('journal_user'), null, 'online failure must not update local profile');
  await assert.rejects(vm.runInContext('apiGetMySubmissions()', context), /API đang bảo trì/);
  await assert.rejects(vm.runInContext('apiGetMyAssignments()', context), /API đang bảo trì/);
  await assert.rejects(vm.runInContext('apiGetSubmissionDetail(101)', context), /API đang bảo trì/);

  context.fetch = async () => ({ ok: true, status: 200, json: async () => [] });
  assert.equal((await vm.runInContext('apiGetMySubmissions()', context)).length, 0);
  assert.equal((await vm.runInContext('apiGetMyAssignments()', context)).length, 0);

  context.fetch = async () => { throw new Error('offline'); };
  const failure = await vm.runInContext('apiSubmitEvaluation({ maPhanCong: 1 })', context);
  assert.equal(failure.success, false, 'network failure must not save a local review for an online token');

  const deployed = createPage('example.vercel.app');
  await assert.rejects(vm.runInContext('apiGetMySubmissions()', deployed.context), /API đang bảo trì/,
    'a deployed host must not replace unavailable API data with sample records');
  deployed.context.fetch = async () => ({ ok: true, status: 200, json: async () => [] });
  assert.equal((await vm.runInContext('apiGetMySubmissions()', deployed.context)).length, 0,
    'an empty API response must remain empty instead of showing sample records');

  assert.equal(vm.runInContext('API_BASE', createPage('localhost', '5001').context), '/api',
    'Web hosted by Testing backend must use its same-origin API');
  assert.equal(vm.runInContext('API_BASE', createPage('localhost', '8088').context), 'http://localhost:5000/api',
    'separate local static server must use the development API');

  console.log('Online API fail-closed checks passed.');
}

main().catch(error => { console.error(error); process.exitCode = 1; });
