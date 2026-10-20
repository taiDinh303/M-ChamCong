import { useEffect, useRef, useState } from "react";
import { clearAuth, getAuth } from "../../services/auth/auth";
import {
    fileToAvatarDataUrl,
    getAvatar,
    setAvatar,
    subscribeAvatarChange,
} from "../../services/avatar/avatar";
import { GuideModal } from "./GuideModal";
import LanguagePicker, { LanguageOptions } from "./LanguagePicker";
import NotificationBell from "./NotificationBell";
import ChangePasswordModal from "./ChangePasswordModal";

import "./header.css";


// Home uses a compact header; modules add a sidebar toggle.
const Header = ({ profile, onToggleSidebar, toggleRef, sidebarExpanded, sidebarControls, minimal = false }) => {
    const auth = getAuth();
    const key = auth?.userId;

    const [open, setOpen] = useState(false);
    const [modal, setModal] = useState(null);
    const [avatar, setAvatarState] = useState(() => getAvatar(key));
    const [avatarBusy, setAvatarBusy] = useState(false);

    // Đường dẫn "phân khu" theo vai trò: Admin -> dashboard,
    // HR/Manager -> module HR, nhân viên thường -> module Me.
    const roles = auth?.roles || [];
    const roleHome = roles.includes("Admin")
        ? "/admin"
        : roles.some((r) => ["HR", "Manager"].includes(r))
            ? "/hr"
            : "/me";

    // Đồng bộ avatar khi bị đổi ở nơi khác (vd trang Hồ sơ).
    useEffect(
        () =>
            subscribeAvatarChange(({ key: k }) => {
                if (k === key) setAvatarState(getAvatar(key));
            }),
        [key]
    );

    // Đóng menu khi click ngoài.
    const userRef = useRef(null);
    const fileRef = useRef(null);
    useEffect(() => {
        const onDown = (e) => {
            if (userRef.current && !userRef.current.contains(e.target))
                setOpen(false);
        };
        const onKey = (event) => {
            if (event.key === "Escape") setOpen(false);
        };
        document.addEventListener("mousedown", onDown);
        document.addEventListener("keydown", onKey);
        return () => { document.removeEventListener("mousedown", onDown); document.removeEventListener("keydown", onKey); };
    }, []);

    const [notice, setNotice] = useState("");
    const showNotice = (msg) => {
        setNotice(msg);
        setTimeout(() => setNotice(""), 3000);
    };

    const name = profile?.fullName || auth?.userName || "Nhân viên";
    const code = profile?.employeeCode || auth?.employeeCode || "";
    const imgSrc = avatar || auth?.picture || null;

    const pickAvatar = async (e) => {
        const file = e.target.files?.[0];
        e.target.value = "";
        if (!file || !key) return;
        setAvatarBusy(true);
        try {
            const dataUrl = await fileToAvatarDataUrl(file);
            setAvatar(key, dataUrl);
            setAvatarState(dataUrl);
            setOpen(false);
            showNotice("Đã đổi ảnh đại diện.");
        } catch {
            showNotice("Không thể đọc ảnh, vui lòng thử lại.");
        } finally {
            setAvatarBusy(false);
        }
    };

    const logout = () => {
        clearAuth();
        window.location.href = "/login";
    };

    return (
        <header className={`app-header${minimal ? " app-header--minimal" : ""}`}>
            <div className="app-header-left">
                {!minimal && (
                    <button
                        type="button"
                        className="app-header-toggle"
                        ref={toggleRef}
                        aria-label="Menu điều hướng"
                        aria-expanded={sidebarExpanded}
                        aria-controls={sidebarControls}
                        onClick={onToggleSidebar}
                    >
                        <span />
                        <span />
                        <span />
                    </button>
                )}
                <a href="/home" className="app-header-brand" aria-label="MARIXA — Trang chủ">
                    <span className="app-header-logo">M</span>
                    <span className="app-header-logo-text">MARIXA</span>
                </a>
            </div>

            <div className="app-header-user" ref={userRef}>
                <NotificationBell />
                {minimal && <LanguagePicker flagOnly />}
                <button
                    type="button"
                    className="app-header-avatar-btn"
                    aria-haspopup="menu"
                    aria-expanded={open}
                    aria-label="Tùy chọn tài khoản"
                    onClick={() => setOpen((v) => !v)}
                >
                    {imgSrc ? (
                        <img src={imgSrc} alt="Ảnh đại diện" />
                    ) : (
                        <span className="app-header-avatar-fallback" aria-hidden="true">
                            <svg viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                                <circle cx="12" cy="8" r="4" />
                                <path d="M4 20c0-3.3 3.6-6 8-6s8 2.7 8 6" />
                            </svg>
                        </span>
                    )}
                </button>

                {/* Nút "về phân khu theo phận sự" — icon home, chỉ ở home (minimal) */}
                {minimal && (
                    <button
                        type="button"
                        className="app-header-home-btn"
                        title="Vào phân khu của bạn"
                        aria-label="Vào phân khu theo phận sự"
                        onClick={() => {
                            window.location.href = roleHome;
                        }}
                    >
                        <span className="app-header-home-icon" aria-hidden="true">
                            <svg viewBox="0 0 24 24" width="19" height="19" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                                <path d="M3 11l9-8 9 8" />
                                <path d="M5 10v10h5v-6h4v6h5V10" />
                            </svg>
                        </span>
                    </button>
                )}

                {open && (
                    <div className="app-header-menu" role="menu">
                        <div className="app-header-menu-head">
                            <strong>{name}</strong>
                            <span>{code || "—"}</span>
                        </div>
                        {!minimal && <LanguageOptions />}
                        <button
                            type="button"
                            role="menuitem"
                            onClick={() => {
                                setOpen(false);
                                setModal("password");
                            }}
                        >
                            Đổi mật khẩu
                        </button>
                        <button
                            type="button"
                            role="menuitem"
                            disabled={avatarBusy}
                            onClick={() => fileRef.current?.click()}
                        >
                            Đổi ảnh đại diện
                        </button>
                        <button
                            type="button"
                            role="menuitem"
                            onClick={() => {
                                setOpen(false);
                                setModal("guide");
                            }}
                        >
                            Hướng dẫn
                        </button>
                        <button
                            type="button"
                            role="menuitem"
                            onClick={() => {
                                setOpen(false);
                                window.location.href = "/profile";
                            }}
                        >
                            Hồ sơ
                        </button>
                        <div className="app-header-menu-sep" />
                        <button
                            type="button"
                            role="menuitem"
                            className="danger"
                            onClick={logout}
                        >
                            Đăng xuất
                        </button>
                        <input
                            ref={fileRef}
                            type="file"
                            accept="image/*"
                            hidden
                            onChange={pickAvatar}
                        />
                    </div>
                )}

                {notice && <div className="app-header-notice">{notice}</div>}
                {modal === "guide" && (
                    <GuideModal onClose={() => setModal(null)} />
                )}
                {modal === "password" && (
                    <ChangePasswordModal
                        onClose={(ok) => {
                            setModal(null);
                            if (ok) showNotice("Đã đổi mật khẩu thành công.");
                        }}
                    />
                )}
            </div>
        </header>
    );
};

export default Header;
