import { useEffect, useRef, useState } from "react";
import { useLanguage } from "../../services/i18n/LanguageProvider";
import "./lang-picker.css";

// Cờ SVG inline (hiển thị đúng trên mọi OS, kể cả Windows không có font emoji cờ)
const FLAGS = {
    vi: <svg viewBox="0 0 30 20" xmlns="http://www.w3.org/2000/svg" aria-hidden="true"><rect width="30" height="20" fill="#DE2910"/><path d="M15 4l1.9 5.7h6.2l-5 3.6 1.9 5.7-5-3.6-5 3.6 1.9-5.7-5-3.6h6.2z" fill="#FFDE00"/></svg>,
    en: <svg viewBox="0 0 60 40" xmlns="http://www.w3.org/2000/svg" aria-hidden="true"><rect width="60" height="40" fill="#012169"/><path d="M0 0l60 40M60 0l-60 40" stroke="#fff" strokeWidth="8"/><path d="M0 0l60 40M60 0l-60 40" stroke="#C8102E" strokeWidth="4"/><rect x="26" width="8" height="40" fill="#fff"/><rect y="16" width="60" height="8" fill="#fff"/><rect x="28" width="4" height="40" fill="#C8102E"/><rect y="18" width="60" height="4" fill="#C8102E"/></svg>,
    zh: <svg viewBox="0 0 30 20" xmlns="http://www.w3.org/2000/svg" aria-hidden="true"><rect width="30" height="20" fill="#DE2910"/><path d="M6 3l.6 2.4h2.5L8.2 7l1 2.4-2.2-1.6-2.2 1.6 1-2.4L4.4 5.4h2.5z" fill="#FFDE00"/><path d="M13 5.5l.5 1.6h1.7L14.7 8.3l.7 1.6-1.5-1.1-1.5 1.1.7-1.6-1.5-1.2h1.7z" fill="#FFDE00"/><path d="M14.5 10l.5 1.6h1.7l-1.6 1.2.7 1.6-1.5-1.1-1.5 1.1.7-1.6-1.5-1.2h1.7z" fill="#FFDE00"/><path d="M13 14.5l.5 1.6h1.7l-1.6 1.2.7 1.6-1.5-1.1-1.5 1.1.7-1.6-1.5-1.2h1.7z" fill="#FFDE00"/></svg>,
};
const LANG_LABELS = { vi: "Tiếng Việt", en: "English", zh: "中文" };

// Nút chuyển ngôn ngữ (cờ + tên + ▾) — dùng chung cho Header và các trang không có header.
// flagOnly: chỉ hiện cờ (không hiện chữ) — dùng cho header gọn. Dropdown giữ nguyên đầy đủ.
const LanguagePicker = ({ className = "", flagOnly = false }) => {
    const { language, setLanguage } = useLanguage();
    const [open, setOpen] = useState(false);
    const ref = useRef(null);

    useEffect(() => {
        const onDown = (e) => {
            if (ref.current && !ref.current.contains(e.target)) setOpen(false);
        };
        document.addEventListener("mousedown", onDown);
        return () => document.removeEventListener("mousedown", onDown);
    }, []);

    return (
        <label className={`lang-picker${flagOnly ? " lang-picker--flag-only" : ""} ${className}`.trim()} ref={ref} title="Language">
            <button
                type="button"
                className="lang-toggle"
                onClick={() => setOpen((v) => !v)}
                aria-haspopup="menu"
                aria-expanded={open}
                aria-label="Language"
            >
                <span className="flag">{FLAGS[language]}</span>
                {!flagOnly && <span>{LANG_LABELS[language]}</span>}
                <span className="caret">▾</span>
            </button>
            {open && (
                <div className="lang-menu" role="menu">
                    {["vi", "en", "zh"].map((code) => (
                        <button
                            key={code}
                            type="button"
                            role="menuitem"
                            className={code === language ? "active" : ""}
                            onClick={() => { setLanguage(code); setOpen(false); }}
                        >
                            <span className="flag">{FLAGS[code]}</span>
                            <span>{LANG_LABELS[code]}</span>
                        </button>
                    ))}
                </div>
            )}
        </label>
    );
};

// Danh sách lựa chọn ngôn ngữ (dùng nội嵌 trong dropdown tài khoản ở header
// non-home, nơi không có ô cờ riêng).
export const LanguageOptions = () => {
    const { language, setLanguage } = useLanguage();
    return (
        <div className="lang-inline" role="menu" aria-label="Ngôn ngữ">
            <div className="lang-inline__label">Ngôn ngữ</div>
            <div className="lang-inline__row">
                {["vi", "en", "zh"].map((code) => (
                    <button
                        key={code}
                        type="button"
                        role="menuitem"
                        className={`lang-inline__opt${code === language ? " active" : ""}`}
                        title={LANG_LABELS[code]}
                        aria-pressed={code === language}
                        onClick={() => setLanguage(code)}
                    >
                        <span className="flag">{FLAGS[code]}</span>
                    </button>
                ))}
            </div>
        </div>
    );
};

export default LanguagePicker;
