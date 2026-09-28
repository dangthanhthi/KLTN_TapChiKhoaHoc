// Read-only smoke tests for the isolated journal Testing API.
// Every request is a GET and the environment identity is checked first.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');

const base = (process.env.JOURNAL_TEST_BASE_URL || 'http://127.0.0.1:5001/api').replace(/\/$/, '');
const url = new URL(base);
assert.ok(['127.0.0.1', 'localhost'].includes(url.hostname), 'Only a local Testing API is permitted');
assert.equal(url.pathname, '/api', 'Base URL must end in /api');

const results = [];
async function request(path, expected = 200) {
  const response = await fetch(`${base}${path}`, { method: 'GET', redirect: 'manual', signal: AbortSignal.timeout(15000) });
  assert.equal(response.status, expected, `GET ${path}: expected ${expected}, got ${response.status}`);
  return response;
}
async function check(name, fn) {
  try {
    const detail = await fn();
    results.push({ name, status: 'PASS', detail });
    console.log(`PASS ${name}${detail ? ` — ${detail}` : ''}`);
  } catch (error) {
    results.push({ name, status: 'FAIL', detail: error.message });
    console.error(`FAIL ${name} — ${error.message}`);
  }
}

async function main() {
  // Fail before any data inspection if the target is not the isolated DB.
  const target = await (await request('/testing/environment')).json();
  assert.deepEqual(
    [target.environment, target.database, target.dataSource],
    ['Testing', 'QL_TapChiKhoaHoc_Test', '.'],
    'Unexpected API/database identity'
  );
  console.log('TARGET Testing / QL_TapChiKhoaHoc_Test / .');

  await check('Public specialties come from API', async () => {
    const rows = await (await request('/chuyennganh')).json();
    assert.ok(Array.isArray(rows));
    assert.ok(rows.every(row => Number.isInteger(row.maChuyenNganh) && row.tenChuyenNganh));
    return `${rows.length} specialties`;
  });

  let issues = [];
  const publicArticleIds = [];
  await check('Only published issues appear in archive', async () => {
    issues = await (await request('/sotapchi')).json();
    assert.ok(Array.isArray(issues));
    assert.ok(issues.every(issue => ['Đã xuất bản', 'Đã phát hành'].includes(issue.trangThai)));
    assert.ok(issues.every(issue => Number.isInteger(issue.tongSoBaiBao) && issue.tongSoBaiBao > 0));
    return `${issues.length} published issues`;
  });

  await check('Archive counts match each issue detail', async () => {
    let articles = 0;
    for (const issue of issues) {
      const detail = await (await request(`/sotapchi/${issue.maSoTapChi}`)).json();
      const list = detail.danhSachBaiBao;
      assert.ok(Array.isArray(list), `Issue ${issue.maSoTapChi} has no article list`);
      assert.equal(list.length, issue.tongSoBaiBao, `Issue ${issue.maSoTapChi} count mismatch`);
      assert.ok(list.every(article => article.filePdfUrl === `/api/baibao/public/${article.maBaiBao}/pdf`));
      articles += list.length;
      publicArticleIds.push(...list.map(article => article.maBaiBao));
    }
    assert.equal(new Set(publicArticleIds).size, publicArticleIds.length, 'An article appears in more than one issue');
    return `${issues.length} issues, ${articles} article references`;
  });

  await check('Every archive PDF link returns a real PDF', async () => {
    let next = 0;
    let checked = 0;
    await Promise.all(Array.from({ length: 4 }, async () => {
      while (next < publicArticleIds.length) {
        const id = publicArticleIds[next++];
        const response = await fetch(`${base}/baibao/public/${id}/pdf`, {
          method: 'GET', redirect: 'manual', signal: AbortSignal.timeout(15000)
        });
        assert.equal(response.status, 200, `Article ${id} PDF returned ${response.status}`);
        assert.match(response.headers.get('content-type') || '', /application\/pdf/i, `Article ${id} content type`);
        const reader = response.body.getReader();
        try {
          const first = await reader.read();
          assert.equal(Buffer.from(first.value || []).subarray(0, 5).toString('ascii'), '%PDF-', `Article ${id} signature`);
        } finally {
          await reader.cancel();
        }
        checked++;
      }
    }));
    return `${checked} PDFs checked`;
  });

  let latest = [];
  await check('Latest articles expose only public article/PDF links', async () => {
    latest = await (await request('/baibao/public/latest?limit=10')).json();
    assert.ok(Array.isArray(latest));
    assert.ok(latest.length <= 10);
    assert.ok(latest.every(article => article.maBaiBao > 0 &&
      article.filePdfUrl === `/api/baibao/public/${article.maBaiBao}/pdf`));
    return `${latest.length} latest articles`;
  });

  await check('Sample public article details and PDFs are accessible', async () => {
    const sample = latest.slice(0, 3);
    for (const article of sample) {
      const detail = await (await request(`/baibao/public/${article.maBaiBao}`)).json();
      assert.equal(detail.maBaiBao, article.maBaiBao);
      const pdf = await request(`/baibao/public/${article.maBaiBao}/pdf`);
      assert.match(pdf.headers.get('content-type') || '', /application\/pdf/i);
      const signature = Buffer.from(await pdf.arrayBuffer()).subarray(0, 5).toString('ascii');
      assert.equal(signature, '%PDF-', `Article ${article.maBaiBao} PDF signature`);
    }
    return `${sample.length} article/PDF pairs`;
  });

  await check('Private endpoints reject anonymous visitors', async () => {
    for (const path of ['/auth/profile', '/baibao/my-submissions', '/phanbien/my-assignments',
      '/desktop-editorial/articles', '/desktop-admin/users']) {
      await request(path, 401);
    }
    return '5 endpoints returned 401';
  });

  await check('An existing private manuscript is not directly served', async () => {
    const contentRoot = path.resolve(__dirname, '..', 'Backend', 'HuitJournal.Api');
    const folder = path.join(contentRoot, 'Uploads', 'Submissions');
    function findExistingFile(dir) {
      if (!fs.existsSync(dir)) return null;
      for (const item of fs.readdirSync(dir, { withFileTypes: true })) {
        const itemPath = path.join(dir, item.name);
        if (item.isFile()) return itemPath;
        if (item.isDirectory()) {
          const nested = findExistingFile(itemPath);
          if (nested) return nested;
        }
      }
      return null;
    }
    const file = findExistingFile(folder);
    assert.ok(file, 'No private manuscript fixture found on disk');
    const publicPath = path.relative(contentRoot, file).split(path.sep).map(encodeURIComponent).join('/');
    const response = await fetch(`${url.origin}/${publicPath}`, { method: 'GET', redirect: 'manual' });
    assert.equal(response.status, 404);
  });

  const failed = results.filter(row => row.status === 'FAIL');
  console.log(JSON.stringify({ passed: results.length - failed.length, failed: failed.length, total: results.length }));
  if (failed.length) process.exitCode = 1;
}

main().catch(error => { console.error('STOP', error.message); process.exitCode = 1; });
