import axios from "axios";
import { getAuth, clearAuth } from "../auth/auth";
import { API_BASE_URL } from "./apiConfig";
import { apiErrorMessage } from "./contracts";
import { responseCache, sessionScope, cacheKey, cacheableRead, invalidateData } from "./responseCache.js";
import { connection, recoverableRead, isConnectionFailure } from "./connection.js";

const axiosClient = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30000,
  headers: {
    "Content-Type": "application/json",
  },
});

// Đính kèm token khi đã đăng nhập (lưu trong marixa_auth)
axiosClient.interceptors.request.use((config) => {
    if (config.data instanceof FormData) config.headers.delete("Content-Type");
    const token = getAuth()?.token;
    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
});

// Token hết hạn / bị thu hồi (401) -> xoá phiên cũ, đưa về trang đăng nhập.
// Tránh cảnh 20+ request đồng loạt trả 401 làm tràn log "Failed to load resource".
axiosClient.interceptors.response.use(
  (response) => {
    if (!["get", "head", "options"].includes(response.config.method?.toLowerCase())) {
      const trackingOnly = /^\/?EmployeeReport\/viewed\//i.test(response.config.url || "");
      invalidateData(true, trackingOnly ? "/EmployeeReport/" : null);
    }
    return response;
  },
  async (error) => {
    // File endpoints return JSON errors as a Blob when responseType is "blob".
    if (error?.response?.data instanceof Blob && error.response.data.type.includes("json")) {
      try { error.response.data = JSON.parse(await error.response.data.text()); } catch { /* keep original */ }
    }
    error.message = apiErrorMessage(error);
    if (!['get', 'head', 'options'].includes(error?.config?.method?.toLowerCase()) && isConnectionFailure(error)) {
      // A timed-out write may already have committed. Invalidate, never replay it.
      invalidateData(true); connection.unavailable();
      error.message = "Chưa xác nhận được kết quả gửi. Hãy kiểm tra lại dữ liệu trước khi gửi lần nữa.";
      if (error.response) error.response.data = { message: error.message };
    }
    if (error?.response && typeof error.response.data?.message !== "string") {
      error.response.data = { ...(error.response.data && typeof error.response.data === "object" ? error.response.data : {}), message: error.message };
    }
    const status = error?.response?.status;
    if ([401, 403].includes(status)) responseCache.clear();
    const auth = getAuth();
    // Chỉ xoá phiên khi đang giữ token (không quăng khách chưa đăng nhập về /login)
    const sentToken = error?.config?.headers?.Authorization;
    if (status === 401 && sentToken && (!auth?.token || sentToken === `Bearer ${auth.token}`)
      && !window.location.pathname.startsWith("/login")) {
      clearAuth();
      window.location.href = "/login";
    }
    return Promise.reject(error);
  }
);

const probeClient = axios.create({ baseURL: API_BASE_URL, timeout: 10_000 });
connection.configure(async ({ timeout = 10_000 } = {}) => {
    const response = await probeClient.get("/AttendanceRulePublic/active", { timeout });
    if (!Array.isArray(response.data?.data)) {
        const error = new Error("Máy chủ chưa sẵn sàng.");
        error.response = { status: 503 };
        throw error;
    }
});

const networkGet = axiosClient.get.bind(axiosClient);
axiosClient.get = (url, config = {}) => {
    const auth = getAuth();
    const scope = sessionScope(auth);
    const load = () => recoverableRead(() => {
        if (sessionScope(getAuth()) !== scope) throw new axios.CanceledError("Phiên đăng nhập đã thay đổi.");
        return networkGet(url, { timeout: 15_000, ...config });
    }, { signal: config.signal, recover: config.recover !== false });
    if (!cacheableRead({ ...config, url }, auth)) return load();
    return responseCache.read(cacheKey(axiosClient.getUri({ ...config, url })), scope, load, {
        signal: config.signal, force: config.cache === "reload", staleOnError: isConnectionFailure,
    });
};

export default axiosClient;
