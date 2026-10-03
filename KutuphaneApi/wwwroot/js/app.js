import { onRequest } from "./api.js";
import { esc, showError } from "./ui.js";
import { renderAuthors, renderBooks, renderCategories, renderLoans, renderMembers } from "./views.js";
import { renderTests } from "./tests.js";

// Basit yönlendirme: adres çubuğundaki #books, #loans... hangi ekranın çizileceğini belirler.
const routes = {
  books: renderBooks,
  loans: renderLoans,
  members: renderMembers,
  authors: renderAuthors,
  categories: renderCategories,
  tests: renderTests
};

const view = document.getElementById("view");

async function navigate() {
  const name = location.hash.slice(1) || "books";
  const render = routes[name] ?? renderBooks;

  document.querySelectorAll(".drawer-tab").forEach(tab => {
    if (tab.dataset.view === name) tab.setAttribute("aria-current", "page");
    else tab.removeAttribute("aria-current");
  });

  view.innerHTML = `<p class="muted mono">Yükleniyor…</p>`;
  try {
    await render(view);
  } catch (error) {
    showError(error);
    view.innerHTML = `<p class="empty">Bu ekran yüklenemedi. API çalışıyor mu? Terminalde <span class="mono">dotnet run --project KutuphaneApi</span> komutunu kontrol et.</p>`;
  }
  view.focus({ preventScroll: true });
}

document.querySelectorAll(".drawer-tab").forEach(tab =>
  tab.addEventListener("click", () => { location.hash = tab.dataset.view; }));
window.addEventListener("hashchange", navigate);

// ---------- İstek defteri ----------
const ledger = document.getElementById("ledger-list");

onRequest(result => {
  const family = String(result.status).charAt(0);
  const item = document.createElement("li");
  const details = [
    result.body !== undefined ? `İstek gövdesi:\n${JSON.stringify(result.body, null, 2)}` : "",
    result.data !== null ? `Yanıt:\n${typeof result.data === "string" ? result.data : JSON.stringify(result.data, null, 2)}` : "Yanıt gövdesi yok."
  ].filter(Boolean).join("\n\n");

  item.innerHTML = `
    <details>
      <summary>
        <span>${esc(result.method)}</span>
        <span class="code code--${family}">${result.status || "—"}</span>
        <span class="ledger-path" title="${esc(result.path)}">${esc(result.path)}</span>
      </summary>
      <pre>${esc(details)}</pre>
    </details>`;
  ledger.prepend(item);

  while (ledger.children.length > 150) ledger.lastElementChild.remove();
});

document.getElementById("ledger-clear").addEventListener("click", () => { ledger.innerHTML = ""; });

navigate();
