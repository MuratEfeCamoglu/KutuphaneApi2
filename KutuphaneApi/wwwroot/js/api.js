// API ile konuşan tek yer. Her istek fetch ile atılır ve sonucu "İstek defteri"ne bildirilir.
// Arayüz API ile aynı adresten (http://localhost:5041) sunulduğu için adresler göreli: "/api/books".

const listeners = [];

export function onRequest(listener) {
  listeners.push(listener);
}

// Ham istek: hata olsa bile sonucu döndürür. Kural testleri beklenen durum kodunu bununla karşılaştırır.
export async function request(method, path, body) {
  const options = { method, headers: { Accept: "application/json" } };
  if (body !== undefined) {
    options.headers["Content-Type"] = "application/json";
    options.body = JSON.stringify(body);
  }

  let result;
  try {
    const response = await fetch(path, options);
    const text = await response.text();
    let data = null;
    if (text) {
      try { data = JSON.parse(text); } catch { data = text; }
    }
    result = { method, path, body, status: response.status, ok: response.ok, data };
  } catch (networkError) {
    result = { method, path, body, status: 0, ok: false, data: { title: "API'ye ulaşılamadı", detail: networkError.message } };
  }

  listeners.forEach(listener => listener(result));
  return result;
}

export class ApiError extends Error {
  constructor(result) {
    super(describeProblem(result));
    this.result = result;
  }
}

// Ekranlar için: başarısız yanıtı ApiError olarak fırlatır, başarılıysa sadece gövdeyi döner.
export async function call(method, path, body) {
  const result = await request(method, path, body);
  if (!result.ok) throw new ApiError(result);
  return result.data;
}

// ProblemDetails yanıtını okunur tek satıra çevirir.
export function describeProblem(result) {
  const data = result.data;
  if (data && typeof data === "object") {
    if (data.errors) {
      return Object.entries(data.errors)
        .map(([field, messages]) => `${field || "istek"}: ${messages.join(" ")}`)
        .join(" · ");
    }
    return data.detail || data.title || `HTTP ${result.status}`;
  }
  return `HTTP ${result.status}`;
}
