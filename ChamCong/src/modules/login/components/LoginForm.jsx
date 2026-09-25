import { useState } from "react";
import authApi from "../api/authApi";

const LoginForm = () => {
    const [form, setForm] = useState({
        username: "",
        password: "",
        rememberMe: false,
    });

    const [loading, setLoading] = useState(false);
    const [error, setError] = useState("");

    const handleChange = (e) => {
        const { name, value, type, checked } = e.target;

        setForm((prev) => ({
            ...prev,
            [name]: type === "checkbox" ? checked : value,
        }));
    };

    const handleSubmit = async (e) => {
        e.preventDefault();

        try {
            setLoading(true);
            setError("");

            const response = await authApi.login(form);

            console.log("Login response:", response);
            console.log("Login data:", response.data);

            const data = response.data.data;

            // Tạm thời lưu response để kiểm tra
            console.log("Auth data:", data);

            // Nếu backend trả token
            if (data?.token) {
                localStorage.setItem("token", data.token);
            }

            if (data?.accessToken) {
                localStorage.setItem("token", data.accessToken);
            }

            window.location.href = "/employees";
        } catch (error) {
            console.error("Login error:", error);

            setError(
                error.response?.data?.message ||
                error.response?.data?.data ||
                "Đăng nhập thất bại."
            );
        } finally {
            setLoading(false);
        }
    };

    return (
        <form onSubmit={handleSubmit} className="login-form">
            <div className="form-group">
                <label>Tài khoản</label>

                <input
                    type="text"
                    name="username"
                    value={form.username}
                    onChange={handleChange}
                    placeholder="Nhập username"
                    required
                />
            </div>

            <div className="form-group">
                <label>Mật khẩu</label>

                <input
                    type="password"
                    name="password"
                    value={form.password}
                    onChange={handleChange}
                    placeholder="Nhập mật khẩu"
                    required
                />
            </div>

            <div className="remember-me">
                <input
                    type="checkbox"
                    name="rememberMe"
                    checked={form.rememberMe}
                    onChange={handleChange}
                />

                <label>Ghi nhớ đăng nhập</label>
            </div>

            {error && (
                <div className="error-message">
                    {error}
                </div>
            )}

            <button type="submit" disabled={loading}>
                {loading ? "Đang đăng nhập..." : "Đăng nhập"}
            </button>
        </form>
    );
};

export default LoginForm;