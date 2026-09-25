import axios from "axios";

const axiosClient = axios.create({
    baseURL: "https://localhost:7038/api",
    headers: {
        "Content-Type": "application/json",
    },
});

// Đính kèm token khi đã đăng nhập
axiosClient.interceptors.request.use((config) => {
    const token = localStorage.getItem("token");
    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
});

export default axiosClient;
