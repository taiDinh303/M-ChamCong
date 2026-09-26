import axios from "axios";
import { getAuth } from "../auth/auth";

const axiosClient = axios.create({
    baseURL: "https://localhost:7038/api",
    headers: {
        "Content-Type": "application/json",
    },
});

// Đính kèm token khi đã đăng nhập (lưu trong marixa_auth)
axiosClient.interceptors.request.use((config) => {
    const token = getAuth()?.token;
    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
});

export default axiosClient;
