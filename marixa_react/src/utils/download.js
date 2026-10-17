export function downloadResponse(response, fallbackName) {
    const disposition = response.headers?.["content-disposition"] || "";
    const encoded = disposition.match(/filename\*=UTF-8''([^;]+)/i)?.[1];
    let name = disposition.match(/filename="([^"]+)"/i)?.[1] || fallbackName;
    if (encoded) { try { name = decodeURIComponent(encoded); } catch { /* use fallback */ } }
    const url = URL.createObjectURL(response.data);
    const link = document.createElement("a");
    link.href = url;
    link.download = name.replace(/[\\/:*?"<>|\x00-\x1F]/g, "_"); // eslint-disable-line no-control-regex
    document.body.appendChild(link);
    link.click();
    link.remove();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
}
