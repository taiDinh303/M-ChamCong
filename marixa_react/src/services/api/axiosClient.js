import axios from "axios";
import { getAuth, clearAuth } from "../auth/auth";
import { API_BASE_URL } from "./apiConfig";
import { apiErrorMessage } from "./contracts";

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
  (response) => response,
  async (error) => {
    // File endpoints return JSON errors as a Blob when responseType is "blob".
    if (error?.response?.data instanceof Blob && error.response.data.type.includes("json")) {
      try { error.response.data = JSON.parse(await error.response.data.text()); } catch { /* keep original */ }
    }
    error.message = apiErrorMessage(error);
    if (error?.response && typeof error.response.data?.message !== "string") {
      error.response.data = { ...(error.response.data && typeof error.response.data === "object" ? error.response.data : {}), message: error.message };
    }
    const status = error?.response?.status;
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

export default axiosClient;
