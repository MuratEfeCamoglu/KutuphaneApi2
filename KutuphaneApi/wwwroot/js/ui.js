import { ApiError } from "./api.js";

// Kullanıcıdan veya API'den gelen metni HTML'e güvenle yazmak için.
export function esc(value) {
  return String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#39;");
}

export function formatDate(iso) {
  if (!iso) return "—";
  return new Date(iso).toLocaleDateString("tr-TR", { day: "2-digit", month: "short", year: "numeric" });
}

export const statusLabels = { Active: "Ödünçte", Overdue: "Gecikti", Returned: "İade edildi" };

let toastTimer;
export function toast(message, isError = false) {
  const element = document.getElementById("toast");
  element.textContent = message;
  element.classList.toggle("toast--error", isError);
  element.classList.add("toast--show");
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => element.classList.remove("toast--show"), isError ? 6000 : 3000);
}

// Hata mesajının başına durum kodunu koyar: "409 · Bu kitabın müsait kopyası yok."
export function showError(error) {
  if (error instanceof ApiError) {
    toast(`${error.result.status} · ${error.message}`, true);
  } else {
    toast(error.message, true);
    console.error(error);
  }
}

export function openDialog(html) {
  const dialog = document.getElementById("dialog");
  dialog.innerHTML = html;
  if (!dialog.open) dialog.showModal();
  dialog.querySelectorAll("[data-close]").forEach(button => button.addEventListener("click", closeDialog));
  return dialog;
}

export function closeDialog() {
  document.getElementById("dialog").close();
}

// Doğrulama hatalarını (400) ilgili alanın altına yazar. API "Title" döner, input adı "title".
export function showFieldErrors(form, error) {
  form.querySelectorAll(".field-error").forEach(element => element.remove());
  const errors = error instanceof ApiError ? error.result.data?.errors : null;
  if (!errors) return;

  for (const [key, messages] of Object.entries(errors)) {
    const name = key.charAt(0).toLowerCase() + key.slice(1);
    const input = form.querySelector(`[name="${name}"]`);
    const target = input?.closest("label") ?? input?.closest("fieldset");
    if (target) {
      target.insertAdjacentHTML("beforeend", `<span class="field-error">${esc(messages.join(" "))}</span>`);
    }
  }
}

// Boş metin → null, sayı alanları → number.
export function readForm(form) {
  const values = {};
  for (const element of form.elements) {
    if (!element.name || element.type === "checkbox") continue;
    const raw = element.value.trim();
    if (element.type === "number" || element.dataset.number !== undefined) {
      values[element.name] = raw === "" ? null : Number(raw);
    } else {
      values[element.name] = raw === "" ? null : raw;
    }
  }
  return values;
}

export function options(items, selectedValue, placeholder) {
  const first = placeholder ? `<option value="">${esc(placeholder)}</option>` : "";
  return first + items
    .map(item => `<option value="${item.value}" ${String(item.value) === String(selectedValue ?? "") ? "selected" : ""}>${esc(item.label)}</option>`)
    .join("");
}
