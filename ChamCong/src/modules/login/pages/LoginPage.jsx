import LoginForm from "../components/LoginForm";

const LoginPage = () => {
    return (
        <div className="login-page">
            <div className="login-card">
                <h1>Đăng nhập</h1>

                <p>
                    Đăng nhập vào hệ thống chấm công
                </p>

                <LoginForm />
            </div>
        </div>
    );
};

export default LoginPage;