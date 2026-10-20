// Shared response contracts for the ASP.NET API (BaseResponse/BasePaginatedList).
export const dataOf = (response) => response?.data?.data;
export const itemsOf = (response) => {
    const value = dataOf(response);
    return Array.isArray(value) ? value : Array.isArray(value?.items) ? value.items : [];
};

// Existing screens filter/summarize locally, so they must load all pages rather
// than silently report totals from a single capped page.
export async function collectPages(request, pageSize = 200) {
    const items = [];
    for (let page = 1; page <= 1000; page++) {
        const response = await request(page, pageSize);
        const value = dataOf(response);
        if (Array.isArray(value)) return response;
        if (!Array.isArray(value?.items)) throw new Error("Dữ liệu danh sách từ API không đúng định dạng.");
        items.push(...value.items);
        const totalPages = Number(value.totalPages);
        if (value.hasNextPage === false || (Number.isFinite(totalPages) && totalPages >= 0 && page >= totalPages)
            || (!Number.isFinite(totalPages) && value.items.length < pageSize)) {
            return { ...response, data: { ...response.data, data: { ...value, items, totalItems: items.length } } };
        }
        if (value.items.length === 0) throw new Error("API trả trang rỗng trước khi tải đủ danh sách. Vui lòng thử lại.");
    }
    throw new Error("Danh sách quá lớn. Cần thu hẹp bộ lọc trên máy chủ.");
}

// Return a complete fresh list synchronously, or a cache miss. Never show a
// partial directory as complete when a later server page has not been cached.
export function peekPages(readPage, pageSize = 200) {
    const items = [];
    for (let page = 1; page <= 1000; page++) {
        const response = readPage(page, pageSize);
        if (!response) return null;
        const value = dataOf(response);
        if (Array.isArray(value)) return response;
        if (!Array.isArray(value?.items)) return null;
        items.push(...value.items);
        const totalPages = Number(value.totalPages);
        if (value.hasNextPage === false || (Number.isFinite(totalPages) && totalPages >= 0 && page >= totalPages)
            || (!Number.isFinite(totalPages) && value.items.length < pageSize)) {
            return { ...response, data: { ...response.data, data: { ...value, items, totalItems: items.length } } };
        }
        if (value.items.length === 0) return null;
    }
    return null;
}

export function apiErrorMessage(error, fallback = "Không thể thực hiện yêu cầu. Vui lòng thử lại.") {
    const body = error?.response?.data;
    if (typeof body?.message === "string" && body.message) return body.message;
    if (typeof body === "string" && body && !body.trim().startsWith("<")) return body;
    if (error?.response?.status === 403) return "Bạn không có quyền thực hiện thao tác này.";
    if (error?.response?.status === 429) return "Bạn thao tác quá nhanh. Vui lòng đợi rồi thử lại.";
    if (error?.response?.status >= 500) return "Máy chủ chưa xử lý được yêu cầu. Vui lòng thử lại hoặc liên hệ quản trị viên.";
    return error?.message || fallback;
}
