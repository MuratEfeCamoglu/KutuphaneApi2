import { call } from "./api.js";
import {
  closeDialog, esc, formatDate, openDialog, options, readForm,
  showError, showFieldErrors, statusLabels, toast
} from "./ui.js";

// ============================== Kitaplar ==============================

const bookState = {
  search: "", authorId: "", categoryId: "", onlyAvailable: false,
  sortBy: "title", sortDirection: "asc", page: 1, pageSize: 12
};

export async function renderBooks(view) {
  const [authors, categories] = await Promise.all([call("GET", "/api/authors"), call("GET", "/api/categories")]);
  const authorOptions = authors.map(a => ({ value: a.id, label: `${a.firstName} ${a.lastName}` }));
  const categoryOptions = categories.map(c => ({ value: c.id, label: c.name }));

  view.innerHTML = `
    <header class="view-head">
      <div>
        <h1>Kitaplar</h1>
        <p>Katalogda ara ve filtrele. Bir karta tıklayınca ayrıntısı açılır, oradan ödünç verebilirsin.</p>
      </div>
      <button class="button" id="add-book">Kitap ekle</button>
    </header>
    <form class="filters" id="book-filters" novalidate>
      <label class="field">Ara
        <input type="search" name="search" placeholder="Başlık veya ISBN" value="${esc(bookState.search)}">
      </label>
      <label class="field">Yazar
        <select name="authorId">${options(authorOptions, bookState.authorId, "Tüm yazarlar")}</select>
      </label>
      <label class="field">Kategori
        <select name="categoryId">${options(categoryOptions, bookState.categoryId, "Tüm kategoriler")}</select>
      </label>
      <label class="field">Sırala
        <select name="sortBy">${options([{ value: "title", label: "Başlık" }, { value: "year", label: "Yayın yılı" }], bookState.sortBy)}</select>
      </label>
      <label class="field">Yön
        <select name="sortDirection">${options([{ value: "asc", label: "Artan" }, { value: "desc", label: "Azalan" }], bookState.sortDirection)}</select>
      </label>
      <label class="field field--check">
        <input type="checkbox" name="onlyAvailable" ${bookState.onlyAvailable ? "checked" : ""}> Sadece rafta olanlar
      </label>
    </form>
    <div id="book-results"></div>`;

  const results = view.querySelector("#book-results");
  const form = view.querySelector("#book-filters");
  const reload = () => loadBooks(results, reload);

  let searchTimer;
  form.addEventListener("input", event => {
    bookState.search = form.search.value;
    bookState.authorId = form.authorId.value;
    bookState.categoryId = form.categoryId.value;
    bookState.sortBy = form.sortBy.value;
    bookState.sortDirection = form.sortDirection.value;
    bookState.onlyAvailable = form.onlyAvailable.checked;
    bookState.page = 1;
    clearTimeout(searchTimer);
    searchTimer = setTimeout(reload, event.target.name === "search" ? 300 : 0);
  });
  form.addEventListener("submit", event => event.preventDefault());

  view.querySelector("#add-book").addEventListener("click", () => openBookForm(null, reload));
  await reload();
}

async function loadBooks(container, reload) {
  const query = new URLSearchParams();
  for (const [key, value] of Object.entries(bookState)) {
    if (value !== "" && value !== false) query.set(key, value);
  }

  try {
    const page = await call("GET", `/api/books?${query}`);

    if (page.items.length === 0) {
      container.innerHTML = `<p class="empty">Bu filtrelerle kitap bulunamadı. Aramayı veya filtreleri değiştir.</p>`;
      return;
    }

    container.innerHTML = `
      <div class="catalog">
        ${page.items.map(book => `
          <button class="card card--clickable" data-id="${book.id}">
            <div class="card-call"><span>ISBN ${esc(book.isbn)}</span><span>${book.publishedYear}</span></div>
            <h3 class="card-title">${esc(book.title)}</h3>
            <p class="card-line">${esc(book.authorName)}</p>
            <p class="card-line availability ${book.availableCopies > 0 ? "" : "availability--none"}">
              ${book.availableCopies > 0 ? `${book.availableCopies} kopya rafta` : "Rafta kopya yok"}
            </p>
          </button>`).join("")}
      </div>
      <div class="pager">
        <button class="button button--ghost button--small" data-page="${page.page - 1}" ${page.page <= 1 ? "disabled" : ""}>Önceki</button>
        <span>Sayfa ${page.page} / ${Math.max(page.totalPages, 1)} · ${page.totalCount} kitap</span>
        <button class="button button--ghost button--small" data-page="${page.page + 1}" ${page.page >= page.totalPages ? "disabled" : ""}>Sonraki</button>
      </div>`;

    container.querySelectorAll("[data-id]").forEach(card =>
      card.addEventListener("click", () => openBookDetail(Number(card.dataset.id), reload)));
    container.querySelectorAll("[data-page]").forEach(button =>
      button.addEventListener("click", () => { bookState.page = Number(button.dataset.page); reload(); }));
  } catch (error) {
    showError(error);
  }
}

async function openBookDetail(id, onChange) {
  try {
    const [book, members] = await Promise.all([call("GET", `/api/books/${id}`), call("GET", "/api/members")]);
    const memberOptions = members.map(m => ({ value: m.id, label: `${m.firstName} ${m.lastName} (${m.activeLoanCount} ödünçte)` }));

    const dialog = openDialog(`
      <h2>${esc(book.title)}</h2>
      <p class="mono">ISBN ${esc(book.isbn)} · ${book.publishedYear}</p>
      <p>${esc(book.author.fullName)}</p>
      <p class="muted">${book.categories.length ? book.categories.map(c => esc(c.name)).join(", ") : "Kategori yok"}</p>
      <p class="availability ${book.availableCopies > 0 ? "" : "availability--none"}">
        Stok ${book.stockCount} · rafta ${book.availableCopies}
      </p>
      <form class="filters" id="borrow-form" novalidate>
        <label class="field">Üye
          <select name="memberId">${options(memberOptions, "", "Üye seç")}</select>
        </label>
        <button class="button" type="submit">Ödünç ver</button>
      </form>
      <div class="form-actions">
        <button class="button button--danger" id="delete-book">Kitabı sil</button>
        <button class="button button--ghost" id="edit-book">Düzenle</button>
        <button class="button button--ghost" data-close>Kapat</button>
      </div>`);

    dialog.querySelector("#borrow-form").addEventListener("submit", async event => {
      event.preventDefault();
      const memberId = Number(event.target.memberId.value) || 0;
      try {
        const loan = await call("POST", "/api/loans", { bookId: id, memberId });
        toast(`Ödünç verildi. Son iade tarihi: ${formatDate(loan.dueDate)}`);
        await onChange();
        await openBookDetail(id, onChange);
      } catch (error) {
        showError(error);
      }
    });

    dialog.querySelector("#delete-book").addEventListener("click", async () => {
      if (!confirm(`"${book.title}" silinsin mi?`)) return;
      try {
        await call("DELETE", `/api/books/${id}`);
        closeDialog();
        toast("Kitap silindi.");
        await onChange();
      } catch (error) {
        showError(error);
      }
    });

    dialog.querySelector("#edit-book").addEventListener("click", () => openBookForm(book, onChange));
  } catch (error) {
    showError(error);
  }
}

async function openBookForm(book, onSaved) {
  try {
    const [authors, categories] = await Promise.all([call("GET", "/api/authors"), call("GET", "/api/categories")]);
    const authorOptions = authors.map(a => ({ value: a.id, label: `${a.firstName} ${a.lastName}` }));
    const selected = new Set(book?.categories.map(c => c.id) ?? []);

    const dialog = openDialog(`
      <h2>${book ? "Kitabı düzenle" : "Kitap ekle"}</h2>
      <form id="book-form" novalidate>
        <div class="form-grid">
          <label class="field span-2">Başlık<input type="text" name="title" value="${esc(book?.title)}"></label>
          <label class="field">ISBN (13 rakam)<input type="text" name="isbn" value="${esc(book?.isbn)}" inputmode="numeric"></label>
          <label class="field">Yayın yılı<input type="number" name="publishedYear" value="${book?.publishedYear ?? ""}"></label>
          <label class="field">Stok<input type="number" name="stockCount" value="${book?.stockCount ?? 1}"></label>
          <label class="field">Yazar<select name="authorId" data-number>${options(authorOptions, book?.author.id, "Yazar seç")}</select></label>
          <fieldset class="field fieldset-plain span-2">
            <legend>Kategoriler</legend>
            <div class="checks">
              ${categories.map(c => `
                <label class="field field--check">
                  <input type="checkbox" name="categoryIds" value="${c.id}" ${selected.has(c.id) ? "checked" : ""}> ${esc(c.name)}
                </label>`).join("")}
            </div>
          </fieldset>
        </div>
        <div class="form-actions">
          <button class="button button--ghost" type="button" data-close>Vazgeç</button>
          <button class="button" type="submit">${book ? "Değişiklikleri kaydet" : "Kitabı ekle"}</button>
        </div>
      </form>`);

    const form = dialog.querySelector("#book-form");
    form.addEventListener("submit", async event => {
      event.preventDefault();
      const body = readForm(form);
      body.categoryIds = [...form.querySelectorAll('[name="categoryIds"]:checked')].map(input => Number(input.value));
      try {
        if (book) {
          await call("PUT", `/api/books/${book.id}`, body);
          toast("Kitap güncellendi.");
        } else {
          await call("POST", "/api/books", body);
          toast("Kitap eklendi.");
        }
        closeDialog();
        await onSaved();
      } catch (error) {
        showFieldErrors(form, error);
        showError(error);
      }
    });
  } catch (error) {
    showError(error);
  }
}

// ============================== Ödünçler ==============================

const loanState = { status: "", memberId: "", page: 1, pageSize: 10 };

export function slipsHtml(loans) {
  if (loans.length === 0) return `<p class="empty">Gösterilecek ödünç yok.</p>`;
  return `<div class="slips">
    ${loans.map(loan => `
      <article class="slip">
        <div>
          <h3>${esc(loan.bookTitle)}</h3>
          <div class="slip-meta">
            ${esc(loan.memberFullName)} · alındı ${formatDate(loan.loanDate)} · son iade ${formatDate(loan.dueDate)}
            ${loan.returnDate ? ` · iade ${formatDate(loan.returnDate)}` : ""}
          </div>
        </div>
        <span class="stamp stamp--${loan.status}">${statusLabels[loan.status] ?? loan.status}</span>
        <div>${loan.status !== "Returned" ? `<button class="button button--small" data-return="${loan.id}">İade al</button>` : ""}</div>
      </article>`).join("")}
  </div>`;
}

// API sayfa başına en fazla 50 kitap verir; açılır liste için tüm sayfaları sırayla toplar.
async function fetchAllBooks() {
  const books = [];
  let page = 1, totalPages = 1;
  do {
    const result = await call("GET", `/api/books?pageSize=50&sortBy=title&page=${page}`);
    books.push(...result.items);
    totalPages = result.totalPages;
    page++;
  } while (page <= totalPages);
  return books;
}

export async function renderLoans(view) {
  const [books, members] = await Promise.all([
    fetchAllBooks(),
    call("GET", "/api/members")
  ]);
  const bookOptions = books.map(b => ({ value: b.id, label: `${b.title} (${b.availableCopies} rafta)` }));
  const memberOptions = members.map(m => ({ value: m.id, label: `${m.firstName} ${m.lastName}` }));

  view.innerHTML = `
    <header class="view-head">
      <div>
        <h1>Ödünçler</h1>
        <p>Ödünç ver ve iade al. Durum her sorguda son iade tarihine göre yeniden hesaplanır.</p>
      </div>
    </header>
    <form class="filters" id="give-loan" novalidate>
      <label class="field">Kitap<select name="bookId">${options(bookOptions, "", "Kitap seç")}</select></label>
      <label class="field">Üye<select name="memberId">${options(memberOptions, "", "Üye seç")}</select></label>
      <button class="button" type="submit">Ödünç ver</button>
    </form>
    <form class="filters" id="loan-filters" novalidate>
      <label class="field">Durum
        <select name="status">${options([
          { value: "Active", label: "Ödünçte" },
          { value: "Overdue", label: "Gecikti" },
          { value: "Returned", label: "İade edildi" }
        ], loanState.status, "Tümü")}</select>
      </label>
      <label class="field">Üye<select name="memberId">${options(memberOptions, loanState.memberId, "Tüm üyeler")}</select></label>
    </form>
    <div id="loan-results"></div>`;

  const results = view.querySelector("#loan-results");
  const reload = () => loadLoans(results, reload);

  view.querySelector("#give-loan").addEventListener("submit", async event => {
    event.preventDefault();
    const form = event.target;
    try {
      const loan = await call("POST", "/api/loans", {
        bookId: Number(form.bookId.value) || 0,
        memberId: Number(form.memberId.value) || 0
      });
      toast(`Ödünç verildi. Son iade tarihi: ${formatDate(loan.dueDate)}`);
      await renderLoans(view);
    } catch (error) {
      showError(error);
    }
  });

  const filters = view.querySelector("#loan-filters");
  filters.addEventListener("input", () => {
    loanState.status = filters.status.value;
    loanState.memberId = filters.memberId.value;
    loanState.page = 1;
    reload();
  });

  await reload();
}

async function loadLoans(container, reload) {
  const query = new URLSearchParams();
  for (const [key, value] of Object.entries(loanState)) {
    if (value !== "") query.set(key, value);
  }

  try {
    const page = await call("GET", `/api/loans?${query}`);
    container.innerHTML = slipsHtml(page.items) + (page.totalPages > 1 ? `
      <div class="pager">
        <button class="button button--ghost button--small" data-page="${page.page - 1}" ${page.page <= 1 ? "disabled" : ""}>Önceki</button>
        <span>Sayfa ${page.page} / ${page.totalPages}</span>
        <button class="button button--ghost button--small" data-page="${page.page + 1}" ${page.page >= page.totalPages ? "disabled" : ""}>Sonraki</button>
      </div>` : "");

    bindReturnButtons(container, reload);
    container.querySelectorAll("[data-page]").forEach(button =>
      button.addEventListener("click", () => { loanState.page = Number(button.dataset.page); reload(); }));
  } catch (error) {
    showError(error);
  }
}

export function bindReturnButtons(container, afterReturn) {
  container.querySelectorAll("[data-return]").forEach(button =>
    button.addEventListener("click", async () => {
      try {
        await call("POST", `/api/loans/${button.dataset.return}/return`);
        toast("İade alındı.");
        await afterReturn();
      } catch (error) {
        showError(error);
      }
    }));
}

// ============================== Basit CRUD ekranları ==============================

// Yazar, kategori ve üye ekranları aynı kalıpta: tablo + ekle/düzenle formu + sil.
function crudView(config) {
  return async function render(view) {
    view.innerHTML = `
      <header class="view-head">
        <div><h1>${config.title}</h1><p>${config.intro}</p></div>
        <button class="button" id="add-item">${config.addLabel}</button>
      </header>
      <div id="crud-results"></div>`;

    const results = view.querySelector("#crud-results");
    const reload = async () => {
      try {
        const items = await call("GET", config.path);
        results.innerHTML = items.length === 0
          ? `<p class="empty">Henüz kayıt yok. "${config.addLabel}" ile ilkini ekle.</p>`
          : `<table class="table">
              <thead><tr>${config.columns.map(c => `<th>${c.label}</th>`).join("")}<th></th></tr></thead>
              <tbody>
                ${items.map(item => `
                  <tr>
                    ${config.columns.map(c => `<td>${c.cell(item)}</td>`).join("")}
                    <td class="actions">
                      ${(config.extraActions ?? []).map((action, index) =>
                        `<button class="button button--ghost button--small" data-extra="${index}" data-id="${item.id}">${action.label}</button>`).join(" ")}
                      <button class="button button--ghost button--small" data-edit="${item.id}">Düzenle</button>
                      <button class="button button--danger button--small" data-delete="${item.id}">Sil</button>
                    </td>
                  </tr>`).join("")}
              </tbody>
            </table>`;

        const byId = id => items.find(item => item.id === Number(id));
        results.querySelectorAll("[data-edit]").forEach(button =>
          button.addEventListener("click", () => openForm(byId(button.dataset.edit))));
        results.querySelectorAll("[data-delete]").forEach(button =>
          button.addEventListener("click", () => remove(byId(button.dataset.delete))));
        results.querySelectorAll("[data-extra]").forEach(button =>
          button.addEventListener("click", () => config.extraActions[button.dataset.extra].run(byId(button.dataset.id), reload)));
      } catch (error) {
        showError(error);
      }
    };

    const openForm = item => {
      const dialog = openDialog(`
        <h2>${item ? `${config.singular} düzenle` : config.addLabel}</h2>
        <form id="crud-form" novalidate>
          <div class="form-grid">
            ${config.fields.map(field => `
              <label class="field ${field.wide ? "span-2" : ""}">${field.label}
                <input type="${field.type ?? "text"}" name="${field.name}" value="${esc(item?.[field.name])}">
              </label>`).join("")}
          </div>
          <div class="form-actions">
            <button class="button button--ghost" type="button" data-close>Vazgeç</button>
            <button class="button" type="submit">${item ? "Değişiklikleri kaydet" : config.addLabel}</button>
          </div>
        </form>`);

      const form = dialog.querySelector("#crud-form");
      form.addEventListener("submit", async event => {
        event.preventDefault();
        try {
          if (item) {
            await call("PUT", `${config.path}/${item.id}`, readForm(form));
            toast(`${config.singular} güncellendi.`);
          } else {
            await call("POST", config.path, readForm(form));
            toast(`${config.singular} eklendi.`);
          }
          closeDialog();
          await reload();
        } catch (error) {
          showFieldErrors(form, error);
          showError(error);
        }
      });
    };

    const remove = async item => {
      if (!confirm(`${config.singular} silinsin mi?`)) return;
      try {
        await call("DELETE", `${config.path}/${item.id}`);
        toast(`${config.singular} silindi.`);
        await reload();
      } catch (error) {
        showError(error);
      }
    };

    view.querySelector("#add-item").addEventListener("click", () => openForm(null));
    await reload();
  };
}

export const renderAuthors = crudView({
  title: "Yazarlar",
  intro: "Kitabı olan yazar silinemez; denersen 409 döner.",
  path: "/api/authors",
  singular: "Yazar",
  addLabel: "Yazar ekle",
  columns: [
    { label: "Ad soyad", cell: a => `${esc(a.firstName)} ${esc(a.lastName)}` },
    { label: "Doğum yılı", cell: a => `<span class="mono">${a.birthYear ?? "—"}</span>` },
    { label: "Kitap", cell: a => `<span class="mono">${a.bookCount}</span>` }
  ],
  fields: [
    { name: "firstName", label: "Ad" },
    { name: "lastName", label: "Soyad" },
    { name: "birthYear", label: "Doğum yılı", type: "number" }
  ]
});

export const renderCategories = crudView({
  title: "Kategoriler",
  intro: "Kategori adları benzersizdir; aynı adı tekrar eklemek 409 döner.",
  path: "/api/categories",
  singular: "Kategori",
  addLabel: "Kategori ekle",
  columns: [
    { label: "Ad", cell: c => esc(c.name) },
    { label: "Kitap", cell: c => `<span class="mono">${c.bookCount}</span>` }
  ],
  fields: [{ name: "name", label: "Ad", wide: true }]
});

export const renderMembers = crudView({
  title: "Üyeler",
  intro: "Aynı e-postayla ikinci üye eklenemez. İade edilmemiş ödüncü olan üye silinemez.",
  path: "/api/members",
  singular: "Üye",
  addLabel: "Üye ekle",
  columns: [
    { label: "Ad soyad", cell: m => `${esc(m.firstName)} ${esc(m.lastName)}` },
    { label: "E-posta", cell: m => `<span class="mono">${esc(m.email)}</span>` },
    { label: "Telefon", cell: m => `<span class="mono">${esc(m.phoneNumber ?? "—")}</span>` },
    { label: "Kayıt", cell: m => formatDate(m.createdAt) },
    { label: "Ödünçte", cell: m => `<span class="mono">${m.activeLoanCount}</span>` }
  ],
  fields: [
    { name: "firstName", label: "Ad" },
    { name: "lastName", label: "Soyad" },
    { name: "email", label: "E-posta", wide: true },
    { name: "phoneNumber", label: "Telefon (isteğe bağlı)", wide: true }
  ],
  extraActions: [{
    label: "Ödünçleri",
    run: async function showMemberLoans(member, reloadMembers) {
      try {
        const loans = await call("GET", `/api/members/${member.id}/loans`);
        const dialog = openDialog(`
          <h2>${esc(member.firstName)} ${esc(member.lastName)}</h2>
          ${slipsHtml(loans)}
          <div class="form-actions"><button class="button button--ghost" data-close>Kapat</button></div>`);
        bindReturnButtons(dialog, async () => {
          await reloadMembers();
          await showMemberLoans(member, reloadMembers);
        });
      } catch (error) {
        showError(error);
      }
    }
  }]
});
