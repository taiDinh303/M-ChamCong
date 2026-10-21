import { useEffect, useRef, useState } from "react";
import { useLocation } from "react-router-dom";
import Header from "./Header";
import { pageTitle } from "./pageTitles";
import "./shell.css";
import PageGuidance from "./PageGuidance";
import useMobile from "../common/useMobile";

export default function AuthenticatedShell({ storageKey, sidebar, children, profile }) {
    const mobile = useMobile();
    const { pathname } = useLocation();
    const [drawerOpen, setDrawerOpen] = useState(false);
    const [collapsed, setCollapsed] = useState(() => {
        try { return localStorage.getItem(storageKey) === "1"; } catch { return false; }
    });
    const dialogRef = useRef(null);
    const toggleRef = useRef(null);
    const title = pageTitle(pathname);

    useEffect(() => {
        document.title = `${title} · MARIXA`;
    }, [title]);

    const toggle = () => {
        if (mobile) setDrawerOpen((value) => !value);
        else setCollapsed((value) => {
            const next = !value;
            try { localStorage.setItem(storageKey, next ? "1" : "0"); } catch { /* optional preference */ }
            return next;
        });
    };

    useEffect(() => {
        const dialog = dialogRef.current;
        if (!mobile || !drawerOpen || !dialog) return;
        const toggleButton = toggleRef.current;
        const previousOverflow = document.body.style.overflow;
        document.body.style.overflow = "hidden";
        dialog.showModal();
        return () => {
            dialog.close();
            document.body.style.overflow = previousOverflow;
            toggleButton?.focus();
        };
    }, [mobile, drawerOpen]);

    const closeAfterNavigation = (event) => {
        if (event.target.closest("a[href], .att-nav-item, .work-back-btn")) setDrawerOpen(false);
    };

    const personal = pathname === "/me" || pathname.startsWith("/me/");
    const workspace = personal ? "me" : /^\/hr(?:\/|$)/.test(pathname) ? "hr" : /^\/admin(?:\/|$)/.test(pathname) ? "admin" : /^\/accounting(?:\/|$)/.test(pathname) ? "accounting" : undefined;
    return <div className={`app-shell app-shell--responsive${personal ? " app-shell--me" : ""}`} data-workspace={workspace}>
            <Header profile={profile} onToggleSidebar={toggle} toggleRef={toggleRef}
                sidebarExpanded={mobile ? drawerOpen : !collapsed} sidebarControls={mobile ? "mobile-navigation" : "desktop-navigation"} />
            <div className="app-body">
                {!mobile && <div id="desktop-navigation" className="desktop-navigation">{sidebar(collapsed)}</div>}
                <main className="att-main" aria-label={title}>{children}<PageGuidance pathname={pathname} /></main>
            </div>
            {mobile && <dialog ref={dialogRef} id="mobile-navigation" className="navigation-drawer" aria-labelledby="navigation-title"
                onCancel={() => setDrawerOpen(false)} onClick={(event) => {
                    if (event.target === event.currentTarget) setDrawerOpen(false);
                }}>
                <div className="navigation-drawer-panel">
                    <div className="navigation-drawer-head"><strong id="navigation-title">Điều hướng</strong>
                        <button type="button" aria-label="Đóng menu điều hướng" onClick={() => setDrawerOpen(false)}>×</button>
                    </div>
                    <div onClick={closeAfterNavigation}>{sidebar(false)}</div>
                </div>
            </dialog>}
        </div>;
}
