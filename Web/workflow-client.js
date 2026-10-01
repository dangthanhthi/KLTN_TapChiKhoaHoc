/* Shared workflow client: private downloads always use the signed-in session. */
window.journalWorkflow = {
  async request(path, options = {}) {
    const token = localStorage.getItem('journal_token');
    const headers = new Headers(options.headers || {});
    if (token) headers.set('Authorization', `Bearer ${token}`);
    if (options.body && !(options.body instanceof FormData)) headers.set('Content-Type', 'application/json');
    const response = await fetchWithTimeout(`${API_BASE}/workflows/${path}`, { ...options, headers, timeout: 120000 });
    const result = await response.json().catch(() => null);
    if (!response.ok) throw new Error(result?.message || `Không thể thực hiện yêu cầu (HTTP ${response.status}).`);
    return result;
  },
  async blob(path) {
    const response = await fetchWithTimeout(`${API_BASE}/workflows/${path}`, {
      headers: { Authorization: `Bearer ${localStorage.getItem('journal_token') || ''}` }, timeout: 120000
    });
    if (!response.ok) throw new Error(`Không tải được tệp (HTTP ${response.status}).`);
    return response.blob();
  },
  async download(path, name) {
    const url = URL.createObjectURL(await this.blob(path));
    const a = document.createElement('a'); a.href = url; a.download = name; a.click();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  }
};
