import { request } from "./api.js";
import { esc } from "./ui.js";

// Canlı API'ye karşı çalışan senaryolar. Her senaryo kendi yazar/kitap/üye kayıtlarını oluşturur,
// adımlarda beklenen durum kodunu gerçek yanıtla karşılaştırır ve sonunda oluşturduğu kayıtları temizler.
// (Otomatik testler KutuphaneApi.Tests projesinde; bu sayfa aynı kuralları gözle görebilmen için.)

let sequence = 0;
const unique = () => `${Date.now().toString().slice(-7)}${String(++sequence).padStart(3, "0")}`;

class ScenarioContext {
  constructor(onStep) {
    this.steps = [];
    this.onStep = onStep;
    this.created = { loans: [], books: [], members: [], authors: [] };
  }

  async expect(label, method, path, body, expectedStatus) {
    const result = await request(method, path, body);
    const step = { label, method, path, expected: expectedStatus, actual: result.status, pass: result.status === expectedStatus, data: result.data };
    this.steps.push(step);
    this.onStep();
    return result;
  }

  check(label, condition) {
    this.steps.push({ label, pass: Boolean(condition) });
    this.onStep();
  }

  async author() {
    const result = await this.expect("Test yazarı oluştur", "POST", "/api/authors", { firstName: "Test", lastName: `Yazar ${unique()}`, birthYear: 1950 }, 201);
    this.created.authors.push(result.data?.id);
    return result.data;
  }

  async book(stockCount = 1) {
    this.authorForBooks ??= await this.author();
    const isbn = `979${unique()}`;
    const result = await this.expect(`Kitap oluştur (stok ${stockCount})`, "POST", "/api/books",
      { title: `Test Kitabı ${isbn}`, isbn, publishedYear: 2000, stockCount, authorId: this.authorForBooks?.id ?? 0, categoryIds: [] }, 201);
    this.created.books.push(result.data?.id);
    return result.data;
  }

  async member() {
    const result = await this.expect("Üye oluştur", "POST", "/api/members",
      { firstName: "Test", lastName: "Üye", email: `test${unique()}@ornek.com`, phoneNumber: null }, 201);
    this.created.members.push(result.data?.id);
    return result.data;
  }

  async borrow(book, member, expectedStatus = 201, label = "Ödünç ver") {
    const result = await this.expect(label, "POST", "/api/loans", { bookId: book?.id ?? 0, memberId: member?.id ?? 0 }, expectedStatus);
    if (result.status === 201) this.created.loans.push(result.data.id);
    return result.data;
  }

  // Oluşturulan kayıtları ters sırayla temizler: önce ödünçleri iade, sonra kitap, üye ve yazarları sil.
  // Bu istekler defterde görünür ama senaryonun sonucunu etkilemez.
  async cleanup() {
    for (const id of this.created.loans) await request("POST", `/api/loans/${id}/return`);
    for (const id of this.created.books.filter(Boolean)) await request("DELETE", `/api/books/${id}`);
    for (const id of this.created.members.filter(Boolean)) await request("DELETE", `/api/members/${id}`);
    for (const id of this.created.authors.filter(Boolean)) await request("DELETE", `/api/authors/${id}`);
  }
}

const scenarios = [
  {
    rule: "Kural 1",
    title: "Ödünç süresi 14 gündür",
    async run(ctx) {
      const book = await ctx.book();
      const member = await ctx.member();
      const loan = await ctx.borrow(book, member);
      const days = loan ? (new Date(loan.dueDate) - new Date(loan.loanDate)) / 86_400_000 : NaN;
      ctx.check(`Son iade tarihi − ödünç tarihi = ${days} gün (beklenen 14)`, days === 14);
      ctx.check(`Durum "${loan?.status}" (beklenen Active)`, loan?.status === "Active");
    }
  },
  {
    rule: "Kural 2",
    title: "Bir üye aynı anda en fazla 3 kitap alabilir",
    async run(ctx) {
      const member = await ctx.member();
      for (let i = 1; i <= 3; i++) await ctx.borrow(await ctx.book(), member, 201, `${i}. kitabı ödünç ver`);
      await ctx.borrow(await ctx.book(), member, 409, "4. kitabı ödünç vermeyi dene");
    }
  },
  {
    rule: "Kural 3",
    title: "Müsait kopyası olmayan kitap ödünç verilemez",
    async run(ctx) {
      const book = await ctx.book(1);
      await ctx.borrow(book, await ctx.member(), 201, "Tek kopyayı birinci üyeye ver");
      await ctx.borrow(book, await ctx.member(), 409, "İkinci üyeye vermeyi dene");
    }
  },
  {
    rule: "Kural 4",
    title: "Üye aynı kitabı iade etmeden tekrar alamaz",
    async run(ctx) {
      const book = await ctx.book(5);
      const member = await ctx.member();
      await ctx.borrow(book, member);
      await ctx.borrow(book, member, 409, "Aynı kitabı tekrar ödünç vermeyi dene");
    }
  },
  {
    rule: "Kural 5",
    title: "Gecikmiş ödüncü olan üye yeni kitap alamaz",
    note: "Tarayıcıdan sunucunun saati ileri sarılamadığı için seed verisindeki gecikmiş ödünç kullanılır.",
    async run(ctx) {
      const overdue = await ctx.expect("Gecikmiş ödünçleri listele", "GET", "/api/loans?status=Overdue&pageSize=1", undefined, 200);
      const loan = overdue.data?.items?.[0];
      if (!loan) {
        ctx.skipped = "Veritabanında gecikmiş ödünç yok. kutuphane.db dosyasını silip uygulamayı yeniden başlatınca seed verisi geri gelir.";
        return;
      }
      const book = await ctx.book(1);
      await ctx.borrow(book, { id: loan.memberId }, 409, `${loan.memberFullName} için yeni ödünç dene`);
    }
  },
  {
    rule: "Kural 6",
    title: "İade edilmiş ödünç tekrar iade edilemez",
    async run(ctx) {
      const loan = await ctx.borrow(await ctx.book(), await ctx.member());
      await ctx.expect("İade al", "POST", `/api/loans/${loan?.id}/return`, undefined, 204);
      await ctx.expect("Tekrar iade almayı dene", "POST", `/api/loans/${loan?.id}/return`, undefined, 409);
    }
  },
  {
    rule: "Kural 7",
    title: "İade edilmemiş ödüncü olan kitap veya üye silinemez",
    async run(ctx) {
      const book = await ctx.book();
      const member = await ctx.member();
      await ctx.borrow(book, member);
      await ctx.expect("Ödünçteki kitabı silmeyi dene", "DELETE", `/api/books/${book?.id}`, undefined, 409);
      await ctx.expect("Ödüncü olan üyeyi silmeyi dene", "DELETE", `/api/members/${member?.id}`, undefined, 409);
    }
  },
  {
    rule: "Kural 8",
    title: "Kitabı olan yazar silinemez",
    async run(ctx) {
      await ctx.book();
      await ctx.expect("Yazarı silmeyi dene", "DELETE", `/api/authors/${ctx.authorForBooks?.id}`, undefined, 409);
    }
  },
  {
    rule: "Kural 9",
    title: "Stok, ödünçteki kopya sayısının altına düşürülemez",
    async run(ctx) {
      const book = await ctx.book(2);
      await ctx.borrow(book, await ctx.member());
      await ctx.borrow(book, await ctx.member());
      const body = { title: book?.title, isbn: book?.isbn, publishedYear: 2000, stockCount: 1, authorId: ctx.authorForBooks?.id, categoryIds: [] };
      await ctx.expect("Stoğu 2'den 1'e düşürmeyi dene", "PUT", `/api/books/${book?.id}`, body, 409);
    }
  },
  {
    rule: "Doğrulama",
    title: "Geçersiz istek alan hatalarıyla 400 döner",
    async run(ctx) {
      const result = await ctx.expect("Adı boş, doğum yılı 3000 olan yazar gönder", "POST", "/api/authors", { firstName: "", lastName: "X", birthYear: 3000 }, 400);
      const fields = Object.keys(result.data?.errors ?? {});
      ctx.check(`Hatalı alanlar: ${fields.join(", ") || "yok"}`, fields.includes("FirstName") && fields.includes("BirthYear"));
    }
  },
  {
    rule: "Benzersizlik",
    title: "Aynı ISBN ile ikinci kitap 409 döner",
    async run(ctx) {
      const book = await ctx.book();
      await ctx.expect("Aynı ISBN ile kitap eklemeyi dene", "POST", "/api/books",
        { title: "Kopya", isbn: book?.isbn, publishedYear: 2000, stockCount: 1, authorId: ctx.authorForBooks?.id, categoryIds: [] }, 409);
    }
  },
  {
    rule: "Bulunamadı",
    title: "Olmayan kayıt 404 döner",
    async run(ctx) {
      await ctx.expect("Olmayan kitabı iste", "GET", "/api/books/999999", undefined, 404);
      await ctx.expect("Olmayan ödüncü iade et", "POST", "/api/loans/999999/return", undefined, 404);
    }
  }
];

function scenarioHtml(scenario, state) {
  const steps = state?.ctx.steps ?? [];
  let stamp = "";
  if (state?.done) {
    if (state.ctx.skipped) stamp = `<span class="stamp stamp--skip">Atlandı</span>`;
    else if (state.passed) stamp = `<span class="stamp stamp--pass">Geçti</span>`;
    else stamp = `<span class="stamp stamp--fail">Kaldı</span>`;
  } else if (state) {
    stamp = `<span class="muted mono">çalışıyor…</span>`;
  }

  return `
    <div class="scenario-head">
      <div>
        <h3>${esc(scenario.rule)} · ${esc(scenario.title)}</h3>
        ${scenario.note ? `<p>${esc(scenario.note)}</p>` : ""}
        ${state?.ctx.skipped ? `<p>${esc(state.ctx.skipped)}</p>` : ""}
        ${state?.error ? `<p class="field-error">${esc(state.error)}</p>` : ""}
      </div>
      ${stamp}
    </div>
    ${steps.length ? `<ol class="steps">
      ${steps.map(step => `
        <li>
          <span class="${step.pass ? "ok" : "bad"}">${step.pass ? "✓" : "✗"}</span>
          <span>${esc(step.label)}${step.method ? ` <span class="muted">${step.method} ${esc(step.path)}</span>` : ""}</span>
          <span>${step.method ? `beklenen ${step.expected} · gelen <b class="${step.pass ? "ok" : "bad"}">${step.actual}</b>` : ""}</span>
        </li>`).join("")}
    </ol>` : ""}`;
}

export function renderTests(view) {
  view.innerHTML = `
    <header class="view-head">
      <div>
        <h1>Kural testleri</h1>
        <p>Her senaryo canlı API'ye istek atar, beklenen durum kodunu gelenle karşılaştırır ve oluşturduğu test kayıtlarını sonunda siler.</p>
      </div>
      <button class="button" id="run-all">Tümünü çalıştır</button>
    </header>
    <p class="summary-line" id="summary">${scenarios.length} senaryo hazır.</p>
    <div id="scenarios">
      ${scenarios.map((scenario, index) => `<section class="scenario" data-index="${index}">${scenarioHtml(scenario)}</section>`).join("")}
    </div>`;

  const runButton = view.querySelector("#run-all");
  runButton.addEventListener("click", async () => {
    runButton.disabled = true;
    let passed = 0;
    let skipped = 0;

    for (const [index, scenario] of scenarios.entries()) {
      const section = view.querySelector(`[data-index="${index}"]`);
      const state = { done: false };
      const redraw = () => { section.innerHTML = scenarioHtml(scenario, state); };
      state.ctx = new ScenarioContext(redraw);
      redraw();

      try {
        await scenario.run(state.ctx);
      } catch (error) {
        state.error = `Senaryo hata verdi: ${error.message}`;
      } finally {
        await state.ctx.cleanup();
      }

      state.done = true;
      state.passed = !state.error && state.ctx.steps.every(step => step.pass);
      if (state.ctx.skipped) skipped++;
      else if (state.passed) passed++;
      redraw();
    }

    const failed = scenarios.length - passed - skipped;
    view.querySelector("#summary").textContent =
      `${passed} geçti · ${failed} kaldı${skipped ? ` · ${skipped} atlandı` : ""} · toplam ${scenarios.length} senaryo`;
    runButton.disabled = false;
  });
}
